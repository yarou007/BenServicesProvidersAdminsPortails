using System.Net.Mail;
using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Extensions;
using BenServicesPlatform.Api.Mapping;
using BenServicesPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminsController(
    AppDbContext dbContext,
    IPasswordHasher<AdminEntity> passwordHasher,
    IEmailService emailService,
    ILogger<AdminsController> logger) : ControllerBase
{
    private const string GetAdminByIdRouteName = "GetAdminById";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminResponseDto>>> GetAllAsync()
    {
        var admins = await dbContext.Admins
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        return Ok(admins.Select(item => item.ToDto()));
    }

    [HttpGet("{id:int}", Name = GetAdminByIdRouteName)]
    public async Task<ActionResult<AdminResponseDto>> GetByIdAsync(int id)
    {
        var admin = await dbContext.Admins
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (admin is null)
        {
            return NotFound();
        }

        return Ok(admin.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<AdminResponseDto>> CreateAsync([FromBody] AdminCreateRequestDto request)
    {
        var connectedAdmin = await GetConnectedAdminAsync();
        if (connectedAdmin is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "Full name is required." });
        }

        if (!MailAddress.TryCreate(request.Email.Trim(), out _))
        {
            return BadRequest(new { message = "Email format is invalid." });
        }

        var role = string.IsNullOrWhiteSpace(request.Role)
            ? AdminRole.Admin
            : request.Role.Trim().ToUpperInvariant();

        if (!AdminRole.IsValid(role))
        {
            return BadRequest(new { message = "Role is invalid." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await dbContext.Admins.AnyAsync(item => item.Email == normalizedEmail);
        if (emailExists)
        {
            return Conflict(new { message = "Email is already in use." });
        }

        var usernameBase = CredentialGenerator.SanitizeUsernameBase(normalizedEmail);
        var username = await BuildUniqueUsernameAsync(usernameBase);
        var temporaryPassword = CredentialGenerator.GenerateTemporaryPassword();

        var admin = new AdminEntity
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Username = username,
            Role = role,
            IsActive = true,
            MustChangePassword = true,
            CreatedByAdminId = connectedAdmin.Id
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, temporaryPassword);

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            dbContext.Admins.Add(admin);
            await dbContext.SaveChangesAsync();

            await emailService.SendAdminCredentialsAsync(
                admin.FullName,
                admin.Email,
                admin.Username,
                temporaryPassword);

            await transaction.CommitAsync();
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            logger.LogWarning(exception, "Failed to create admin account for Email={Email}", admin.Email);

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Failed to send admin credentials email. Admin account was not created."
            });
        }

        return CreatedAtRoute(GetAdminByIdRouteName, new { id = admin.Id }, admin.ToDto());
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<AdminResponseDto>> UpdateStatusAsync(int id, [FromBody] AdminStatusUpdateRequestDto request)
    {
        var connectedAdmin = await GetConnectedAdminAsync();
        if (connectedAdmin is null)
        {
            return Unauthorized();
        }

        if (!request.IsActive && connectedAdmin.Id == id)
        {
            return BadRequest(new { message = "You cannot deactivate your own account." });
        }

        var admin = await dbContext.Admins.FirstOrDefaultAsync(item => item.Id == id);
        if (admin is null)
        {
            return NotFound();
        }

        admin.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync();

        return Ok(admin.ToDto());
    }

    private async Task<string> BuildUniqueUsernameAsync(string usernameBase)
    {
        var username = usernameBase;
        var suffix = 2;

        while (await dbContext.Admins.AnyAsync(item => item.Username == username))
        {
            username = $"{usernameBase}{suffix}";
            suffix++;
        }

        return username;
    }

    private async Task<AdminEntity?> GetConnectedAdminAsync()
    {
        var adminId = HttpContext.GetAuthenticatedAdminId();
        if (adminId is null)
        {
            return null;
        }

        return await dbContext.Admins.FirstOrDefaultAsync(item => item.Id == adminId && item.IsActive);
    }
}
