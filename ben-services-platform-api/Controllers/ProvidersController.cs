using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderDto>>> GetAllAsync()
    {
        var providers = await dbContext.Providers
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        return Ok(providers.Select(item => item.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProviderDto>> GetByIdAsync(int id)
    {
        var provider = await dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (provider is null)
        {
            return NotFound();
        }

        return Ok(provider.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<ProviderDto>> CreateAsync([FromBody] ProviderUpsertRequest request)
    {
        var now = DateTime.UtcNow;

        var entity = new ProviderEntity
        {
            CreatedAt = now,
            UpdatedAt = now
        };

        entity.ApplyUpdate(request);

        if ((entity.VerificationStatus is "Verified" or "Active") && entity.VerifiedAt is null)
        {
            entity.VerifiedAt = now;
        }

        dbContext.Providers.Add(entity);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = entity.Id }, entity.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProviderDto>> UpdateAsync(int id, [FromBody] ProviderUpsertRequest request)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);

        if (entity is null)
        {
            return NotFound();
        }

        entity.ApplyUpdate(request);

        if ((entity.VerificationStatus is "Verified" or "Active") && entity.VerifiedAt is null)
        {
            entity.VerifiedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        return Ok(entity.ToDto());
    }

    [HttpPost("{id:int}/verify")]
    public async Task<ActionResult<ProviderDto>> VerifyAsync(int id)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);

        if (entity is null)
        {
            return NotFound();
        }

        entity.VerificationStatus = "Verified";
        entity.IsActive = true;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(entity.ToDto());
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<ProviderDto>> DeactivateAsync(int id)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);

        if (entity is null)
        {
            return NotFound();
        }

        entity.VerificationStatus = "Inactive";
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(entity.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var entity = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == id);

        if (entity is null)
        {
            return NotFound();
        }

        dbContext.Providers.Remove(entity);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
