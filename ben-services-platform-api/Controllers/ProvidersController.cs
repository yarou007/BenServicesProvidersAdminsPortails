using System.Net.Mime;
using System.Text.RegularExpressions;
using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Extensions;
using BenServicesPlatform.Api.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AdminRole.SuperAdmin},{AdminRole.Admin},{AdminRole.Staff}")]
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
        var normalizedDocumentType = NormalizeDocumentType(documentType);
        if (normalizedDocumentType is null)
        {
            logger.LogInformation(
                "Provider document download rejected due to invalid document type. ProviderId={ProviderId}, RequestedType={RequestedType}",
                id,
                documentType);

            return BadRequest(new { message = "Invalid document type. Allowed values are 'w9' and 'coi'." });
        }

        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Unauthorized();
        }

        var connectedAdmin = await GetConnectedAdminAsync();
        if (connectedAdmin is null)
        {
            logger.LogWarning(
                "Provider document download forbidden for authenticated user. ProviderId={ProviderId}, DocumentType={DocumentType}",
                id,
                normalizedDocumentType);

            return Forbid();
        }

        var provider = await dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (provider is null)
        {
            logger.LogInformation(
                "Provider document download failed because provider does not exist. ProviderId={ProviderId}, DocumentType={DocumentType}",
                id,
                normalizedDocumentType);

            return NotFound();
        }

        var relativePath = GetDocumentRelativePath(provider, normalizedDocumentType);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            logger.LogInformation(
                "Provider document metadata missing. ProviderId={ProviderId}, DocumentType={DocumentType}",
                id,
                normalizedDocumentType);

            return NotFound(new { message = "Document metadata is missing for this provider." });
        }

        if (!TryResolveProviderDocumentPath(
                id,
                relativePath,
                out var absolutePath,
                out var normalizedRelativePath,
                out var pathFailureReason,
                allowLegacyApplicationPath: true))
        {
            logger.LogWarning(
                "Provider document path is invalid. ProviderId={ProviderId}, DocumentType={DocumentType}, RelativePath={RelativePath}, Reason={Reason}",
                id,
                normalizedDocumentType,
                relativePath,
                pathFailureReason);

            return NotFound(new { message = "Document metadata path is invalid or missing." });
        }

        var fileExists = System.IO.File.Exists(absolutePath);
        logger.LogInformation(
            "Provider document resolved. ProviderId={ProviderId}, DocumentType={DocumentType}, RelativePath={RelativePath}, AbsolutePath={AbsolutePath}, FileExists={FileExists}",
            id,
            normalizedDocumentType,
            normalizedRelativePath,
            absolutePath,
            fileExists);

        if (!fileExists)
        {
            return NotFound(new { message = "Document file was not found on disk." });
        }

        var extension = Path.GetExtension(absolutePath);
        var contentType = GetContentTypeFromExtension(extension);
        var downloadName = BuildDownloadFileName(provider, normalizedDocumentType, extension);
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
            DeleteDocumentIfExists(entity.Id, entity.W9FilePath);
            var savedW9 = await SaveProviderDocumentAsync(entity.Id, request.W9File, W9DocumentType);
            entity.W9FilePath = savedW9.relativePath;
            entity.W9UploadedAt = savedW9.uploadedAt;
        }

        if (request.CoiFile is not null)
        {
            DeleteDocumentIfExists(entity.Id, entity.CoiFilePath);
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

        DeleteDocumentIfExists(entity.Id, entity.W9FilePath);
        DeleteDocumentIfExists(entity.Id, entity.CoiFilePath);

        dbContext.Providers.Remove(entity);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private ProviderDto ToProviderDto(ProviderEntity entity)
    {
        var dto = entity.ToDto();

        var w9Document = ResolveProviderDocumentMetadata(entity, W9DocumentType);
        dto.HasW9File = w9Document.hasFile;
        dto.W9FileUrl = w9Document.downloadUrl;
        dto.W9UploadedAt = w9Document.uploadedAt;

        var coiDocument = ResolveProviderDocumentMetadata(entity, CoiDocumentType);
        dto.HasCoiFile = coiDocument.hasFile;
        dto.CoiFileUrl = coiDocument.downloadUrl;
        dto.CoiUploadedAt = coiDocument.uploadedAt;

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

    private void DeleteDocumentIfExists(int providerId, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        if (!TryResolveProviderDocumentPath(providerId, relativePath, out var absolutePath, out _, out var failureReason))
        {
            logger.LogWarning(
                "Skipping provider document delete due to invalid stored path. ProviderId={ProviderId}, RelativePath={RelativePath}, Reason={Reason}",
                providerId,
                relativePath,
                failureReason);

            return;
        }

        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }

    private static string? NormalizeDocumentType(string documentType)
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

    private (bool hasFile, string? downloadUrl, DateTime? uploadedAt) ResolveProviderDocumentMetadata(
        ProviderEntity provider,
        string documentType)
    {
        var relativePath = GetDocumentRelativePath(provider, documentType);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return (false, null, null);
        }

        if (!TryResolveProviderDocumentPath(
                provider.Id,
                relativePath,
                out var absolutePath,
                out _,
                out _,
                allowLegacyApplicationPath: true))
        {
            return (false, null, null);
        }

        if (!System.IO.File.Exists(absolutePath))
        {
            return (false, null, null);
        }

        var downloadUrl = Url.RouteUrl(
            GetProviderDocumentRouteName,
            new { id = provider.Id, documentType },
            Request.Scheme);

        var uploadedAt = documentType == W9DocumentType ? provider.W9UploadedAt : provider.CoiUploadedAt;
        return (true, downloadUrl, uploadedAt);
    }

    private bool TryResolveProviderDocumentPath(
        int providerId,
        string relativePath,
        out string absolutePath,
        out string normalizedRelativePath,
        out string failureReason,
        bool allowLegacyApplicationPath = false)
    {
        absolutePath = string.Empty;
        normalizedRelativePath = relativePath.Trim().Replace('\\', '/');
        failureReason = string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedRelativePath))
        {
            failureReason = "Path is empty.";
            return false;
        }

        if (Path.IsPathRooted(normalizedRelativePath))
        {
            failureReason = "Path must be relative.";
            return false;
        }

        var combinedPath = Path.Combine(
            environment.ContentRootPath,
            normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var fullPath = Path.GetFullPath(combinedPath);

        var providerRoot = Path.GetFullPath(
            Path.Combine(environment.ContentRootPath, "uploads", "providers", providerId.ToString()));
        var expectedProviderPrefix = $"uploads/providers/{providerId}/";
        if (normalizedRelativePath.StartsWith(expectedProviderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var expectedProviderRootPrefix = providerRoot + Path.DirectorySeparatorChar;
            var isInsideProviderRoot =
                fullPath.StartsWith(expectedProviderRootPrefix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fullPath, providerRoot, StringComparison.OrdinalIgnoreCase);

            if (!isInsideProviderRoot)
            {
                failureReason = "Path escapes provider upload root.";
                return false;
            }

            absolutePath = fullPath;
            return true;
        }

        if (allowLegacyApplicationPath
            && normalizedRelativePath.StartsWith("uploads/provider-applications/", StringComparison.OrdinalIgnoreCase))
        {
            var applicationsRoot = Path.GetFullPath(
                Path.Combine(environment.ContentRootPath, "uploads", "provider-applications"));
            var expectedApplicationsRootPrefix = applicationsRoot + Path.DirectorySeparatorChar;
            var isInsideApplicationsRoot =
                fullPath.StartsWith(expectedApplicationsRootPrefix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fullPath, applicationsRoot, StringComparison.OrdinalIgnoreCase);

            if (!isInsideApplicationsRoot)
            {
                failureReason = "Path escapes provider-application upload root.";
                return false;
            }

            absolutePath = fullPath;
            return true;
        }

        failureReason = allowLegacyApplicationPath
            ? $"Path must start with '{expectedProviderPrefix}' or 'uploads/provider-applications/'."
            : $"Path must start with '{expectedProviderPrefix}'.";
        return false;
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => MediaTypeNames.Application.Pdf,
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => MediaTypeNames.Application.Octet
        };
    }

    private static string BuildDownloadFileName(ProviderEntity provider, string documentType, string extension)
    {
        var prefix = documentType == W9DocumentType ? "W9" : "COI";
        var displayName = !string.IsNullOrWhiteSpace(provider.BusinessName)
            ? provider.BusinessName
            : provider.FullName;
        var safeProviderName = SanitizeProviderNameForFileName(displayName, provider.Id);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".pdf" : extension.ToLowerInvariant();

        return $"{prefix}-{safeProviderName}{safeExtension}";
    }

    private static string SanitizeProviderNameForFileName(string? value, int providerId)
    {
        var fallbackName = $"provider-{providerId}";
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallbackName;
        }

        var sanitized = value.Trim();

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidCharacter, ' ');
        }

        sanitized = Regex.Replace(sanitized, @"[^A-Za-z0-9\s-]", " ");
        sanitized = Regex.Replace(sanitized, @"\s+", "-");
        sanitized = Regex.Replace(sanitized, @"-+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(sanitized) ? fallbackName : sanitized;
    }

    private async Task<AdminEntity?> GetConnectedAdminAsync()
    {
        var adminId = HttpContext.GetAuthenticatedAdminId();
        if (adminId is null)
        {
            return null;
        }

        return await dbContext.Admins
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == adminId
                && item.IsActive
                && (item.Role == AdminRole.SuperAdmin
                    || item.Role == AdminRole.Admin
                    || item.Role == AdminRole.Staff));
    }
}
