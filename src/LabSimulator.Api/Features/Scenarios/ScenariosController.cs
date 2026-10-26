using LabSimulator.Api.Auth;
using LabSimulator.Api.Data;
using LabSimulator.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSimulator.Api.Features.Scenarios;

public sealed record ScenarioStepResponse(string Code, string Stage, string Name, int MaxPoints, int DisplayOrder);

public sealed record ScenarioResponse(string Code, string Name, int MaxScore, IReadOnlyList<ScenarioStepResponse> Steps)
{
    public static ScenarioResponse From(Scenario scenario)
    {
        var steps = scenario.Steps
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new ScenarioStepResponse(s.Code, s.Stage, s.Name, s.MaxPoints, s.DisplayOrder))
            .ToList();

        return new ScenarioResponse(scenario.Code, scenario.Name, steps.Sum(s => s.MaxPoints), steps);
    }
}

[ApiController]
[Route("api/v1/scenarios")]
[Authorize(Policy = Policies.Registered)]
[Produces("application/json")]
public sealed class ScenariosController(LabSimulatorDbContext db) : ControllerBase
{
    /// <summary>Lists active scenarios with their scoring rubric.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<ScenarioResponse>> List(CancellationToken cancellationToken)
    {
        var scenarios = await db.Scenarios.AsNoTracking()
            .Include(s => s.Steps)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken);

        return scenarios.Select(ScenarioResponse.From).ToList();
    }

    /// <summary>Returns one scenario with its scoring rubric.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType<ScenarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScenarioResponse>> Get(string code, CancellationToken cancellationToken)
    {
        var scenario = await db.Scenarios.AsNoTracking()
            .Include(s => s.Steps)
            .FirstOrDefaultAsync(s => s.Code == code && s.IsActive, cancellationToken);

        return scenario is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Scenario not found.")
            : ScenarioResponse.From(scenario);
    }
}
