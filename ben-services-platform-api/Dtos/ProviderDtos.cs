using Microsoft.AspNetCore.Http;

namespace BenServicesPlatform.Api.Dtos;

public class ProviderDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Locksmith";
    public string[] ServicesOffered { get; set; } = [];
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string[] ZipCodes { get; set; } = [];
    public string Region { get; set; } = string.Empty;
    public bool EmergencyService { get; set; }
    public string Availability { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "New";
    public bool IsActive { get; set; }
    public string Source { get; set; } = "Manual";
    public int YearsOfExperience { get; set; }
    public string? Notes { get; set; }
    public string? AdminComments { get; set; }
    public bool HasW9File { get; set; }
    public bool HasCoiFile { get; set; }
    public string? W9FileUrl { get; set; }
    public string? CoiFileUrl { get; set; }
    public DateTime? W9UploadedAt { get; set; }
    public DateTime? CoiUploadedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public interface IProviderUpsertPayload
{
    public string FullName { get; set; }
    public string BusinessName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string ServiceType { get; set; }
    public string[] ServicesOffered { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string[] ZipCodes { get; set; }
    public string Region { get; set; }
    public bool EmergencyService { get; set; }
    public string Availability { get; set; }
    public string WorkingHours { get; set; }
    public string VerificationStatus { get; set; }
    public bool IsActive { get; set; }
    public string Source { get; set; }
    public int YearsOfExperience { get; set; }
    public string? Notes { get; set; }
    public string? AdminComments { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public class ProviderUpsertRequest : IProviderUpsertPayload
{
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Locksmith";
    public string[] ServicesOffered { get; set; } = [];
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string[] ZipCodes { get; set; } = [];
    public string Region { get; set; } = string.Empty;
    public bool EmergencyService { get; set; }
    public string Availability { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "New";
    public bool IsActive { get; set; }
    public string Source { get; set; } = "Manual";
    public int YearsOfExperience { get; set; }
    public string? Notes { get; set; }
    public string? AdminComments { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public class ProviderCreateRequest : IProviderUpsertPayload
{
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Locksmith";
    public string[] ServicesOffered { get; set; } = [];
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string[] ZipCodes { get; set; } = [];
    public string Region { get; set; } = string.Empty;
    public bool EmergencyService { get; set; }
    public string Availability { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "New";
    public bool IsActive { get; set; }
    public string Source { get; set; } = "Manual";
    public int YearsOfExperience { get; set; }
    public string? Notes { get; set; }
    public string? AdminComments { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public IFormFile? W9File { get; set; }
    public IFormFile? CoiFile { get; set; }
}

public class ProviderDocumentsUploadRequest
{
    public IFormFile? W9File { get; set; }
    public IFormFile? CoiFile { get; set; }
}
