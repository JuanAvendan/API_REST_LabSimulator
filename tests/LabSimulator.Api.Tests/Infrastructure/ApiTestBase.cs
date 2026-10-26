using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LabSimulator.Api.Data;
using LabSimulator.Api.Domain;
using LabSimulator.Api.Features.Attempts;
using LabSimulator.Api.Features.Matches;
using Microsoft.Extensions.DependencyInjection;

namespace LabSimulator.Api.Tests.Infrastructure;

public sealed record TestUser(Guid Id, HttpClient Client);

public abstract class ApiTestBase(ApiFactory factory)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected ApiFactory Factory { get; } = factory;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Client authenticated as a Supabase user that has not created a profile yet.</summary>
    protected HttpClient CreateClient(Guid userId)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Factory.Tokens.Create(userId));
        return client;
    }

    protected async Task<TestUser> CreateUserAsync(Role role = Role.Student, string? displayName = null)
    {
        var id = Guid.NewGuid();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LabSimulatorDbContext>();
            db.Profiles.Add(new Profile
            {
                Id = id,
                DisplayName = displayName ?? $"{role} {id.ToString()[..8]}",
                Role = role,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(Ct);
        }

        return new TestUser(id, CreateClient(id));
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;

    protected static Task<HttpResponseMessage> PostAsync<T>(HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url, body, Json, Ct);

    protected static Task<HttpResponseMessage> PutAsync<T>(HttpClient client, string url, T body) =>
        client.PutAsJsonAsync(url, body, Json, Ct);

    protected static async Task<MatchResponse> CreateMatchAsync(TestUser teacher, string roomName = "Sala de prueba")
    {
        var response = await PostAsync(teacher.Client, "/api/v1/matches", new CreateMatchRequest { RoomName = roomName });
        response.EnsureSuccessStatusCode();
        return await ReadAsync<MatchResponse>(response);
    }

    protected static SubmitAttemptRequest AttemptRequest(int pointsPerStep, Guid? matchId = null) => new()
    {
        ScenarioCode = ScenarioSeed.InfusionPumpCode,
        MatchId = matchId,
        SelectedMedication = "Dopamina",
        SelectedPathology = "Shock cardiogénico",
        RemainingSeconds = 125,
        Steps = ScenarioSeed.InfusionPumpSteps
            .Select(s => new StepScoreRequest { StepCode = s.Code, EarnedPoints = pointsPerStep })
            .ToList(),
    };
}
