using System.Net.Mime;
using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController(
    AppDbContext dbContext,
    ILogger<ProvidersController> logger,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string GetProviderByIdRouteName = "GetProviderById";
    private const string GetProviderDocumentRouteName = "GetProviderDocumentById";
    private const long MaxDocumentFileSizeBytes = 10 * 1024 * 1024;
    private const long MultipartBodyLimitBytes = 25 * 1024 * 1024;
    private const string W9DocumentType = "w9";
    private const string CoiDocumentType = "coi";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaTypeNames.Application.Pdf,
        "image/jpeg",
        "image/jpg",
        "image/pjpeg",
        "image/png"
    };

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderDto>>> GetAllAsync()
    {
        var providers = await dbContext.Providers
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        return Ok(providers.Select(ToProviderDto));
    }

    [HttpGet("{id:int}", Name = GetProviderByIdRouteName)]
    public async Task<ActionResult<ProviderDto>> GetByIdAsync(int id)
    {
        var provider = await dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (provider is null)
        {
            return NotFound();
        }

        return Ok(ToProviderDto(provider));
    }

    [HttpGet("{id:int}/documents/{documentType}", Name = GetProviderDocumentRouteName)]
    public async Task<IActionResult> DownloadDocumentAsync(int id, string documentType)
    {
        var provider = await dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (provider is null)
        {
            return NotFound();
        }

        var normalizedDocumentType = NormalizeDocumentType(documentType);
        if (normalizedDocumentType is null)
        {
            return NotFound();
        }

        var relativePath = GetDocumentRelativePath(provider, normalizedDocumentType);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return NotFound();
        }

        var absolutePath = ToAbsoluteDocumentPath(relativePath);
        if (!System.IO.File.Exists(absolutePath))
        {
            return NotFound();
        }

        var extension = Path.GetExtension(absolutePath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => MediaTypeNames.Application.Pdf,
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => MediaTypeNames.Application.Octet
        };

        var downloadName = $"{normalizedDocumentType}{extension}";
        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, contentType, downloadName);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartBodyLimitBytes)]
    public async Task<ActionResult<ProviderDto>> CreateAsync([FromForm] ProviderCreateRequest request)
    {
        ProviderEntity? createdEntity = null;

        try
        {
            var requestValidation = ValidateCreateRequest(request);
            if (requestValidation is not null)
            {
                return requestValidation;
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var normalizedPhone = request.Phone.Trim();
            var normalizedFullName = request.FullName.Trim().ToLowerInvariant();

            // Guard against duplicate inserts caused by client retries on transient failures.
            var existingProvider = await dbContext.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Email.ToLower() == normalizedEmail &&
                    item.Phone == normalizedPhone &&
                    item.FullName.ToLower() == normalizedFullName);

            if (existingProvider is not null)
            {
                logger.LogInformation(
                    "Duplicate provider creation prevented for Email={Email}, Phone={Phone}. ExistingId={ProviderId}",
                    normalizedEmail,
                    normalizedPhone,
                    existingProvider.Id);

                return Conflict(new
                {
                    message = "A provider with the same identity already exists.",
                    provider = ToProviderDto(existingProvider)
                });
            }

            var now = DateTime.UtcNow;

            createdEntity = new ProviderEntity
            {
                CreatedAt = now,
                UpdatedAt = now
            };

            createdEntity.ApplyUpdate(request);

            if ((createdEntity.VerificationStatus is "Verified" or "Active") && createdEntity.VerifiedAt is null)
            {
                createdEntity.VerifiedAt = now;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            dbContext.Providers.Add(createdEntity);
            await dbContext.SaveChangesAsync();

            var savedW9 = await SaveProviderDocumentAsync(createdEntity.Id, request.W9File!, W9DocumentType);
            var savedCoi = await SaveProviderDocumentAsync(createdEntity.Id, request.CoiFile!, CoiDocumentType);

            createdEntity.W9FilePath = savedW9.relativePath;
            createdEntity.CoiFilePath = savedCoi.relativePath;
            createdEntity.W9UploadedAt = savedW9.uploadedAt;
            createdEntity.CoiUploadedAt = savedCoi.uploadedAt;
            createdEntity.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtRoute(GetProviderByIdRouteName, new { id = createdEntity.Id }, ToProviderDto(createdEntity));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Provider creation failed for Email={Email}, Phone={Phone}.",
                request.Email,
                request.Phone);

            // If the record was committed before response generation failed, return a success payload.
            if (createdEntity?.Id > 0)
            {
                var persistedEntity = await dbContext.Providers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == createdEntity.Id);

                if (persistedEntity is not null)
                {
                    return Ok(ToProviderDto(persistedEntity));
                }
            }

            return Problem(
                title: "Provider creation failed",
                detail: "An unexpected error occurred while creating the provider.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProviderDto>> UpdateAsync(int id, [FromBody] ProviderUpsertRequest request)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);

        if (entity is null)
        {
            return NotFound();
        }

        entity.ApplyUpdate(request);

        if ((entity.VerificationStatus is "Verified" or "Active") && entity.VerifiedAt is null)
        {
            entity.VerifiedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        return Ok(ToProviderDto(entity));
    }

    [HttpPost("{id:int}/documents")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartBodyLimitBytes)]
    public async Task<ActionResult<ProviderDto>> UploadDocumentsAsync(int id, [FromForm] ProviderDocumentsUploadRequest request)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        if (request.W9File is null && request.CoiFile is null)
        {
            return BadRequest(new { message = "Please upload at least one document." });
        }

        var requestValidation = ValidateOptionalDocuments(request.W9File, request.CoiFile);
        if (requestValidation is not null)
        {
            return requestValidation;
        }

        if (request.W9File is not null)
        {
            DeleteDocumentIfExists(entity.W9FilePath);
            var savedW9 = await SaveProviderDocumentAsync(entity.Id, request.W9File, W9DocumentType);
            entity.W9FilePath = savedW9.relativePath;
            entity.W9UploadedAt = savedW9.uploadedAt;
        }

        if (request.CoiFile is not null)
        {
            DeleteDocumentIfExists(entity.CoiFilePath);
            var savedCoi = await SaveProviderDocumentAsync(entity.Id, request.CoiFile, CoiDocumentType);
            entity.CoiFilePath = savedCoi.relativePath;
            entity.CoiUploadedAt = savedCoi.uploadedAt;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Ok(ToProviderDto(entity));
    }

    [HttpPost("{id:int}/verify")]
    public async Task<ActionResult<ProviderDto>> VerifyAsync(int id)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);

        if (entity is null)
        {
            return NotFound();
        }

        entity.VerificationStatus = "Verified";
        entity.IsActive = true;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(ToProviderDto(entity));
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<ProviderDto>> DeactivateAsync(int id)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);

        if (entity is null)
        {
            return NotFound();
        }

        entity.VerificationStatus = "Inactive";
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(ToProviderDto(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);

        if (entity is null)
        {
            return NotFound();
        }

        DeleteDocumentIfExists(entity.W9FilePath);
        DeleteDocumentIfExists(entity.CoiFilePath);

        dbContext.Providers.Remove(entity);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private ProviderDto ToProviderDto(ProviderEntity entity)
    {
        var dto = entity.ToDto();
        dto.W9FileUrl = dto.HasW9File
            ? Url.RouteUrl(GetProviderDocumentRouteName, new { id = entity.Id, documentType = W9DocumentType }, Request.Scheme)
            : null;
        dto.CoiFileUrl = dto.HasCoiFile
            ? Url.RouteUrl(GetProviderDocumentRouteName, new { id = entity.Id, documentType = CoiDocumentType }, Request.Scheme)
            : null;
        return dto;
    }

    private ActionResult? ValidateCreateRequest(ProviderCreateRequest request)
    {
        var missingFields = new List<string>();
        if (request.W9File is null)
        {
            missingFields.Add("W-9 form is required.");
        }

        if (request.CoiFile is null)
        {
            missingFields.Add("Certificate of Insurance is required.");
        }

        if (missingFields.Count > 0)
        {
            return BadRequest(new { message = string.Join(" ", missingFields) });
        }

        return ValidateOptionalDocuments(request.W9File, request.CoiFile);
    }

    private ActionResult? ValidateOptionalDocuments(IFormFile? w9File, IFormFile? coiFile)
    {
        var w9Error = ValidateFile(w9File, "W-9");
        if (w9Error is not null)
        {
            return BadRequest(new { message = w9Error });
        }

        var coiError = ValidateFile(coiFile, "Certificate of Insurance");
        if (coiError is not null)
        {
            return BadRequest(new { message = coiError });
        }

        return null;
    }

    private static string? ValidateFile(IFormFile? file, string label)
    {
        if (file is null)
        {
            return null;
        }

        if (file.Length <= 0)
        {
            return $"{label} upload is empty.";
        }

        if (file.Length > MaxDocumentFileSizeBytes)
        {
            return "File size must be less than 10 MB.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return "Only PDF, JPG, and PNG files are allowed.";
        }

        var contentType = file.ContentType?.Trim() ?? string.Empty;
        if (!AllowedContentTypes.Contains(contentType))
        {
            return "Only PDF, JPG, and PNG files are allowed.";
        }

        return null;
    }

    private async Task<(string relativePath, DateTime uploadedAt)> SaveProviderDocumentAsync(
        int providerId,
        IFormFile file,
        string documentType)
    {
        // TODO: Use cloud object storage in production (S3/Cloudinary/etc.) instead of local disk.
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var relativeDirectory = Path.Combine("uploads", "providers", providerId.ToString());
        var absoluteDirectory = Path.Combine(environment.ContentRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var generatedFileName = $"{documentType}-{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteDirectory, generatedFileName);

        await using (var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = Path.Combine(relativeDirectory, generatedFileName).Replace('\\', '/');
        return (relativePath, DateTime.UtcNow);
    }

    private void DeleteDocumentIfExists(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var absolutePath = ToAbsoluteDocumentPath(relativePath);
        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }

    private string? NormalizeDocumentType(string documentType)
    {
        return documentType.Trim().ToLowerInvariant() switch
        {
            W9DocumentType => W9DocumentType,
            CoiDocumentType => CoiDocumentType,
            _ => null
        };
    }

    private static string? GetDocumentRelativePath(ProviderEntity provider, string documentType)
    {
        return documentType switch
        {
            W9DocumentType => provider.W9FilePath,
            CoiDocumentType => provider.CoiFilePath,
            _ => null
        };
    }

    private string ToAbsoluteDocumentPath(string relativePath)
    {
        var safeRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(environment.ContentRootPath, safeRelativePath);
    }
}
