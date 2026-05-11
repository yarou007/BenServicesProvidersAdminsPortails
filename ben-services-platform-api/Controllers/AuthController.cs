using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Extensions;
using BenServicesPlatform.Api.Mapping;
using BenServicesPlatform.Api.Services;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    AppDbContext dbContext,
    IPasswordHasher<AdminEntity> passwordHasher,
    IJwtTokenService jwtTokenService,
    IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> LoginAsync([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Login and password are required." });
        }

        var normalizedLogin = request.Login.Trim().ToLowerInvariant();

        var admin = await dbContext.Admins.FirstOrDefaultAsync(item =>
            item.Email.ToLower() == normalizedLogin || item.Username.ToLower() == normalizedLogin);

        if (admin is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        if (!admin.IsActive)
        {
            return Unauthorized(new { message = "This admin account is inactive." });
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            admin.PasswordHash = passwordHasher.HashPassword(admin, request.Password);
            await dbContext.SaveChangesAsync();
        }

        var token = jwtTokenService.GenerateToken(admin);
        return Ok(new LoginResponseDto
        {
            Token = token,
            Admin = admin.ToDto()
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AdminResponseDto>> MeAsync()
    {
        var adminId = HttpContext.GetAuthenticatedAdminId();
        if (adminId is null)
        {
            return Unauthorized();
        }

        var admin = await dbContext.Admins
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == adminId && item.IsActive);

        if (admin is null)
        {
            return Unauthorized();
        }

        return Ok(admin.ToDto());
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ChangePasswordResponseDto>> ChangePasswordAsync([FromBody] ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword)
            || string.IsNullOrWhiteSpace(request.NewPassword)
            || string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return BadRequest(new { message = "Current password, new password, and confirm password are required." });
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { message = "New password and confirm password do not match." });
        }

        if (!PasswordPolicyValidator.IsStrong(request.NewPassword, out var passwordError))
        {
            return BadRequest(new { message = passwordError });
        }

        var adminId = HttpContext.GetAuthenticatedAdminId();
        if (adminId is null)
        {
            return Unauthorized();
        }

        var admin = await dbContext.Admins.FirstOrDefaultAsync(item => item.Id == adminId && item.IsActive);
        if (admin is null)
        {
            return Unauthorized();
        }

        var currentPasswordResult = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.CurrentPassword);
        if (currentPasswordResult == PasswordVerificationResult.Failed)
        {
            return BadRequest(new { message = "Current password is incorrect." });
        }

        admin.PasswordHash = passwordHasher.HashPassword(admin, request.NewPassword);
        admin.MustChangePassword = false;
        await dbContext.SaveChangesAsync();

        return Ok(new ChangePasswordResponseDto
        {
            Message = "Password changed successfully",
            Admin = admin.ToDto()
        });
    }

    [HttpPost("register-admin")]
    [AllowAnonymous]
    public async Task<ActionResult<AdminResponseDto>> RegisterAdminAsync([FromBody] RegisterAdminRequestDto request)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        if (await dbContext.Admins.AnyAsync())
        {
            return Conflict(new { message = "Initial admin already exists." });
        }

        if (string.IsNullOrWhiteSpace(request.FullName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Username)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Full name, email, username, and password are required." });
        }

        if (!MailAddress.TryCreate(request.Email.Trim(), out _))
        {
            return BadRequest(new { message = "Email format is invalid." });
        }

        if (!PasswordPolicyValidator.IsStrong(request.Password, out var passwordError))
        {
            return BadRequest(new { message = passwordError });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var emailInUse = await dbContext.Admins.AnyAsync(item => item.Email == normalizedEmail);
        if (emailInUse)
        {
            return Conflict(new { message = "Email is already in use." });
        }

        var usernameInUse = await dbContext.Admins.AnyAsync(item => item.Username == normalizedUsername);
        if (usernameInUse)
        {
            return Conflict(new { message = "Username is already in use." });
        }

        var admin = new AdminEntity
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Username = normalizedUsername,
            Role = AdminRole.Admin,
            IsActive = true,
            MustChangePassword = false
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, request.Password);

        dbContext.Admins.Add(admin);
        await dbContext.SaveChangesAsync();

        return CreatedAtRoute("GetAdminById", new { id = admin.Id }, admin.ToDto());
    }
}
