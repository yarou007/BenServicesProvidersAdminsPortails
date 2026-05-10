namespace BenServicesPlatform.Api.Entities;

public class ProviderEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Locksmith";
    public string ServicesOfferedJson { get; set; } = "[]";
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCodesJson { get; set; } = "[]";
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
    public string? W9FilePath { get; set; }
    public string? CoiFilePath { get; set; }
    public DateTime? W9UploadedAt { get; set; }
    public DateTime? CoiUploadedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
