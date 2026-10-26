using System.Net;
using LabSimulator.Api.Common;
using LabSimulator.Api.Data;
using LabSimulator.Api.Domain;
using LabSimulator.Api.Features.Attempts;
using LabSimulator.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LabSimulator.Api.Tests;

public sealed class AttemptTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static readonly int StepCount = ScenarioSeed.InfusionPumpSteps.Length;
    private static readonly int MaxScore = ScenarioSeed.InfusionPumpSteps.Sum(s => s.MaxPoints);

    [Fact]
    public async Task Practice_attempt_is_scored_by_the_server()
    {
        var student = await CreateUserAsync(displayName: "Luis");

        var response = await PostAsync(student.Client, "/api/v1/attempts", AttemptRequest(pointsPerStep: 50));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var attempt = await ReadAsync<AttemptResponse>(response);
        Assert.Equal((50 * StepCount, MaxScore), (attempt.Score, attempt.MaxScore));
        Assert.Equal(Math.Round(100m * 50 * StepCount / MaxScore, 2), attempt.ScorePercent);
        Assert.Equal(("Luis", 125, (Guid?)null), (attempt.Student.DisplayName, attempt.RemainingSeconds, attempt.MatchId));
        Assert.Equal(ScenarioSeed.InfusionPumpSteps.Select(s => s.Code), attempt.Steps.Select(s => s.StepCode));

        var fetched = await ReadAsync<AttemptResponse>(await student.Client.GetAsync(response.Headers.Location, Ct));
        Assert.Equal(attempt.Id, fetched.Id);

        var mine = await ReadAsync<PagedResponse<AttemptResponse>>(await student.Client.GetAsync("/api/v1/me/attempts", Ct));
        Assert.Equal(attempt.Id, Assert.Single(mine.Items).Id);
    }

    [Fact]
    public async Task Attempts_that_do_not_match_the_rubric_are_rejected()
    {
        var student = await CreateUserAsync();
        var valid = AttemptRequest(pointsPerStep: 10);
        var overMax = valid with
        {
            Steps = [.. valid.Steps!.Skip(1), valid.Steps![0] with { EarnedPoints = int.MaxValue }],
        };
        var missingAndUnknown = valid with
        {
            Steps = [.. valid.Steps!.Skip(1), new StepScoreRequest { StepCode = "made-up", EarnedPoints = 1 }],
        };

        var tooMany = await PostAsync(student.Client, "/api/v1/attempts", overMax);
        var wrongSteps = await PostAsync(student.Client, "/api/v1/attempts", missingAndUnknown);
        var noScenario = await PostAsync(student.Client, "/api/v1/attempts", valid with { ScenarioCode = "nope" });

        Assert.Contains($"steps.{valid.Steps![0].StepCode}", (await ReadAsync<ValidationProblemDetails>(tooMany)).Errors.Keys);
        var errors = (await ReadAsync<ValidationProblemDetails>(wrongSteps)).Errors.Keys;
        Assert.Contains("steps.missing", errors);
        Assert.Contains("steps.unknown", errors);
        Assert.Equal(HttpStatusCode.BadRequest, noScenario.StatusCode);
    }

    [Fact]
    public async Task Match_attempts_require_joining_a_running_match()
    {
        var teacher = await CreateUserAsync(Role.Teacher);
        var student = await CreateUserAsync();
        var match = await CreateMatchAsync(teacher);

        var notJoined = await PostAsync(student.Client, "/api/v1/attempts", AttemptRequest(10, match.Id));
        Assert.Equal(HttpStatusCode.Forbidden, notJoined.StatusCode);

        (await student.Client.PostAsync($"/api/v1/matches/{match.Id}/participants", null, Ct)).EnsureSuccessStatusCode();
        var joined = await PostAsync(student.Client, "/api/v1/attempts", AttemptRequest(10, match.Id));
        Assert.Equal(HttpStatusCode.Created, joined.StatusCode);

        (await teacher.Client.PostAsync($"/api/v1/matches/{match.Id}/end", null, Ct)).EnsureSuccessStatusCode();
        var afterEnd = await PostAsync(student.Client, "/api/v1/attempts", AttemptRequest(10, match.Id));
        Assert.Equal(HttpStatusCode.Conflict, afterEnd.StatusCode);

        var unknownMatch = await PostAsync(student.Client, "/api/v1/attempts", AttemptRequest(10, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, unknownMatch.StatusCode);
    }

    [Fact]
    public async Task Attempts_are_visible_to_their_student_and_the_match_host_only()
    {
        var teacher = await CreateUserAsync(Role.Teacher);
        var otherTeacher = await CreateUserAsync(Role.Teacher);
        var student = await CreateUserAsync();
        var classmate = await CreateUserAsync();
        var match = await CreateMatchAsync(teacher);
        (await student.Client.PostAsync($"/api/v1/matches/{match.Id}/participants", null, Ct)).EnsureSuccessStatusCode();
        var attempt = await ReadAsync<AttemptResponse>(
            await PostAsync(student.Client, "/api/v1/attempts", AttemptRequest(30, match.Id)));
        var url = $"/api/v1/attempts/{attempt.Id}";

        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.GetAsync(url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await classmate.Client.GetAsync(url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherTeacher.Client.GetAsync(url, Ct)).StatusCode);

        var classResults = await ReadAsync<PagedResponse<AttemptResponse>>(
            await teacher.Client.GetAsync($"/api/v1/matches/{match.Id}/attempts", Ct));
        Assert.Equal(attempt.Id, Assert.Single(classResults.Items).Id);
        Assert.Equal(HttpStatusCode.NotFound,
            (await student.Client.GetAsync($"/api/v1/matches/{match.Id}/attempts", Ct)).StatusCode);
    }
}
