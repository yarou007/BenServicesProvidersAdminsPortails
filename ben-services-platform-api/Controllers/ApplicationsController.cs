using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;
using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Mapping;
using BenServicesPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AdminRole.SuperAdmin},{AdminRole.Admin},{AdminRole.Staff}")]
public class ApplicationsController(
    AppDbContext dbContext,
    IPasswordHasher<ProviderAccountEntity> providerPasswordHasher,
    IEmailService emailService,
    IWebHostEnvironment environment,
    ILogger<ApplicationsController> logger) : ControllerBase
{
    private const string GetApplicationDocumentRouteName = "GetApplicationDocumentById";
    private const long MaxDocumentFileSizeBytes = 10 * 1024 * 1024;
    private const long MultipartBodyLimitBytes = 30 * 1024 * 1024;

    private static readonly HashSet<string> AllowedServiceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Locksmith",
        "Glass",
        "Both"
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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderApplicationDto>>> GetAllAsync()
    {
        var applications = await dbContext.ProviderApplications
            .AsNoTracking()
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync();

        return Ok(applications.Select(ToApplicationDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProviderApplicationDto>> GetByIdAsync(int id)
    {
        var application = await dbContext.ProviderApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        return Ok(ToApplicationDto(application));
    }

    [HttpGet("{id:int}/documents/{documentType}", Name = GetApplicationDocumentRouteName)]
    public async Task<IActionResult> DownloadDocumentAsync(int id, string documentType)
    {
        var normalizedDocumentType = NormalizeDocumentType(documentType);
        if (normalizedDocumentType is null)
        {
            return BadRequest(new { message = "Invalid document type. Allowed values are 'license', 'insurance', and 'w9'." });
        }

        var application = await dbContext.ProviderApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        var relativePath = GetDocumentRelativePath(application, normalizedDocumentType);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return NotFound(new { message = "Document metadata is missing for this application." });
        }

        if (!TryResolveApplicationDocumentPath(id, relativePath, out var absolutePath, out _, out _))
        {
            return NotFound(new { message = "Document metadata path is invalid or missing." });
        }

        if (!System.IO.File.Exists(absolutePath))
        {
            return NotFound(new { message = "Document file was not found on disk." });
        }

        var extension = Path.GetExtension(absolutePath);
        var contentType = GetContentTypeFromExtension(extension);
        var safeBusinessName = SanitizeNameForFileName(application.BusinessName, $"application-{id}");
        var downloadName = $"{normalizedDocumentType}-{safeBusinessName}{extension}";
        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return File(stream, contentType, downloadName);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ProviderApplicationDto>> CreateAsync([FromBody] ProviderApplicationCreateRequest request)
    {
        var entity = request.ToEntity();
        entity.Status = ProviderApplicationStatus.Pending;

        dbContext.ProviderApplications.Add(entity);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = entity.Id }, ToApplicationDto(entity));
    }

    [HttpPost("apply")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartBodyLimitBytes)]
    public async Task<ActionResult<ProviderApplicationSubmissionResponseDto>> ApplyAsync([FromForm] ProviderApplicationApplyRequest request)
    {
        var validationError = ValidateApplyRequest(
            request,
            out var normalizedEmail,
            out var normalizedBusinessName,
            out var servicesOffered,
            out var statesCovered,
            out var citiesCovered,
            out var zipCodes);

        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var missingComplianceDocumentErrors = GetMissingComplianceDocumentErrors(request);
        if (missingComplianceDocumentErrors.Count > 0)
        {
            return BadRequest(new
            {
                message = "Missing required compliance documents.",
                errors = missingComplianceDocumentErrors
            });
        }

        var existingOpenApplication = await dbContext.ProviderApplications
            .AnyAsync(item => item.Email.ToLower() == normalizedEmail && item.Status != ProviderApplicationStatus.Rejected);

        if (existingOpenApplication)
        {
            return Conflict(new { message = "An active application already exists with this email." });
        }

        var existingProvider = await dbContext.Providers
            .AnyAsync(item => item.Email.ToLower() == normalizedEmail && item.IsActive);

        if (existingProvider)
        {
            return Conflict(new { message = "An active provider already exists with this email." });
        }

        var now = DateTime.UtcNow;

        var application = new ProviderApplicationEntity
        {
            FullName = request.FullName.Trim(),
            BusinessName = normalizedBusinessName,
            StreetAddress = request.StreetAddress.Trim(),
            Phone = request.Phone.Trim(),
            Email = normalizedEmail,
            ServiceType = NormalizeServiceType(request.ServiceType),
            ServicesOfferedJson = JsonArrayMapper.Serialize(servicesOffered),
            StatesJson = JsonArrayMapper.Serialize(statesCovered),
            CitiesCoveredJson = JsonArrayMapper.Serialize(citiesCovered),
            City = citiesCovered[0],
            State = statesCovered[0],
            ZipCodesJson = JsonArrayMapper.Serialize(zipCodes),
            YearsOfExperience = request.YearsOfExperience,
            EmergencyService = request.EmergencyService,
            WorkingHours = request.WorkingHours.Trim(),
            Message = request.Message.Trim(),
            Source = "PublicWebsite",
            Status = ProviderApplicationStatus.Pending,
            SubmittedAt = now,
            UpdatedAt = now,
            LicenseFileName = string.IsNullOrWhiteSpace(request.LicenseDocument?.FileName)
                ? null
                : Path.GetFileName(request.LicenseDocument!.FileName)
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            dbContext.ProviderApplications.Add(application);
            await dbContext.SaveChangesAsync();

            application.LicenseFileUrl = await SaveOptionalApplicationDocumentAsync(
                application.Id,
                request.LicenseDocument,
                "license",
                normalizedBusinessName);

            application.InsuranceFileUrl = await SaveOptionalApplicationDocumentAsync(
                application.Id,
                request.InsuranceDocument,
                "insurance",
                normalizedBusinessName);

            application.W9FileUrl = await SaveOptionalApplicationDocumentAsync(
                application.Id,
                request.W9Document,
                "w9",
                normalizedBusinessName);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();

            if (application.Id > 0)
            {
                DeleteApplicationUploadDirectoryIfExists(application.Id);
            }

            logger.LogError(exception, "Provider application submission failed. Email={Email}", normalizedEmail);

            return Problem(
                title: "Application submission failed",
                detail: "An unexpected error occurred while submitting the application.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var emailDeliveryFailed = false;

        try
        {
            await emailService.SendProviderApplicationReceivedAsync(
                application.FullName,
                application.Email);
        }
        catch (Exception exception)
        {
            emailDeliveryFailed = true;
            logger.LogWarning(
                exception,
                "Provider application submitted but confirmation email failed. ApplicationId={ApplicationId}, Email={Email}",
                application.Id,
                normalizedEmail);
        }

        return Ok(new ProviderApplicationSubmissionResponseDto
        {
            ApplicationId = application.Id,
            Status = application.Status,
            Message = emailDeliveryFailed
                ? "Application submitted successfully and is pending admin review. Confirmation email could not be sent at this time."
                : "Application submitted successfully and is pending admin review."
        });
    }

    [HttpPost("{id:int}/mark-under-review")]
    public async Task<ActionResult<ProviderApplicationDto>> MarkUnderReviewAsync(int id)
    {
        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        if (application.Status != ProviderApplicationStatus.Pending
            && application.Status != ProviderApplicationStatus.MissingInfo)
        {
            return BadRequest(new { message = "This application cannot be moved to UnderReview." });
        }

        application.Status = ProviderApplicationStatus.UnderReview;
        application.ReviewedAt ??= DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync();
            await emailService.SendProviderApplicationUnderReviewAsync(application.FullName, application.Email);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to mark application under review. ApplicationId={ApplicationId}", id);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Unable to send under-review email. Please retry." });
        }

        return Ok(ToApplicationDto(application));
    }

    [HttpPost("{id:int}/request-missing-info")]
    public async Task<ActionResult<ProviderApplicationDto>> RequestMissingInfoAsync(int id, [FromBody] ApplicationReasonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Missing info reason is required." });
        }

        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        if (application.Status is ProviderApplicationStatus.Converted
            or ProviderApplicationStatus.Rejected
            or ProviderApplicationStatus.Verified)
        {
            return BadRequest(new { message = "This application cannot be moved to MissingInfo." });
        }

        application.Status = ProviderApplicationStatus.MissingInfo;
        application.MissingInfoReason = request.Reason.Trim();
        application.ReviewedAt ??= DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        if (application.UserId is not null)
        {
            var account = await dbContext.ProviderAccounts.FirstOrDefaultAsync(item => item.Id == application.UserId);
            if (account is not null && account.Status != ProviderAccountStatus.Active)
            {
                account.Status = ProviderAccountStatus.PendingApproval;
            }
        }

        try
        {
            await dbContext.SaveChangesAsync();
            await emailService.SendProviderApplicationMissingInfoAsync(
                application.FullName,
                application.Email,
                application.MissingInfoReason);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to request missing info. ApplicationId={ApplicationId}", id);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Unable to send missing-info email. Please retry." });
        }

        return Ok(ToApplicationDto(application));
    }

    [HttpPost("{id:int}/request-more-info")]
    public Task<ActionResult<ProviderApplicationDto>> RequestMoreInfoLegacyAsync(int id, [FromBody] ApplicationReasonRequest? request)
    {
        return RequestMissingInfoAsync(id, request ?? new ApplicationReasonRequest { Reason = "Additional information is required." });
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<ProviderApplicationDto>> RejectAsync(int id, [FromBody] ApplicationReasonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Rejection reason is required." });
        }

        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        if (application.Status == ProviderApplicationStatus.Converted)
        {
            return BadRequest(new { message = "Converted applications cannot be rejected." });
        }

        application.Status = ProviderApplicationStatus.Rejected;
        application.RejectionReason = request.Reason.Trim();
        application.RejectedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        if (application.UserId is not null)
        {
            var account = await dbContext.ProviderAccounts.FirstOrDefaultAsync(item => item.Id == application.UserId);
            if (account is not null)
            {
                account.Status = ProviderAccountStatus.Rejected;
            }
        }

        try
        {
            await dbContext.SaveChangesAsync();
            await emailService.SendProviderApplicationRejectedAsync(
                application.FullName,
                application.Email,
                application.RejectionReason);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to reject application. ApplicationId={ApplicationId}", id);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Unable to send rejection email. Please retry." });
        }

        return Ok(ToApplicationDto(application));
    }

    [HttpPost("{id:int}/accept")]
    public async Task<ActionResult<ProviderApplicationDto>> AcceptAsync(int id)
    {
        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);
        if (application is null)
        {
            return NotFound();
        }

        if (application.Status is ProviderApplicationStatus.Converted or ProviderApplicationStatus.Rejected)
        {
            return BadRequest(new { message = "This application cannot be accepted." });
        }

        if (!CanMoveToAccepted(application.Status))
        {
            return BadRequest(new { message = "Application status does not allow accept action." });
        }

        application.Status = ProviderApplicationStatus.Accepted;
        application.ReviewedAt ??= DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(ToApplicationDto(application));
    }

    [HttpPost("{id:int}/approve")]
    public Task<ActionResult<ProviderApplicationDto>> ApproveLegacyAsync(int id)
    {
        return AcceptAsync(id);
    }

    [HttpPost("{id:int}/verify")]
    public async Task<ActionResult<ProviderApplicationDto>> VerifyAsync(int id, [FromBody] ApplicationVerificationRequest? request)
    {
        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);
        if (application is null)
        {
            return NotFound();
        }

        if (application.Status == ProviderApplicationStatus.Rejected)
        {
            return BadRequest(new { message = "Rejected applications cannot be verified." });
        }

        if (application.Status == ProviderApplicationStatus.Converted)
        {
            return BadRequest(new { message = "Converted applications are already activated." });
        }

        if (application.Status == ProviderApplicationStatus.Verified)
        {
            return BadRequest(new { message = "Application is already verified." });
        }

        if (application.Status != ProviderApplicationStatus.Accepted)
        {
            return BadRequest(new { message = "Only accepted applications can be verified." });
        }

        var now = DateTime.UtcNow;
        var shouldIssueNewCredentials = true;

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var provider = await UpsertProviderFromApplicationAsync(application, now);
            application.ConvertedProviderId ??= provider.Id;

            var temporaryPassword = shouldIssueNewCredentials ? CredentialGenerator.GenerateTemporaryPassword() : null;
            var account = await CreateOrUpdateProviderAccountAsync(application, temporaryPassword, now);

            application.UserId = account.Id;
            application.Status = ProviderApplicationStatus.Verified;
            application.VerificationNotes = request?.VerificationNotes?.Trim();
            application.VerifiedAt = now;
            application.ReviewedAt ??= now;
            application.UpdatedAt = now;

            await dbContext.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(temporaryPassword))
            {
                await emailService.SendProviderCredentialsAsync(application.FullName, application.Email, temporaryPassword);
            }

            await transaction.CommitAsync();
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            logger.LogError(exception, "Failed to verify provider application. ApplicationId={ApplicationId}", id);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Verification failed. Please retry." });
        }

        return Ok(ToApplicationDto(application));
    }

    [HttpPost("{id:int}/convert-to-provider")]
    public async Task<ActionResult<ProviderApplicationDto>> ConvertToProviderAsync(int id)
    {
        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);
        if (application is null)
        {
            return NotFound();
        }

        if (application.Status == ProviderApplicationStatus.Converted)
        {
            return BadRequest(new { message = "Application has already been converted." });
        }

        if (application.Status != ProviderApplicationStatus.Accepted && application.Status != ProviderApplicationStatus.Verified)
        {
            return BadRequest(new { message = "Only Accepted or Verified applications can be converted." });
        }

        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var provider = await UpsertProviderFromApplicationAsync(application, now);
            application.ConvertedProviderId = provider.Id;

            if (application.Status == ProviderApplicationStatus.Accepted)
            {
                var temporaryPassword = CredentialGenerator.GenerateTemporaryPassword();
                var account = await CreateOrUpdateProviderAccountAsync(application, temporaryPassword, now);
                application.UserId = account.Id;
                application.VerifiedAt = now;
                application.VerificationNotes ??= "Auto-verified during conversion.";
                await emailService.SendProviderCredentialsAsync(application.FullName, application.Email, temporaryPassword);
            }
            else if (application.UserId is not null)
            {
                var account = await dbContext.ProviderAccounts.FirstOrDefaultAsync(item => item.Id == application.UserId);
                if (account is not null)
                {
                    account.Status = ProviderAccountStatus.Active;
                }
            }

            application.Status = ProviderApplicationStatus.Converted;
            application.ReviewedAt ??= now;
            application.UpdatedAt = now;

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            logger.LogError(exception, "Failed to convert provider application. ApplicationId={ApplicationId}", id);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Convert action failed. Please retry." });
        }

        return Ok(ToApplicationDto(application));
    }

    [HttpPut("{id:int}/notes")]
    public Task<ActionResult<ProviderApplicationDto>> UpdateNotesViaPutAsync(int id, [FromBody] ApplicationNotesUpdateRequest request)
    {
        return UpdateNotesAsync(id, request);
    }

    [HttpPost("{id:int}/notes")]
    public Task<ActionResult<ProviderApplicationDto>> UpdateNotesViaPostAsync(int id, [FromBody] ApplicationNotesUpdateRequest request)
    {
        return UpdateNotesAsync(id, request);
    }

    private async Task<ActionResult<ProviderApplicationDto>> UpdateNotesAsync(int id, ApplicationNotesUpdateRequest request)
    {
        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        application.AdminNotes = request.AdminNotes.Trim();
        application.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Ok(ToApplicationDto(application));
    }

    private async Task<ProviderEntity> UpsertProviderFromApplicationAsync(ProviderApplicationEntity application, DateTime now)
    {
        ProviderEntity? provider = null;

        if (application.ConvertedProviderId is not null)
        {
            provider = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == application.ConvertedProviderId.Value);
        }

        provider ??= await dbContext.Providers.FirstOrDefaultAsync(item => item.Email.ToLower() == application.Email.ToLower());

        if (provider is null)
        {
            provider = new ProviderEntity();
            dbContext.Providers.Add(provider);
        }

        provider.FullName = application.FullName;
        provider.BusinessName = application.BusinessName;
        provider.StreetAddress = application.StreetAddress;
        provider.Phone = application.Phone;
        provider.Email = application.Email;
        provider.ServiceType = application.ServiceType;
        provider.ServicesOfferedJson = application.ServicesOfferedJson;
        provider.StatesJson = application.StatesJson;
        provider.City = application.City;
        provider.State = application.State;
        provider.ZipCodesJson = application.ZipCodesJson;
        provider.Region = MapRegionByState(application.State);
        provider.EmergencyService = application.EmergencyService;
        provider.Availability = application.EmergencyService ? "Daily" : "Weekdays";
        provider.WorkingHours = application.WorkingHours;
        provider.VerificationStatus = "Active";
        provider.IsActive = true;
        provider.Source = application.Source;
        provider.YearsOfExperience = application.YearsOfExperience;
        provider.Notes = application.Message;
        provider.AdminComments = string.IsNullOrWhiteSpace(application.AdminNotes)
            ? $"Imported from provider application #{application.Id}."
            : application.AdminNotes;
        provider.CreatedAt = provider.CreatedAt == default ? now : provider.CreatedAt;
        provider.UpdatedAt = now;
        provider.VerifiedAt ??= now;

        await dbContext.SaveChangesAsync();

        var shouldImportW9 =
            !string.IsNullOrWhiteSpace(application.W9FileUrl)
            && (string.IsNullOrWhiteSpace(provider.W9FilePath)
                || provider.W9FilePath.StartsWith("uploads/provider-applications/", StringComparison.OrdinalIgnoreCase));
        if (shouldImportW9)
        {
            var importedW9Path = await CopyApplicationDocumentToProviderAsync(
                application.Id,
                provider.Id,
                application.W9FileUrl,
                "w9");
            if (!string.IsNullOrWhiteSpace(importedW9Path))
            {
                provider.W9FilePath = importedW9Path;
                provider.W9UploadedAt = now;
            }
            else if (string.IsNullOrWhiteSpace(provider.W9FilePath))
            {
                // Keep legacy references for backward compatibility when copy is not possible.
                provider.W9FilePath = application.W9FileUrl;
                provider.W9UploadedAt ??= now;
            }
        }

        var shouldImportCoi =
            !string.IsNullOrWhiteSpace(application.InsuranceFileUrl)
            && (string.IsNullOrWhiteSpace(provider.CoiFilePath)
                || provider.CoiFilePath.StartsWith("uploads/provider-applications/", StringComparison.OrdinalIgnoreCase));
        if (shouldImportCoi)
        {
            var importedCoiPath = await CopyApplicationDocumentToProviderAsync(
                application.Id,
                provider.Id,
                application.InsuranceFileUrl,
                "coi");
            if (!string.IsNullOrWhiteSpace(importedCoiPath))
            {
                provider.CoiFilePath = importedCoiPath;
                provider.CoiUploadedAt = now;
            }
            else if (string.IsNullOrWhiteSpace(provider.CoiFilePath))
            {
                // Keep legacy references for backward compatibility when copy is not possible.
                provider.CoiFilePath = application.InsuranceFileUrl;
                provider.CoiUploadedAt ??= now;
            }
        }

        await dbContext.SaveChangesAsync();

        return provider;
    }

    private async Task<ProviderAccountEntity> CreateOrUpdateProviderAccountAsync(
        ProviderApplicationEntity application,
        string? temporaryPassword,
        DateTime now)
    {
        ProviderAccountEntity? account = null;

        if (application.UserId is not null)
        {
            account = await dbContext.ProviderAccounts.FirstOrDefaultAsync(item => item.Id == application.UserId.Value);
        }

        account ??= await dbContext.ProviderAccounts.FirstOrDefaultAsync(item => item.Email == application.Email.ToLower());

        var hasTemporaryPassword = !string.IsNullOrWhiteSpace(temporaryPassword);

        if (account is null)
        {
            if (!hasTemporaryPassword)
            {
                throw new InvalidOperationException("A temporary password is required to create a provider account.");
            }

            account = new ProviderAccountEntity
            {
                Email = application.Email.ToLower(),
                Role = ProviderAccountRole.Provider,
                Status = ProviderAccountStatus.Active,
                MustChangePassword = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.ProviderAccounts.Add(account);
        }
        else
        {
            account.Email = application.Email.ToLower();
            account.Role = ProviderAccountRole.Provider;
            account.Status = ProviderAccountStatus.Active;
            account.MustChangePassword = hasTemporaryPassword || account.MustChangePassword;
            account.UpdatedAt = now;
        }

        if (hasTemporaryPassword)
        {
            account.PasswordHash = providerPasswordHasher.HashPassword(account, temporaryPassword!);
        }

        await dbContext.SaveChangesAsync();
        return account;
    }

    private ProviderApplicationDto ToApplicationDto(ProviderApplicationEntity entity)
    {
        var dto = entity.ToDto();

        dto.LicenseFileUrl = ResolveApplicationDocumentUrl(entity.Id, "license", entity.LicenseFileUrl);
        dto.InsuranceFileUrl = ResolveApplicationDocumentUrl(entity.Id, "insurance", entity.InsuranceFileUrl);
        dto.W9FileUrl = ResolveApplicationDocumentUrl(entity.Id, "w9", entity.W9FileUrl);

        return dto;
    }

    private static bool CanMoveToAccepted(string currentStatus)
    {
        return currentStatus is ProviderApplicationStatus.Pending
            or ProviderApplicationStatus.UnderReview
            or ProviderApplicationStatus.MissingInfo;
    }

    private static string? NormalizeDocumentType(string documentType)
    {
        return documentType.Trim().ToLowerInvariant() switch
        {
            "license" => "license",
            "insurance" => "insurance",
            "w9" => "w9",
            _ => null
        };
    }

    private static string? GetDocumentRelativePath(ProviderApplicationEntity application, string documentType)
    {
        return documentType switch
        {
            "license" => application.LicenseFileUrl,
            "insurance" => application.InsuranceFileUrl,
            "w9" => application.W9FileUrl,
            _ => null
        };
    }

    private string? ResolveApplicationDocumentUrl(int applicationId, string documentType, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        if (!TryResolveApplicationDocumentPath(applicationId, relativePath, out var absolutePath, out _, out _))
        {
            return null;
        }

        if (!System.IO.File.Exists(absolutePath))
        {
            return null;
        }

        return Url.RouteUrl(
            GetApplicationDocumentRouteName,
            new { id = applicationId, documentType },
            Request.Scheme);
    }

    private string? ValidateApplyRequest(
        ProviderApplicationApplyRequest request,
        out string normalizedEmail,
        out string normalizedBusinessName,
        out string[] servicesOffered,
        out string[] statesCovered,
        out string[] citiesCovered,
        out string[] zipCodes)
    {
        normalizedEmail = request.Email.Trim().ToLowerInvariant();
        normalizedBusinessName = request.BusinessName.Trim();
        servicesOffered = ParseStringArray(request.ServicesOfferedJson);
        statesCovered = ParseStringArray(request.StatesJson);
        if (statesCovered.Length == 0 && !string.IsNullOrWhiteSpace(request.State))
        {
            statesCovered = [request.State];
        }

        statesCovered = statesCovered
            .Select(item => item.Trim().ToUpperInvariant())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        citiesCovered = ParseStringArray(request.CitiesCoveredJson);
        zipCodes = ParseStringArray(request.ZipCodesJson);

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return "Full name is required.";
        }

        if (string.IsNullOrWhiteSpace(normalizedBusinessName))
        {
            return "Business name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.StreetAddress))
        {
            return "Street address is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return "Phone is required.";
        }

        if (!MailAddress.TryCreate(normalizedEmail, out _))
        {
            return "A valid email address is required.";
        }

        if (!AllowedServiceTypes.Contains(request.ServiceType.Trim()))
        {
            return "Service type must be Locksmith, Glass, or Both.";
        }

        if (servicesOffered.Length == 0)
        {
            return "At least one service offered is required.";
        }

        if (statesCovered.Length == 0)
        {
            return "At least one state is required.";
        }

        if (citiesCovered.Length == 0)
        {
            return "At least one city covered is required.";
        }

        if (zipCodes.Length == 0)
        {
            return "At least one zip code is required.";
        }

        if (request.YearsOfExperience < 0)
        {
            return "Years of experience must be greater than or equal to 0.";
        }

        if (string.IsNullOrWhiteSpace(request.WorkingHours))
        {
            return "Working hours are required.";
        }

        var licenseValidationError = ValidateFile(request.LicenseDocument, "License document");
        if (licenseValidationError is not null)
        {
            return licenseValidationError;
        }

        var insuranceValidationError = ValidateFile(request.InsuranceDocument, "Insurance document");
        if (insuranceValidationError is not null)
        {
            return insuranceValidationError;
        }

        var w9ValidationError = ValidateFile(request.W9Document, "W-9 document");
        if (w9ValidationError is not null)
        {
            return w9ValidationError;
        }

        return null;
    }

    private static Dictionary<string, string> GetMissingComplianceDocumentErrors(ProviderApplicationApplyRequest request)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (request.LicenseDocument is null)
        {
            errors["licenseDocument"] = "License document is required.";
        }

        if (request.W9Document is null)
        {
            errors["w9Document"] = "W-9 document is required.";
        }

        if (request.InsuranceDocument is null)
        {
            errors["coiDocument"] = "Insurance / COI document is required.";
        }

        return errors;
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
            return "Only PDF, JPG, JPEG, and PNG files are allowed.";
        }

        var contentType = file.ContentType?.Trim() ?? string.Empty;
        if (!AllowedContentTypes.Contains(contentType))
        {
            return "Only PDF, JPG, JPEG, and PNG files are allowed.";
        }

        return null;
    }

    private async Task<string?> SaveOptionalApplicationDocumentAsync(
        int applicationId,
        IFormFile? file,
        string prefix,
        string businessName)
    {
        if (file is null)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var safeBusinessName = SanitizeNameForFileName(businessName, $"application-{applicationId}");

        var relativeDirectory = Path.Combine("uploads", "provider-applications", applicationId.ToString());
        var absoluteDirectory = Path.Combine(environment.ContentRootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var generatedFileName = $"{prefix}-{safeBusinessName}-{timestamp}-{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteDirectory, generatedFileName);

        await using (var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        return Path.Combine(relativeDirectory, generatedFileName).Replace('\\', '/');
    }

    private async Task<string?> CopyApplicationDocumentToProviderAsync(
        int applicationId,
        int providerId,
        string? applicationRelativePath,
        string prefix)
    {
        if (string.IsNullOrWhiteSpace(applicationRelativePath))
        {
            return null;
        }

        if (!TryResolveApplicationDocumentPath(
                applicationId,
                applicationRelativePath,
                out var applicationAbsolutePath,
                out _,
                out var pathFailureReason))
        {
            logger.LogWarning(
                "Skipping provider document copy due to invalid application path. ApplicationId={ApplicationId}, ProviderId={ProviderId}, RelativePath={RelativePath}, Reason={Reason}",
                applicationId,
                providerId,
                applicationRelativePath,
                pathFailureReason);
            return null;
        }

        if (!System.IO.File.Exists(applicationAbsolutePath))
        {
            logger.LogWarning(
                "Skipping provider document copy because source file does not exist. ApplicationId={ApplicationId}, ProviderId={ProviderId}, RelativePath={RelativePath}",
                applicationId,
                providerId,
                applicationRelativePath);
            return null;
        }

        try
        {
            var extension = Path.GetExtension(applicationAbsolutePath).ToLowerInvariant();
            var relativeDirectory = Path.Combine("uploads", "providers", providerId.ToString());
            var absoluteDirectory = Path.Combine(environment.ContentRootPath, relativeDirectory);
            Directory.CreateDirectory(absoluteDirectory);

            var generatedFileName = $"{prefix}-{Guid.NewGuid():N}{extension}";
            var providerAbsolutePath = Path.Combine(absoluteDirectory, generatedFileName);

            await using (var sourceStream = new FileStream(applicationAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using (var destinationStream = new FileStream(providerAbsolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }

            return Path.Combine(relativeDirectory, generatedFileName).Replace('\\', '/');
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to copy application document into provider storage. ApplicationId={ApplicationId}, ProviderId={ProviderId}, RelativePath={RelativePath}",
                applicationId,
                providerId,
                applicationRelativePath);
            return null;
        }
    }

    private void DeleteApplicationUploadDirectoryIfExists(int applicationId)
    {
        var absoluteDirectory = Path.Combine(environment.ContentRootPath, "uploads", "provider-applications", applicationId.ToString());
        if (Directory.Exists(absoluteDirectory))
        {
            Directory.Delete(absoluteDirectory, recursive: true);
        }
    }

    private bool TryResolveApplicationDocumentPath(
        int applicationId,
        string relativePath,
        out string absolutePath,
        out string normalizedRelativePath,
        out string failureReason)
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

        var expectedPrefix = $"uploads/provider-applications/{applicationId}/";
        if (!normalizedRelativePath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            failureReason = $"Path must start with '{expectedPrefix}'.";
            return false;
        }

        var combinedPath = Path.Combine(
            environment.ContentRootPath,
            normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));

        var fullPath = Path.GetFullPath(combinedPath);
        var applicationRoot = Path.GetFullPath(
            Path.Combine(environment.ContentRootPath, "uploads", "provider-applications", applicationId.ToString()));

        var expectedRootPrefix = applicationRoot + Path.DirectorySeparatorChar;
        var isInsideRoot =
            fullPath.StartsWith(expectedRootPrefix, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fullPath, applicationRoot, StringComparison.OrdinalIgnoreCase);

        if (!isInsideRoot)
        {
            failureReason = "Path escapes application upload root.";
            return false;
        }

        absolutePath = fullPath;
        return true;
    }

    private static string[] ParseStringArray(string raw)
    {
        var fromJson = JsonArrayMapper.Deserialize(raw);
        if (fromJson.Length > 0)
        {
            return fromJson
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeServiceType(string serviceType)
    {
        return serviceType.Trim().ToLowerInvariant() switch
        {
            "locksmith" => "Locksmith",
            "glass" => "Glass",
            "both" => "Both",
            _ => "Locksmith"
        };
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

    private static string MapRegionByState(string state)
    {
        return state.Trim().ToUpperInvariant() switch
        {
            "DC" => "DC Metro",
            "VA" => "Virginia",
            "MD" => "Maryland",
            "NY" => "New York",
            "TX" => "South",
            "AZ" => "Southwest",
            "FL" => "Southeast",
            _ => "Atlantic"
        };
    }
}
