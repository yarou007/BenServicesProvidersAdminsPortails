using Microsoft.AspNetCore.Http;

namespace BenServicesPlatform.Api.Dtos;

public class ClientServiceRequestDto
{
    public int Id { get; set; }
    public string ClientType { get; set; } = "Commercial";
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
    public string Status { get; set; } = "New";
    public string Source { get; set; } = "PublicWebsite";
    public string? AdminNotes { get; set; }
    public string? PhotoFileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CommercialClientRequestCreateRequest
{
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
    public IFormFile? PhotoFile { get; set; }
}

public class ClientServiceRequestStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}

public class ClientServiceRequestNotesUpdateRequest
{
    public string AdminNotes { get; set; } = string.Empty;
}
