namespace BenServicesPlatform.Api.Dtos;

public class ProviderApplicationDto
{
    public int Id { get; set; }
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
    public DateTime SubmittedAt { get; set; }
    public string? LicenseFileName { get; set; }
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
