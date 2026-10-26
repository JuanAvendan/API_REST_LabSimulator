using LabSimulator.Api.Auth;
using LabSimulator.Api.Common;
using LabSimulator.Api.Data;
using LabSimulator.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSimulator.Api.Features.Attempts;

[ApiController]
[Route("api/v1")]
[Authorize(Policy = Policies.Registered)]
[Produces("application/json")]
public sealed class AttemptsController(LabSimulatorDbContext db, TimeProvider clock) : ControllerBase
{
    /// <summary>Submits a finished scenario run. The score is computed by the server from the rubric.</summary>
    [HttpPost("attempts")]
    [ProducesResponseType<AttemptResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttemptResponse>> Submit(SubmitAttemptRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var scenario = await db.Scenarios.AsNoTracking()
            .Include(s => s.Steps)
            .FirstOrDefaultAsync(s => s.Code == request.ScenarioCode && s.IsActive, cancellationToken);
        if (scenario is null)
        {
            ModelState.AddModelError(nameof(request.ScenarioCode), $"Scenario '{request.ScenarioCode}' does not exist.");
            return ValidationProblem();
        }

        if (request.MatchId is { } matchId)
        {
            var match = await db.Matches.AsNoTracking()
                .Where(m => m.Id == matchId)
                .Select(m => new { IsEnded = m.EndedAt != null, IsParticipant = m.Participants.Any(p => p.ProfileId == userId) })
                .FirstOrDefaultAsync(cancellationToken);

            if (match is null)
            {
                ModelState.AddModelError(nameof(request.MatchId), "Match not found.");
                return ValidationProblem();
            }

            if (!match.IsParticipant)
            {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Not a participant of this match.",
                    detail: $"Join it first with POST /api/v1/matches/{matchId}/participants.");
            }

            if (match.IsEnded)
            {
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "The match has already ended.");
            }
        }

        var scoring = scenario.Score(request.Steps!.Select(s => (s.StepCode!, s.EarnedPoints!.Value)).ToList());
        if (!scoring.IsValid)
        {
            foreach (var (key, messages) in scoring.Errors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(key, message);
                }
            }

            return ValidationProblem();
        }

        var attempt = new ScenarioAttempt
        {
            ProfileId = userId,
            ScenarioCode = scenario.Code,
            MatchId = request.MatchId,
            SelectedMedication = request.SelectedMedication!.Trim(),
            SelectedPathology = request.SelectedPathology!.Trim(),
            RemainingSeconds = request.RemainingSeconds!.Value,
            Score = scoring.Steps.Sum(s => s.EarnedPoints),
            MaxScore = scoring.Steps.Sum(s => s.MaxPoints),
            CompletedAt = clock.GetUtcNow(),
            StepResults = scoring.Steps,
        };
        db.ScenarioAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(attempt).Reference(a => a.Profile).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAttempt), new { id = attempt.Id }, AttemptResponse.From(attempt));
    }

    /// <summary>Returns an attempt to its student, to the host of its match, or to an admin.</summary>
    [HttpGet("attempts/{id:guid}")]
    [ProducesResponseType<AttemptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptResponse>> GetAttempt(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        var attempt = await db.ScenarioAttempts.AsNoTracking()
            .Include(a => a.Profile)
            .Include(a => a.StepResults)
            .Where(a => a.Id == id)
            .Where(a => isAdmin
                || a.ProfileId == userId
                || db.Matches.Any(m => m.Id == a.MatchId && m.HostId == userId))
            .FirstOrDefaultAsync(cancellationToken);

        return attempt is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Attempt not found.")
            : AttemptResponse.From(attempt);
    }

    /// <summary>Lists the caller's attempts, newest first.</summary>
    [HttpGet("me/attempts")]
    [ProducesResponseType<PagedResponse<AttemptResponse>>(StatusCodes.Status200OK)]
    public async Task<PagedResponse<AttemptResponse>> ListMyAttempts(
        [FromQuery] PageQuery page, [FromQuery] string? scenarioCode, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var query = db.ScenarioAttempts.AsNoTracking()
            .Include(a => a.Profile)
            .Include(a => a.StepResults)
            .Where(a => a.ProfileId == userId);

        if (!string.IsNullOrWhiteSpace(scenarioCode))
        {
            query = query.Where(a => a.ScenarioCode == scenarioCode);
        }

        return await query
            .OrderByDescending(a => a.CompletedAt).ThenByDescending(a => a.Id)
            .AsSplitQuery()
            .ToPagedResponseAsync(page, AttemptResponse.From, cancellationToken);
    }
}
