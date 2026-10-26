using LabSimulator.Api.Data;
using LabSimulator.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ApiFactory))]

namespace LabSimulator.Api.Tests.Infrastructure;

/// <summary>Runs the real API against a throwaway PostgreSQL container with all migrations applied.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public TestTokens Tokens { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<LabSimulatorDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:LabSimulator", _postgres.GetConnectionString());
        builder.UseSetting("Supabase:ProjectUrl", TestTokens.ProjectUrl);
        builder.UseSetting("RateLimiting:PermitsPerMinute", "100000");

        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.ConfigurationManager =
                    new StaticConfigurationManager<OpenIdConnectConfiguration>(Tokens.OpenIdConfiguration)));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
