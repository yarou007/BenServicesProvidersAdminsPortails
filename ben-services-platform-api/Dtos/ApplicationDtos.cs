using Microsoft.AspNetCore.Http;

namespace BenServicesPlatform.Api.Dtos;

public class ProviderApplicationDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Locksmith";
    public string[] ServicesOffered { get; set; } = [];
    public string[] CitiesCovered { get; set; } = [];
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string[] ZipCodes { get; set; } = [];
    public int YearsOfExperience { get; set; }
    public bool EmergencyService { get; set; }
    public string WorkingHours { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = "Form";
    public string Status { get; set; } = "Pending";
    public string? AdminNotes { get; set; }
    public string? MissingInfoReason { get; set; }
    public string? RejectionReason { get; set; }
    public string? VerificationNotes { get; set; }
    public int? ConvertedProviderId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? LicenseFileName { get; set; }
    public string? LicenseFileUrl { get; set; }
    public string? InsuranceFileUrl { get; set; }
    public string? W9FileUrl { get; set; }
}

public class ProviderApplicationCreateRequest
{
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Locksmith";
    public string[] ServicesOffered { get; set; } = [];
    public string[] CitiesCovered { get; set; } = [];
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string[] ZipCodes { get; set; } = [];
    public int YearsOfExperience { get; set; }
    public bool EmergencyService { get; set; }
    public string WorkingHours { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LicenseFileName { get; set; }
}

public class ProviderApplicationApplyRequest
{
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Locksmith";
    public string ServicesOfferedJson { get; set; } = "[]";
    public string CitiesCoveredJson { get; set; } = "[]";
    public string State { get; set; } = string.Empty;
    public string ZipCodesJson { get; set; } = "[]";
    public int YearsOfExperience { get; set; }
    public bool EmergencyService { get; set; }
    public string WorkingHours { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public IFormFile? LicenseDocument { get; set; }
    public IFormFile? InsuranceDocument { get; set; }
    public IFormFile? W9Document { get; set; }
}

public class ProviderApplicationSubmissionResponseDto
{
    public int ApplicationId { get; set; }
    public string Status { get; set; } = "Pending";
    public string Message { get; set; } = string.Empty;
}

public class ApplicationNotesUpdateRequest
{
    public string AdminNotes { get; set; } = string.Empty;
}

public class ApplicationReasonRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class ApplicationVerificationRequest
{
    public string? VerificationNotes { get; set; }
}
