using LabSimulator.Api.Auth;
using LabSimulator.Api.Common;
using LabSimulator.Api.Data;
using LabSimulator.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSimulator.Api.Features.Profiles;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public sealed class ProfilesController(LabSimulatorDbContext db, TimeProvider clock) : ControllerBase
{
    /// <summary>Returns the caller's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var profile = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        return profile is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Profile not found.",
                detail: "Create it with PUT /api/v1/me.")
            : ProfileResponse.From(profile);
    }

    /// <summary>Creates the caller's profile on first sign-in, or updates its display name.</summary>
    /// <remarks>New profiles get the Student role; only an admin can change roles.</remarks>
    [HttpPut("me")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileResponse>> UpsertMe(UpsertProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var displayName = request.DisplayName!.Trim();
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        if (profile is not null)
        {
            profile.DisplayName = displayName;
            await db.SaveChangesAsync(cancellationToken);
            return ProfileResponse.From(profile);
        }

        profile = new Profile { Id = userId, DisplayName = displayName, CreatedAt = clock.GetUtcNow() };
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMe), ProfileResponse.From(profile));
    }

    /// <summary>Lists profiles, optionally filtered by role.</summary>
    [HttpGet("profiles")]
    [Authorize(Policy = Policies.TeacherOrAdmin)]
    [ProducesResponseType<PagedResponse<ProfileResponse>>(StatusCodes.Status200OK)]
    public async Task<PagedResponse<ProfileResponse>> ListProfiles(
        [FromQuery] PageQuery page, [FromQuery] Role? role, CancellationToken cancellationToken)
    {
        var query = db.Profiles.AsNoTracking();
        if (role is not null)
        {
            query = query.Where(p => p.Role == role);
        }

        return await query
            .OrderBy(p => p.DisplayName).ThenBy(p => p.Id)
            .ToPagedResponseAsync(page, ProfileResponse.From, cancellationToken);
    }

    /// <summary>Changes a user's role.</summary>
    [HttpPut("profiles/{id:guid}/role")]
    [Authorize(Policy = Policies.Admin)]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> ChangeRole(
        Guid id, ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (profile is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Profile not found.");
        }

        if (id == User.GetUserId() && request.Role != Role.Admin)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Admins cannot remove their own admin role.");
        }

        profile.Role = request.Role!.Value;
        await db.SaveChangesAsync(cancellationToken);

        return ProfileResponse.From(profile);
    }
}
