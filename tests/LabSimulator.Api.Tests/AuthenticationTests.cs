using System.Net;
using System.Net.Http.Headers;
using LabSimulator.Api.Tests.Infrastructure;

namespace LabSimulator.Api.Tests;

public sealed class AuthenticationTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_are_public(string url)
    {
        var response = await Factory.CreateClient().GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Requests_without_a_token_are_rejected()
    {
        var response = await Factory.CreateClient().GetAsync("/api/v1/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tokens_signed_with_an_unknown_key_are_rejected()
    {
        var token = Factory.Tokens.Create(Guid.NewGuid(), signingKey: TestTokens.CreateKey("attacker"));

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeStatusAsync(token));
    }

    [Fact]
    public async Task Tokens_from_another_issuer_are_rejected()
    {
        var token = Factory.Tokens.Create(Guid.NewGuid(), issuer: "https://other-project.supabase.co/auth/v1");

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeStatusAsync(token));
    }

    [Fact]
    public async Task Expired_tokens_are_rejected()
    {
        var token = Factory.Tokens.Create(Guid.NewGuid(), expires: DateTime.UtcNow.AddMinutes(-5));

        Assert.Equal(HttpStatusCode.Unauthorized, await GetMeStatusAsync(token));
    }

    private async Task<HttpStatusCode> GetMeStatusAsync(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.GetAsync("/api/v1/me", Ct)).StatusCode;
    }
}
