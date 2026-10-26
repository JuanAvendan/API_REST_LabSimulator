using LabSimulator.Api.Domain;

namespace LabSimulator.Api.Tests.Domain;

public sealed class ScenarioScoringTests
{
    private static Scenario CreateScenario()
    {
        var scenario = new Scenario { Code = "demo", Name = "Demo" };
        scenario.Steps.Add(new ScenarioStep { ScenarioCode = "demo", Code = "b", Stage = "s", Name = "B", MaxPoints = 20, DisplayOrder = 2 });
        scenario.Steps.Add(new ScenarioStep { ScenarioCode = "demo", Code = "a", Stage = "s", Name = "A", MaxPoints = 10, DisplayOrder = 1 });
        return scenario;
    }

    [Fact]
    public void Valid_report_snapshots_the_rubric_in_display_order()
    {
        var result = CreateScenario().Score([("b", 15), ("a", 10)]);

        Assert.True(result.IsValid);
        Assert.Collection(result.Steps,
            a => Assert.Equal(("a", 10, 10, 1), (a.StepCode, a.EarnedPoints, a.MaxPoints, a.DisplayOrder)),
            b => Assert.Equal(("b", 15, 20, 2), (b.StepCode, b.EarnedPoints, b.MaxPoints, b.DisplayOrder)));
    }

    [Fact]
    public void Missing_unknown_and_duplicated_steps_are_rejected()
    {
        var result = CreateScenario().Score([("b", 1), ("b", 2), ("zzz", 1)]);

        Assert.False(result.IsValid);
        Assert.Contains("steps", result.Errors.Keys);
        Assert.Contains("steps.unknown", result.Errors.Keys);
        Assert.Contains("steps.missing", result.Errors.Keys);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Points_outside_the_step_range_are_rejected(int points)
    {
        var result = CreateScenario().Score([("a", points), ("b", 0)]);

        Assert.False(result.IsValid);
        Assert.Contains("steps.a", result.Errors.Keys);
    }
}
