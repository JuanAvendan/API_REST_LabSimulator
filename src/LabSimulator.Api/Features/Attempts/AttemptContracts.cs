using System.ComponentModel.DataAnnotations;
using LabSimulator.Api.Domain;
using LabSimulator.Api.Features.Profiles;

namespace LabSimulator.Api.Features.Attempts;

public sealed record SubmitAttemptRequest
{
    [Required]
    [StringLength(Scenario.CodeMaxLength)]
    public string? ScenarioCode { get; init; }

    /// <summary>Class session the attempt belongs to; omit for free practice.</summary>
    public Guid? MatchId { get; init; }

    [Required]
    [StringLength(ScenarioAttempt.SelectionMaxLength)]
    public string? SelectedMedication { get; init; }

    [Required]
    [StringLength(ScenarioAttempt.SelectionMaxLength)]
    public string? SelectedPathology { get; init; }

    [Required]
    [Range(0, 86_400)]
    public int? RemainingSeconds { get; init; }

    /// <summary>Points earned in every step of the scenario rubric (see <c>GET /api/v1/scenarios/{code}</c>).</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public IReadOnlyList<StepScoreRequest>? Steps { get; init; }
}

public sealed record StepScoreRequest
{
    [Required]
    [StringLength(ScenarioStep.CodeMaxLength)]
    public string? StepCode { get; init; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? EarnedPoints { get; init; }
}

public sealed record AttemptStepResponse(string StepCode, int EarnedPoints, int MaxPoints);

public sealed record AttemptResponse(
    Guid Id,
    string ScenarioCode,
    Guid? MatchId,
    ProfileSummary Student,
    string SelectedMedication,
    string SelectedPathology,
    int RemainingSeconds,
    int Score,
    int MaxScore,
    decimal ScorePercent,
    DateTimeOffset CompletedAt,
    IReadOnlyList<AttemptStepResponse> Steps)
{
    /// <remarks>Requires <see cref="ScenarioAttempt.Profile"/> and <see cref="ScenarioAttempt.StepResults"/> to be loaded.</remarks>
    public static AttemptResponse From(ScenarioAttempt attempt) => new(
        attempt.Id,
        attempt.ScenarioCode,
        attempt.MatchId,
        ProfileSummary.From(attempt.Profile),
        attempt.SelectedMedication,
        attempt.SelectedPathology,
        attempt.RemainingSeconds,
        attempt.Score,
        attempt.MaxScore,
        attempt.ScorePercent,
        attempt.CompletedAt,
        attempt.StepResults
            .OrderBy(r => r.DisplayOrder)
            .Select(r => new AttemptStepResponse(r.StepCode, r.EarnedPoints, r.MaxPoints))
            .ToList());
}
