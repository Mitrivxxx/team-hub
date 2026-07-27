using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace team_hub_bff.Services;

public interface ICurrentUserService
{
    Guid? GetUserId();
    Guid GetRequiredUserId();
}

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? GetUserId()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
            return null;

        var raw = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    public Guid GetRequiredUserId() =>
        GetUserId() ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
