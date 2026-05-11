using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Mapping;
using BenServicesPlatform.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IPasswordHasher<AdminEntity> passwordHasher)
    {
        if (!await dbContext.Admins.AnyAsync())
        {
            var seedEmail = configuration["DEFAULT_ADMIN_EMAIL"]?.Trim();
            var seedPassword = configuration["DEFAULT_ADMIN_PASSWORD"];
            var seedFullName = configuration["DEFAULT_ADMIN_FULLNAME"]?.Trim();

            if (!string.IsNullOrWhiteSpace(seedEmail)
                && !string.IsNullOrWhiteSpace(seedPassword)
                && !string.IsNullOrWhiteSpace(seedFullName))
            {
                if (!PasswordPolicyValidator.IsStrong(seedPassword, out var passwordPolicyError))
                {
                    if (!hostEnvironment.IsDevelopment())
                    {
                        throw new InvalidOperationException(
                            $"DEFAULT_ADMIN_PASSWORD does not satisfy password policy: {passwordPolicyError}");
                    }
                }
                else
                {
                    var normalizedEmail = seedEmail.ToLowerInvariant();
                    var usernameBase = CredentialGenerator.SanitizeUsernameBase(normalizedEmail);
                    var username = await BuildUniqueUsernameAsync(dbContext, usernameBase);
                    var now = DateTime.UtcNow;

                    var admin = new AdminEntity
                    {
                        FullName = seedFullName,
                        Email = normalizedEmail,
                        Username = username,
                        Role = AdminRole.Admin,
                        IsActive = true,
                        MustChangePassword = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    admin.PasswordHash = passwordHasher.HashPassword(admin, seedPassword);
                    dbContext.Admins.Add(admin);
                    await dbContext.SaveChangesAsync();
                }
            }
            else if (!hostEnvironment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "No admin account exists. Set DEFAULT_ADMIN_EMAIL, DEFAULT_ADMIN_PASSWORD, and DEFAULT_ADMIN_FULLNAME to bootstrap the first admin.");
            }
        }

        if (!await dbContext.Providers.AnyAsync())
        {
            var now = DateTime.UtcNow;

            dbContext.Providers.AddRange(
                new ProviderEntity
                {
                    FullName = "Michael Torres",
                    BusinessName = "Arlington Lock Pros",
                    Phone = "(817) 555-1022",
                    Email = "michael@arlingtonlockpros.com",
                    ServiceType = "Locksmith",
                    ServicesOfferedJson = JsonArrayMapper.Serialize(["Residential Lockout", "Commercial Locks", "Rekey Service"]),
                    City = "Arlington",
                    State = "TX",
                    ZipCodesJson = JsonArrayMapper.Serialize(["76010", "76011", "76012"]),
                    Region = "South",
                    EmergencyService = true,
                    Availability = "Weekdays + weekends",
                    WorkingHours = "24/7",
                    VerificationStatus = "Active",
                    IsActive = true,
                    Source = "Google",
                    YearsOfExperience = 12,
                    Notes = "Strong reviews, reliable response time.",
                    AdminComments = "Great candidate for hero placement.",
                    CreatedAt = now.AddMonths(-3),
                    UpdatedAt = now.AddDays(-3),
                    VerifiedAt = now.AddMonths(-2)
                },
                new ProviderEntity
                {
                    FullName = "Emily Sanders",
                    BusinessName = "Metro Glass Fast Fix",
                    Phone = "(817) 555-1044",
                    Email = "emily@metroglassfix.com",
                    ServiceType = "Glass",
                    ServicesOfferedJson = JsonArrayMapper.Serialize(["Window Repair", "Storefront Glass Repair", "Board-Up Service"]),
                    City = "Arlington",
                    State = "TX",
                    ZipCodesJson = JsonArrayMapper.Serialize(["76013", "76014"]),
                    Region = "South",
                    EmergencyService = true,
                    Availability = "Daily",
                    WorkingHours = "6:00 AM - 11:00 PM",
                    VerificationStatus = "Verified",
                    IsActive = true,
                    Source = "Referral",
                    YearsOfExperience = 9,
                    CreatedAt = now.AddMonths(-3),
                    UpdatedAt = now.AddDays(-8),
                    VerifiedAt = now.AddMonths(-2)
                },
                new ProviderEntity
                {
                    FullName = "Isabella Young",
                    BusinessName = "Phoenix Glass & Locks",
                    Phone = "(602) 555-1322",
                    Email = "isabella@phxglasslocks.com",
                    ServiceType = "Both",
                    ServicesOfferedJson = JsonArrayMapper.Serialize(["Commercial Locks", "Storefront Glass Repair", "Board-Up Service"]),
                    City = "Phoenix",
                    State = "AZ",
                    ZipCodesJson = JsonArrayMapper.Serialize(["85004", "85006"]),
                    Region = "Southwest",
                    EmergencyService = true,
                    Availability = "All week",
                    WorkingHours = "24/7",
                    VerificationStatus = "Active",
                    IsActive = true,
                    Source = "Google",
                    YearsOfExperience = 13,
                    CreatedAt = now.AddMonths(-2),
                    UpdatedAt = now.AddDays(-4),
                    VerifiedAt = now.AddMonths(-1)
                },
                new ProviderEntity
                {
                    FullName = "Carmen Diaz",
                    BusinessName = "Tampa 24H Locksmith",
                    Phone = "(813) 555-1515",
                    Email = "carmen@tampa24hlocksmith.com",
                    ServiceType = "Locksmith",
                    ServicesOfferedJson = JsonArrayMapper.Serialize(["Residential Lockout", "Car Keys", "Rekey Service"]),
                    City = "Tampa",
                    State = "FL",
                    ZipCodesJson = JsonArrayMapper.Serialize(["33602", "33606"]),
                    Region = "Southeast",
                    EmergencyService = true,
                    Availability = "Daily",
                    WorkingHours = "24/7",
                    VerificationStatus = "Active",
                    IsActive = true,
                    Source = "Form",
                    YearsOfExperience = 15,
                    CreatedAt = now.AddMonths(-2),
                    UpdatedAt = now.AddDays(-2),
                    VerifiedAt = now.AddMonths(-1)
                }
            );
        }

        if (!await dbContext.ProviderApplications.AnyAsync())
        {
            var now = DateTime.UtcNow;

            dbContext.ProviderApplications.AddRange(
                new ProviderApplicationEntity
                {
                    FullName = "Noah Price",
                    BusinessName = "Phoenix Smart Locks",
                    Phone = "(602) 555-2001",
                    Email = "noah@phxsmartlocks.com",
                    ServiceType = "Locksmith",
                    ServicesOfferedJson = JsonArrayMapper.Serialize(["Smart Lock Installation", "Car Keys"]),
                    CitiesCoveredJson = JsonArrayMapper.Serialize(["Phoenix", "Scottsdale"]),
                    City = "Phoenix",
                    State = "AZ",
                    ZipCodesJson = JsonArrayMapper.Serialize(["85008", "85009"]),
                    YearsOfExperience = 7,
                    EmergencyService = true,
                    WorkingHours = "24/7",
                    Message = "We can handle overflow and after-hours calls.",
                    Source = "Form",
                    Status = "Pending",
                    SubmittedAt = now.AddDays(-5),
                    LicenseFileName = "noah-price-license.pdf"
                },
                new ProviderApplicationEntity
                {
                    FullName = "Grace Kim",
                    BusinessName = "Arlington Precision Glass",
                    Phone = "(817) 555-2030",
                    Email = "grace@precisionglassarlington.com",
                    ServiceType = "Glass",
                    ServicesOfferedJson = JsonArrayMapper.Serialize(["Storefront Glass Repair", "Board-Up Service", "Window Repair"]),
                    CitiesCoveredJson = JsonArrayMapper.Serialize(["Arlington"]),
                    City = "Arlington",
                    State = "TX",
                    ZipCodesJson = JsonArrayMapper.Serialize(["76017", "76018"]),
                    YearsOfExperience = 11,
                    EmergencyService = true,
                    WorkingHours = "6:00 AM - 10:00 PM",
                    Message = "Interested in premium placement in Arlington pages.",
                    Source = "Form",
                    Status = "Pending",
                    SubmittedAt = now.AddDays(-4)
                }
            );
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<string> BuildUniqueUsernameAsync(AppDbContext dbContext, string usernameBase)
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
}
