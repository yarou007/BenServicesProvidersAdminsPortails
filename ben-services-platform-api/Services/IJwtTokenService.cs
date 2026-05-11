using BenServicesPlatform.Api.Entities;

namespace BenServicesPlatform.Api.Services;

public interface IJwtTokenService
{
    string GenerateToken(AdminEntity admin);
}
