using System.Net;
using LabSimulator.Api.Common;
using LabSimulator.Api.Domain;
using LabSimulator.Api.Features.Profiles;
using LabSimulator.Api.Features.Scenarios;
using LabSimulator.Api.Tests.Infrastructure;

namespace LabSimulator.Api.Tests;

public sealed class ProfileTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Signed_in_user_without_profile_must_register_first()
    {
        var client = CreateClient(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/me", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/scenarios", Ct)).StatusCode);
    }

    [Fact]
    public async Task Put_me_creates_a_student_profile_and_then_updates_it()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId);

        var created = await PutAsync(client, "/api/v1/me", new UpsertProfileRequest { DisplayName = "  Ana  " });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var profile = await ReadAsync<ProfileResponse>(created);
        Assert.Equal((userId, "Ana", Role.Student), (profile.Id, profile.DisplayName, profile.Role));

        var updated = await PutAsync(client, "/api/v1/me", new UpsertProfileRequest { DisplayName = "Ana María" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Ana María", (await ReadAsync<ProfileResponse>(updated)).DisplayName);

        // The new profile unlocks registered-only endpoints on the next request.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/scenarios", Ct)).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("x")]
    public async Task Put_me_rejects_invalid_display_names(string displayName)
    {
        var response = await PutAsync(CreateClient(Guid.NewGuid()), "/api/v1/me", new UpsertProfileRequest { DisplayName = displayName });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Only_admins_can_change_roles()
    {
        var student = await CreateUserAsync();
        var teacher = await CreateUserAsync(Role.Teacher);
        var admin = await CreateUserAsync(Role.Admin);
        var url = $"/api/v1/profiles/{student.Id}/role";
        var promote = new ChangeRoleRequest { Role = Role.Teacher };

        Assert.Equal(HttpStatusCode.Forbidden, (await PutAsync(student.Client, url, promote)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PutAsync(teacher.Client, url, promote)).StatusCode);

        var response = await PutAsync(admin.Client, url, promote);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Role.Teacher, (await ReadAsync<ProfileResponse>(response)).Role);

        // The role comes from the database, so it applies without a new token.
        await CreateMatchAsync(student);
    }

    [Fact]
    public async Task Admins_cannot_remove_their_own_admin_role()
    {
        var admin = await CreateUserAsync(Role.Admin);

        var response = await PutAsync(admin.Client, $"/api/v1/profiles/{admin.Id}/role", new ChangeRoleRequest { Role = Role.Student });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Teachers_can_list_students_but_students_cannot()
    {
        var student = await CreateUserAsync();
        var teacher = await CreateUserAsync(Role.Teacher);

        Assert.Equal(HttpStatusCode.Forbidden, (await student.Client.GetAsync("/api/v1/profiles", Ct)).StatusCode);

        var response = await teacher.Client.GetAsync("/api/v1/profiles?role=Student&pageSize=100", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadAsync<PagedResponse<ProfileResponse>>(response);
        Assert.All(page.Items, p => Assert.Equal(Role.Student, p.Role));
    }

    [Fact]
    public async Task Scenario_rubric_is_published()
    {
        var student = await CreateUserAsync();

        var scenario = await ReadAsync<ScenarioResponse>(await student.Client.GetAsync("/api/v1/scenarios/infusion-pump", Ct));

        Assert.Equal(11, scenario.Steps.Count);
        Assert.Equal(scenario.Steps.Sum(s => s.MaxPoints), scenario.MaxScore);
        Assert.Equal(HttpStatusCode.NotFound, (await student.Client.GetAsync("/api/v1/scenarios/unknown", Ct)).StatusCode);
    }
}
