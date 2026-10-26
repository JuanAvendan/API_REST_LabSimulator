namespace LabSimulator.Api.Domain;

/// <summary>A clinical scenario and its scoring rubric (one <see cref="ScenarioStep"/> per graded step).</summary>
public sealed class Scenario
{
    public const int CodeMaxLength = 64;

    public required string Code { get; init; }

    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public List<ScenarioStep> Steps { get; } = [];

    /// <summary>
    /// Validates the points reported by the client against the rubric. Every step must be
    /// reported exactly once and within its range; the total is always computed here.
    /// </summary>
    public ScoringResult Score(IReadOnlyCollection<(string StepCode, int EarnedPoints)> reported)
    {
        var errors = new Dictionary<string, string[]>();
        var stepsByCode = Steps.ToDictionary(s => s.Code, StringComparer.Ordinal);

        var duplicates = reported.GroupBy(r => r.StepCode, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();
        if (duplicates.Length > 0)
        {
            errors["steps"] = [$"Steps reported more than once: {string.Join(", ", duplicates)}."];
        }

        var unknown = reported.Select(r => r.StepCode).Where(c => !stepsByCode.ContainsKey(c)).Distinct().ToArray();
        if (unknown.Length > 0)
        {
            errors["steps.unknown"] = [$"Unknown steps for scenario '{Code}': {string.Join(", ", unknown)}."];
        }

        var reportedCodes = reported.Select(r => r.StepCode).ToHashSet(StringComparer.Ordinal);
        var missing = Steps.Where(s => !reportedCodes.Contains(s.Code)).Select(s => s.Code).ToArray();
        if (missing.Length > 0)
        {
            errors["steps.missing"] = [$"Missing steps: {string.Join(", ", missing)}."];
        }

        foreach (var (stepCode, earnedPoints) in reported)
        {
            if (stepsByCode.TryGetValue(stepCode, out var step) && (earnedPoints < 0 || earnedPoints > step.MaxPoints))
            {
                errors[$"steps.{stepCode}"] = [$"Earned points must be between 0 and {step.MaxPoints}."];
            }
        }

        if (errors.Count > 0)
        {
            return ScoringResult.Invalid(errors);
        }

        var results = reported
            .Select(r => new AttemptStepResult
            {
                StepCode = r.StepCode,
                EarnedPoints = r.EarnedPoints,
                MaxPoints = stepsByCode[r.StepCode].MaxPoints,
                DisplayOrder = stepsByCode[r.StepCode].DisplayOrder,
            })
            .OrderBy(r => r.DisplayOrder)
            .ToList();

        return ScoringResult.Valid(results);
    }
}

public sealed record ScoringResult(IReadOnlyDictionary<string, string[]> Errors, List<AttemptStepResult> Steps)
{
    public bool IsValid => Errors.Count == 0;

    public static ScoringResult Valid(List<AttemptStepResult> steps) => new(new Dictionary<string, string[]>(), steps);

    public static ScoringResult Invalid(Dictionary<string, string[]> errors) => new(errors, []);
}
