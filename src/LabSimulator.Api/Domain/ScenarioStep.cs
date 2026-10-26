namespace LabSimulator.Api.Domain;

public sealed class ScenarioStep
{
    public const int CodeMaxLength = 64;

    public required string ScenarioCode { get; init; }

    public required string Code { get; init; }

    /// <summary>Stage the step belongs to, e.g. <c>inspection</c> or <c>configuration</c>.</summary>
    public required string Stage { get; set; }

    /// <summary>Human-readable name shown in the simulator UI.</summary>
    public required string Name { get; set; }

    public int MaxPoints { get; set; }

    public int DisplayOrder { get; set; }
}
