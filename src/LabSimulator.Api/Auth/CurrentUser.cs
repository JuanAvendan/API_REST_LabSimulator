using System.Security.Claims;
using LabSimulator.Api.Domain;

namespace LabSimulator.Api.Auth;

internal static class CurrentUserExtensions
{
    /// <summary>Supabase user id of the caller. Only valid behind an authorization requirement.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(AppClaimTypes.Subject)
            ?? throw new InvalidOperationException("The principal has no subject claim."));

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(nameof(Role.Admin));
}
