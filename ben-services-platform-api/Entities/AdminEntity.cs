namespace BenServicesPlatform.Api.Entities;

public class AdminEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = AdminRole.Admin;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedByAdminId { get; set; }
    public AdminEntity? CreatedByAdmin { get; set; }
    public ICollection<AdminEntity> CreatedAdmins { get; set; } = [];
}
