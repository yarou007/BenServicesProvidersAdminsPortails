using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Dtos;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenServicesPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController(AppDbContext dbContext, ILogger<ProvidersController> logger) : ControllerBase
{
    private const string GetProviderByIdRouteName = "GetProviderById";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderDto>>> GetAllAsync()
    {
        var providers = await dbContext.Providers
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        return Ok(providers.Select(item => item.ToDto()));
    }

    [HttpGet("{id:int}", Name = GetProviderByIdRouteName)]
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
        ProviderEntity? createdEntity = null;

        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var normalizedPhone = request.Phone.Trim();
            var normalizedFullName = request.FullName.Trim().ToLowerInvariant();

            // Guard against duplicate inserts caused by client retries on transient failures.
            var existingProvider = await dbContext.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Email.ToLower() == normalizedEmail &&
                    item.Phone == normalizedPhone &&
                    item.FullName.ToLower() == normalizedFullName);

            if (existingProvider is not null)
            {
                logger.LogInformation(
                    "Duplicate provider creation prevented for Email={Email}, Phone={Phone}. ExistingId={ProviderId}",
                    normalizedEmail,
                    normalizedPhone,
                    existingProvider.Id);

                return Conflict(new
                {
                    message = "A provider with the same identity already exists.",
                    provider = existingProvider.ToDto()
                });
            }

            var now = DateTime.UtcNow;

            createdEntity = new ProviderEntity
            {
                CreatedAt = now,
                UpdatedAt = now
            };

            createdEntity.ApplyUpdate(request);

            if ((createdEntity.VerificationStatus is "Verified" or "Active") && createdEntity.VerifiedAt is null)
            {
                createdEntity.VerifiedAt = now;
            }

            dbContext.Providers.Add(createdEntity);
            await dbContext.SaveChangesAsync();

            var createdDto = createdEntity.ToDto();

            return CreatedAtRoute(GetProviderByIdRouteName, new { id = createdEntity.Id }, createdDto);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Provider creation failed for Email={Email}, Phone={Phone}.",
                request.Email,
                request.Phone);

            // If the record was committed before response generation failed, return a success payload.
            if (createdEntity?.Id > 0)
            {
                var persistedEntity = await dbContext.Providers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == createdEntity.Id);

                if (persistedEntity is not null)
                {
                    return Ok(persistedEntity.ToDto());
                }
            }

            return Problem(
                title: "Provider creation failed",
                detail: "An unexpected error occurred while creating the provider.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
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
