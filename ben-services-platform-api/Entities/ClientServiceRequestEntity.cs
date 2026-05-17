namespace BenServicesPlatform.Api.Entities;

public class ClientServiceRequestEntity
{
    public int Id { get; set; }
    public string ClientType { get; set; } = ClientRequestClientType.Commercial;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? PreferredDateTime { get; set; }
    public string Status { get; set; } = ClientServiceRequestStatus.New;
    public string Source { get; set; } = "PublicWebsite";
    public string? PhotoFileUrl { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
