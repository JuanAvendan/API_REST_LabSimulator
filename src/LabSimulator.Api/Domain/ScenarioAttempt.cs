namespace LabSimulator.Api.Domain;

/// <summary>One completed run of a scenario by a student, with its server-computed score.</summary>
public sealed class ScenarioAttempt
{
    public const int SelectionMaxLength = 100;

    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ProfileId { get; init; }

    public Profile Profile { get; init; } = null!;

    public required string ScenarioCode { get; init; }

    /// <summary>Class session the attempt belongs to; <c>null</c> for free practice.</summary>
    public Guid? MatchId { get; init; }

    public required string SelectedMedication { get; init; }

    public required string SelectedPathology { get; init; }

    public int RemainingSeconds { get; init; }

    public int Score { get; init; }

    public int MaxScore { get; init; }

    public DateTimeOffset CompletedAt { get; init; }

    public List<AttemptStepResult> StepResults { get; init; } = [];

    public decimal ScorePercent => MaxScore == 0 ? 0 : Math.Round(100m * Score / MaxScore, 2);
}
