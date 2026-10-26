namespace LabSimulator.Api.Domain;

public sealed class AttemptStepResult
{
    public Guid AttemptId { get; init; }

    public required string StepCode { get; init; }

    public int EarnedPoints { get; init; }

    /// <summary>Snapshot of the rubric at submission time, so later rubric changes do not rewrite history.</summary>
    public int MaxPoints { get; init; }

    public int DisplayOrder { get; init; }
}
