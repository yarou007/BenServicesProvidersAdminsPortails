namespace BenServicesPlatform.Api.Entities;

public class ProviderApplicationEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Locksmith";
    public string ServicesOfferedJson { get; set; } = "[]";
    public string CitiesCoveredJson { get; set; } = "[]";
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCodesJson { get; set; } = "[]";
    public int YearsOfExperience { get; set; }
    public bool EmergencyService { get; set; }
    public string WorkingHours { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = "Form";
    public string Status { get; set; } = "Pending";
    public DateTime SubmittedAt { get; set; }
    public string? LicenseFileName { get; set; }
}
