namespace BenServicesPlatform.Api.Dtos;

public class AdminCreateRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "ADMIN";
}

public class AdminStatusUpdateRequestDto
{
    public bool IsActive { get; set; }
}
