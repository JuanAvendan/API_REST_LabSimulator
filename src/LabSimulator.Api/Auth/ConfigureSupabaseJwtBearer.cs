using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace LabSimulator.Api.Auth;

internal sealed class ConfigureSupabaseJwtBearer(IOptions<SupabaseAuthOptions> supabaseOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        var supabase = supabaseOptions.Value;

        // Keep raw claim names ("sub", "role") instead of the legacy WS-* URIs. Supabase's own
        // "role" claim is the Postgres role, so application roles use a separate claim type.
        options.MapInboundClaims = false;
        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            supabase.JwksUrl,
            new SupabaseJwksRetriever(supabase.Issuer),
            new HttpDocumentRetriever { RequireHttps = true });

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = supabase.Issuer,
            ValidAudience = supabase.Audience,
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],
            NameClaimType = AppClaimTypes.Subject,
            RoleClaimType = AppClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}
