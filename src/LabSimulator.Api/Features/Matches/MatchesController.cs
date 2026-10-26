using LabSimulator.Api.Auth;
using LabSimulator.Api.Common;
using LabSimulator.Api.Data;
using LabSimulator.Api.Domain;
using LabSimulator.Api.Features.Attempts;
using LabSimulator.Api.Features.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LabSimulator.Api.Features.Matches;

[ApiController]
[Route("api/v1")]
[Authorize(Policy = Policies.Registered)]
[Produces("application/json")]
public sealed class MatchesController(LabSimulatorDbContext db, TimeProvider clock) : ControllerBase
{
    /// <summary>Starts a class session hosted by the calling teacher.</summary>
    [HttpPost("matches")]
    [Authorize(Policy = Policies.TeacherOrAdmin)]
    [ProducesResponseType<MatchResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MatchResponse>> Create(CreateMatchRequest request, CancellationToken cancellationToken)
    {
        var host = await db.Profiles.FirstAsync(p => p.Id == User.GetUserId(), cancellationToken);
        var match = new Match
        {
            RoomName = request.RoomName!.Trim(),
            HostId = host.Id,
            Host = host,
            StartedAt = clock.GetUtcNow(),
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, MatchResponse.From(match));
    }

    /// <summary>Returns a match to its host, its participants, or an admin.</summary>
    [HttpGet("matches/{id:guid}")]
    [ProducesResponseType<MatchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchResponse>> GetMatch(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var match = await LoadMatchAsync(id, tracking: false, cancellationToken);

        if (match is null
            || !(User.IsAdmin() || match.HostId == userId || match.Participants.Any(p => p.ProfileId == userId)))
        {
            return MatchNotFound();
        }

        return MatchResponse.From(match);
    }

    /// <summary>Adds the caller to a running match. Joining twice is a no-op.</summary>
    [HttpPost("matches/{id:guid}/participants")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var match = await db.Matches.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new { IsEnded = m.EndedAt != null, IsParticipant = m.Participants.Any(p => p.ProfileId == userId) })
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return MatchNotFound();
        }

        if (match.IsEnded)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "The match has already ended.");
        }

        if (!match.IsParticipant)
        {
            db.MatchParticipants.Add(new MatchParticipant { MatchId = id, ProfileId = userId, JoinedAt = clock.GetUtcNow() });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // A concurrent request already joined this user.
            }
        }

        return NoContent();
    }

    /// <summary>Ends a match. Only its host or an admin can end it.</summary>
    [HttpPost("matches/{id:guid}/end")]
    [ProducesResponseType<MatchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MatchResponse>> End(Guid id, CancellationToken cancellationToken)
    {
        var match = await LoadMatchAsync(id, tracking: true, cancellationToken);
        if (match is null)
        {
            return MatchNotFound();
        }

        if (match.HostId != User.GetUserId() && !User.IsAdmin())
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Only the host can end this match.");
        }

        if (match.IsEnded)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "The match has already ended.");
        }

        match.End(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return MatchResponse.From(match);
    }

    /// <summary>Lists the attempts submitted in a match. Only its host or an admin can see them.</summary>
    [HttpGet("matches/{id:guid}/attempts")]
    [ProducesResponseType<PagedResponse<AttemptResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<AttemptResponse>>> ListAttempts(
        Guid id, [FromQuery] PageQuery page, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var canSee = await db.Matches.AnyAsync(m => m.Id == id && (isAdmin || m.HostId == userId), cancellationToken);
        if (!canSee)
        {
            return MatchNotFound();
        }

        return await db.ScenarioAttempts.AsNoTracking()
            .Include(a => a.Profile)
            .Include(a => a.StepResults)
            .Where(a => a.MatchId == id)
            .OrderBy(a => a.Profile.DisplayName).ThenBy(a => a.CompletedAt)
            .AsSplitQuery()
            .ToPagedResponseAsync(page, AttemptResponse.From, cancellationToken);
    }

    /// <summary>Lists the matches the caller hosted or joined, newest first.</summary>
    [HttpGet("me/matches")]
    [ProducesResponseType<PagedResponse<MatchSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<PagedResponse<MatchSummaryResponse>> ListMyMatches(
        [FromQuery] PageQuery page, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var query = db.Matches.AsNoTracking()
            .Where(m => m.HostId == userId || m.Participants.Any(p => p.ProfileId == userId))
            .OrderByDescending(m => m.StartedAt).ThenByDescending(m => m.Id);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page.Page - 1) * page.PageSize)
            .Take(page.PageSize)
            .Select(m => new MatchSummaryResponse(
                m.Id,
                m.RoomName,
                new ProfileSummary(m.Host.Id, m.Host.DisplayName),
                m.StartedAt,
                m.EndedAt,
                m.Participants.Count))
            .ToListAsync(cancellationToken);

        return new PagedResponse<MatchSummaryResponse>(items, page.Page, page.PageSize, total);
    }

    private Task<Match?> LoadMatchAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = db.Matches
            .Include(m => m.Host)
            .Include(m => m.Participants).ThenInclude(p => p.Profile)
            .Where(m => m.Id == id);

        return (tracking ? query : query.AsNoTracking()).FirstOrDefaultAsync(cancellationToken);
    }

    private ObjectResult MatchNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Match not found.");
}
