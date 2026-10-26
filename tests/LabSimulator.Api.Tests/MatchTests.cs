using System.Net;
using LabSimulator.Api.Common;
using LabSimulator.Api.Domain;
using LabSimulator.Api.Features.Matches;
using LabSimulator.Api.Tests.Infrastructure;

namespace LabSimulator.Api.Tests;

public sealed class MatchTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Teacher_creates_a_match_and_receives_its_id()
    {
        var teacher = await CreateUserAsync(Role.Teacher, "Profesora Gómez");

        var response = await PostAsync(teacher.Client, "/api/v1/matches", new CreateMatchRequest { RoomName = "UCI 1" });

        // Regression: the old API saved the match but answered 500 without its id.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var match = await ReadAsync<MatchResponse>(response);
        Assert.NotEqual(Guid.Empty, match.Id);
        Assert.Equal($"/api/v1/matches/{match.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(("UCI 1", "Profesora Gómez", 0), (match.RoomName, match.Host.DisplayName, match.PlayerCount));
        Assert.Null(match.EndedAt);
    }

    [Fact]
    public async Task Students_cannot_create_matches()
    {
        var student = await CreateUserAsync();

        var response = await PostAsync(student.Client, "/api/v1/matches", new CreateMatchRequest { RoomName = "Sala" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Joining_is_idempotent_and_visible_to_participants_only()
    {
        var teacher = await CreateUserAsync(Role.Teacher);
        var student = await CreateUserAsync();
        var outsider = await CreateUserAsync();
        var match = await CreateMatchAsync(teacher);
        var url = $"/api/v1/matches/{match.Id}";

        Assert.Equal(HttpStatusCode.NoContent, (await student.Client.PostAsync($"{url}/participants", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await student.Client.PostAsync($"{url}/participants", null, Ct)).StatusCode);

        var seen = await ReadAsync<MatchResponse>(await student.Client.GetAsync(url, Ct));
        Assert.Equal(1, seen.PlayerCount);
        Assert.Equal(student.Id, Assert.Single(seen.Participants).ProfileId);

        Assert.Equal(HttpStatusCode.NotFound, (await outsider.Client.GetAsync(url, Ct)).StatusCode);
    }

    [Fact]
    public async Task Only_the_host_can_end_a_match_and_only_once()
    {
        var teacher = await CreateUserAsync(Role.Teacher);
        var otherTeacher = await CreateUserAsync(Role.Teacher);
        var student = await CreateUserAsync();
        var match = await CreateMatchAsync(teacher);
        var endUrl = $"/api/v1/matches/{match.Id}/end";

        Assert.Equal(HttpStatusCode.Forbidden, (await otherTeacher.Client.PostAsync(endUrl, null, Ct)).StatusCode);

        var ended = await teacher.Client.PostAsync(endUrl, null, Ct);
        Assert.Equal(HttpStatusCode.OK, ended.StatusCode);
        Assert.NotNull((await ReadAsync<MatchResponse>(ended)).EndedAt);

        Assert.Equal(HttpStatusCode.Conflict, (await teacher.Client.PostAsync(endUrl, null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await student.Client.PostAsync($"/api/v1/matches/{match.Id}/participants", null, Ct)).StatusCode);
    }

    [Fact]
    public async Task History_lists_hosted_and_joined_matches_newest_first()
    {
        var teacher = await CreateUserAsync(Role.Teacher);
        var student = await CreateUserAsync();
        var first = await CreateMatchAsync(teacher, "Primera");
        var second = await CreateMatchAsync(teacher, "Segunda");
        foreach (var match in new[] { first, second })
        {
            (await student.Client.PostAsync($"/api/v1/matches/{match.Id}/participants", null, Ct)).EnsureSuccessStatusCode();
        }

        var studentHistory = await ReadAsync<PagedResponse<MatchSummaryResponse>>(await student.Client.GetAsync("/api/v1/me/matches", Ct));
        var teacherHistory = await ReadAsync<PagedResponse<MatchSummaryResponse>>(await teacher.Client.GetAsync("/api/v1/me/matches", Ct));

        Assert.Equal([second.Id, first.Id], studentHistory.Items.Select(m => m.Id));
        Assert.All(studentHistory.Items, m => Assert.Equal(1, m.PlayerCount));
        Assert.Equal(2, teacherHistory.TotalCount);
    }

    [Fact]
    public async Task Paging_parameters_are_validated()
    {
        var student = await CreateUserAsync();

        var response = await student.Client.GetAsync("/api/v1/me/matches?pageSize=1000", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
