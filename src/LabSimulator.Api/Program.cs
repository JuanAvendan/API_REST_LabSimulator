using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LabSimulator.Api.Auth;
using LabSimulator.Api.Common;
using LabSimulator.Api.Data;
using LabSimulator.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Persistence
var connectionString = builder.Configuration.GetConnectionString("LabSimulator");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'LabSimulator' is not configured.");
}

builder.Services.AddDbContext<LabSimulatorDbContext>(options => options
    .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", LabSimulatorDbContext.Schema))
    .UseSnakeCaseNamingConvention());

// Authentication (Supabase Auth access tokens) and authorization
builder.Services.AddOptions<SupabaseAuthOptions>()
    .BindConfiguration(SupabaseAuthOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.ConfigureOptions<ConfigureSupabaseJwtBearer>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddScoped<IClaimsTransformation, ProfileRoleClaimsTransformation>();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.Registered, policy => policy.RequireAuthenticatedUser().RequireClaim(AppClaimTypes.Role))
    .AddPolicy(Policies.TeacherOrAdmin, policy => policy.RequireRole(nameof(Role.Teacher), nameof(Role.Admin)))
    .AddPolicy(Policies.Admin, policy => policy.RequireRole(nameof(Role.Admin)));

// Per-user (or per-IP when anonymous) rate limit
var permitsPerMinute = builder.Configuration.GetValue("RateLimiting:PermitsPerMinute", 120);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(AppClaimTypes.Subject)
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitsPerMinute, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddHealthChecks().AddDbContextCheck<LabSimulatorDbContext>(tags: ["ready"]);
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

app.Run();
