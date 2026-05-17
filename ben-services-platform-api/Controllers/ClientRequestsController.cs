using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;
using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/client-requests")]
[Authorize(Roles = $"{AdminRole.SuperAdmin},{AdminRole.Admin},{AdminRole.Staff}")]
public class ClientRequestsController(
    AppDbContext dbContext,
    IWebHostEnvironment environment,
    ILogger<ClientRequestsController> logger) : ControllerBase
{
    private const long MaxPhotoFileSizeBytes = 10 * 1024 * 1024;
    private const long MultipartBodyLimitBytes = 20 * 1024 * 1024;

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Locksmith",
        "Glass",
        "Door",
        "Board-up",
        "Other"
    };

    private static readonly HashSet<string> AllowedUrgencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Emergency",
        "Scheduled"
    };

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

    [HttpPost("commercial")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartBodyLimitBytes)]
    public async Task<ActionResult<ClientServiceRequestDto>> CreateCommercialAsync([FromForm] CommercialClientRequestCreateRequest request)
    {
        var validationError = ValidateCommercialRequest(request, out var normalizedEmail);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var entity = new ClientServiceRequestEntity
        {
            ClientType = ClientRequestClientType.Commercial,
            CompanyName = request.CompanyName.Trim(),
            ContactName = request.ContactName.Trim(),
            Phone = request.Phone.Trim(),
            Email = normalizedEmail,
            ServiceCategory = NormalizeCategory(request.ServiceCategory),
            Urgency = NormalizeUrgency(request.Urgency),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim().ToUpperInvariant(),
            ZipCode = request.ZipCode.Trim(),
            Description = request.Description.Trim(),
            PreferredDateTime = request.PreferredDateTime,
            Status = ClientServiceRequestStatus.New,
            Source = "PublicWebsite"
        };

        try
        {
            dbContext.ClientServiceRequests.Add(entity);
            await dbContext.SaveChangesAsync();

            entity.PhotoFileUrl = await SaveOptionalPhotoAsync(entity.Id, request.PhotoFile, request.CompanyName);
            await dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByIdAsync), new { id = entity.Id }, entity.ToDto());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create commercial client request. Email={Email}", normalizedEmail);
            return Problem(
                title: "Commercial request submission failed",
                detail: "An unexpected error occurred while submitting your request.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientServiceRequestDto>>> GetAllAsync()
    {
        var requests = await dbContext.ClientServiceRequests
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        return Ok(requests.Select(item => item.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientServiceRequestDto>> GetByIdAsync(int id)
    {
        var request = await dbContext.ClientServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        return Ok(request.ToDto());
    }

    [HttpPut("{id:int}/status")]
    public Task<ActionResult<ClientServiceRequestDto>> UpdateStatusViaPutAsync(int id, [FromBody] ClientServiceRequestStatusUpdateRequest request)
    {
        return UpdateStatusAsync(id, request);
    }

    [HttpPost("{id:int}/status")]
    public Task<ActionResult<ClientServiceRequestDto>> UpdateStatusViaPostAsync(int id, [FromBody] ClientServiceRequestStatusUpdateRequest request)
    {
        return UpdateStatusAsync(id, request);
    }

    [HttpPut("{id:int}/notes")]
    public Task<ActionResult<ClientServiceRequestDto>> UpdateNotesViaPutAsync(int id, [FromBody] ClientServiceRequestNotesUpdateRequest request)
    {
        return UpdateNotesAsync(id, request);
    }

    [HttpPost("{id:int}/notes")]
    public Task<ActionResult<ClientServiceRequestDto>> UpdateNotesViaPostAsync(int id, [FromBody] ClientServiceRequestNotesUpdateRequest request)
    {
        return UpdateNotesAsync(id, request);
    }

    private async Task<ActionResult<ClientServiceRequestDto>> UpdateStatusAsync(int id, ClientServiceRequestStatusUpdateRequest request)
    {
        var entity = await dbContext.ClientServiceRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        var normalizedStatus = request.Status.Trim();
        if (!ClientServiceRequestStatus.IsValid(normalizedStatus))
        {
            return BadRequest(new { message = "Invalid status value." });
        }

        entity.Status = normalizedStatus;
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Ok(entity.ToDto());
    }

    private async Task<ActionResult<ClientServiceRequestDto>> UpdateNotesAsync(int id, ClientServiceRequestNotesUpdateRequest request)
    {
        var entity = await dbContext.ClientServiceRequests.FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        entity.AdminNotes = request.AdminNotes.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Ok(entity.ToDto());
    }

    private string? ValidateCommercialRequest(CommercialClientRequestCreateRequest request, out string normalizedEmail)
    {
        normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return "Company name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.ContactName))
        {
            return "Contact full name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return "Phone is required.";
        }

        if (!MailAddress.TryCreate(normalizedEmail, out _))
        {
            return "A valid email address is required.";
        }

        if (!AllowedCategories.Contains(request.ServiceCategory.Trim()))
        {
            return "Service category is invalid.";
        }

        if (!AllowedUrgencies.Contains(request.Urgency.Trim()))
        {
            return "Urgency must be Emergency or Scheduled.";
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return "Address is required.";
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            return "City is required.";
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            return "State is required.";
        }

        if (string.IsNullOrWhiteSpace(request.ZipCode))
        {
            return "Zip code is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return "Service description is required.";
        }

        var photoValidationError = ValidatePhoto(request.PhotoFile);
        if (photoValidationError is not null)
        {
            return photoValidationError;
        }

        return null;
    }

    private static string? ValidatePhoto(IFormFile? file)
    {
        if (file is null)
        {
            return null;
        }

        if (file.Length <= 0)
        {
            return "Uploaded attachment is empty.";
        }

        if (file.Length > MaxPhotoFileSizeBytes)
        {
            return "Attachment size must be less than 10 MB.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return "Only PDF, JPG, JPEG, and PNG files are allowed for attachments.";
        }

        var contentType = file.ContentType?.Trim() ?? string.Empty;
        if (!AllowedContentTypes.Contains(contentType))
        {
            return "Only PDF, JPG, JPEG, and PNG files are allowed for attachments.";
        }

        return null;
    }

    private async Task<string?> SaveOptionalPhotoAsync(int requestId, IFormFile? file, string companyName)
    {
        if (file is null)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeCompanyName = SanitizeNameForFileName(companyName, $"request-{requestId}");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        var relativeDirectory = Path.Combine("uploads", "client-requests", requestId.ToString());
        var absoluteDirectory = Path.Combine(environment.ContentRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var generatedFileName = $"photo-{safeCompanyName}-{timestamp}-{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteDirectory, generatedFileName);

        await using (var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        return Path.Combine(relativeDirectory, generatedFileName).Replace('\\', '/');
    }

    private static string NormalizeCategory(string category)
    {
        return category.Trim() switch
        {
            "locksmith" or "Locksmith" => "Locksmith",
            "glass" or "Glass" => "Glass",
            "door" or "Door" => "Door",
            "board-up" or "Board-up" or "Board-Up" => "Board-up",
            _ => "Other"
        };
    }

    private static string NormalizeUrgency(string urgency)
    {
        return urgency.Trim().Equals("Emergency", StringComparison.OrdinalIgnoreCase)
            ? "Emergency"
            : "Scheduled";
    }

    private static string SanitizeNameForFileName(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var sanitized = value.Trim();

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidCharacter, ' ');
        }

        sanitized = Regex.Replace(sanitized, @"[^A-Za-z0-9\s-]", " ");
        sanitized = Regex.Replace(sanitized, @"\s+", "-");
        sanitized = Regex.Replace(sanitized, @"-+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }
}
