using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderApplicationDto>>> GetAllAsync()
    {
        var applications = await dbContext.ProviderApplications
            .AsNoTracking()
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync();

        return Ok(applications.Select(item => item.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProviderApplicationDto>> GetByIdAsync(int id)
    {
        var application = await dbContext.ProviderApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        return Ok(application.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<ProviderApplicationDto>> CreateAsync([FromBody] ProviderApplicationCreateRequest request)
    {
        var entity = request.ToEntity();
        dbContext.ProviderApplications.Add(entity);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = entity.Id }, entity.ToDto());
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<ProviderApplicationDto>> ApproveAsync(int id)
    {
        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        if (application.Status == "Approved")
        {
            return Ok(application.ToDto());
        }

        application.Status = "Approved";

        var verificationStatus = application.EmergencyService ? "Active" : "Verified";
        var now = DateTime.UtcNow;

        var provider = new ProviderEntity
        {
            FullName = application.FullName,
            BusinessName = application.BusinessName,
            Phone = application.Phone,
            Email = application.Email,
            ServiceType = application.ServiceType,
            ServicesOfferedJson = application.ServicesOfferedJson,
            City = application.City,
            State = application.State,
            ZipCodesJson = application.ZipCodesJson,
            Region = MapRegionByState(application.State),
            EmergencyService = application.EmergencyService,
            Availability = application.EmergencyService ? "Daily" : "Weekdays",
            WorkingHours = application.WorkingHours,
            VerificationStatus = verificationStatus,
            IsActive = true,
            Source = application.Source,
            YearsOfExperience = application.YearsOfExperience,
            Notes = application.Message,
            AdminComments = "Approved from public application queue.",
            CreatedAt = now,
            UpdatedAt = now,
            VerifiedAt = now
        };

        dbContext.Providers.Add(provider);
        await dbContext.SaveChangesAsync();

        return Ok(application.ToDto());
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<ProviderApplicationDto>> RejectAsync(int id)
    {
        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        application.Status = "Rejected";
        await dbContext.SaveChangesAsync();

        return Ok(application.ToDto());
    }

    [HttpPost("{id:int}/request-more-info")]
    public async Task<ActionResult<ProviderApplicationDto>> RequestMoreInfoAsync(int id)
    {
        var application = await dbContext.ProviderApplications.FirstOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        application.Status = "More Info Requested";
        await dbContext.SaveChangesAsync();

        return Ok(application.ToDto());
    }

    private static string MapRegionByState(string state)
    {
        return state.Trim().ToUpperInvariant() switch
        {
            "TX" => "South",
            "AZ" => "Southwest",
            "FL" => "Southeast",
            _ => "Atlantic"
        };
    }
}
