using System.Security.Claims;
using LabSimulator.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace LabSimulator.Api.Auth;

/// <summary>
/// Adds the application role from the user's profile. The database is the source of truth, so a
/// role change applies on the next request without waiting for the token to expire.
/// </summary>
internal sealed class ProfileRoleClaimsTransformation(LabSimulatorDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(c => c.Type == AppClaimTypes.Role)
            || !Guid.TryParse(principal.FindFirstValue(AppClaimTypes.Subject), out var userId))
        {
            return principal;
        }

        var role = await db.Profiles
            .Where(p => p.Id == userId)
            .Select(p => (Domain.Role?)p.Role)
            .FirstOrDefaultAsync();

        if (role is not null)
        {
            principal.AddIdentity(new ClaimsIdentity(
                [new Claim(AppClaimTypes.Role, role.Value.ToString())],
                authenticationType: null,
                nameType: AppClaimTypes.Subject,
                roleType: AppClaimTypes.Role));
        }

        return principal;
    }
}
