using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Mapping;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext)
    {
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
}
