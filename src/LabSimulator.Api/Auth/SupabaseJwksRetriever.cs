using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace LabSimulator.Api.Auth;

/// <summary>
/// Supabase Auth publishes its asymmetric signing keys as a bare JWKS document rather than an
/// OpenID discovery document. This retriever wraps that JWKS so the standard JwtBearer
/// <see cref="ConfigurationManager{T}"/> can cache and rotate the keys.
/// </summary>
internal sealed class SupabaseJwksRetriever(string issuer) : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var json = await retriever.GetDocumentAsync(address, cancel);
        var keySet = new JsonWebKeySet(json);

        var configuration = new OpenIdConnectConfiguration { Issuer = issuer, JsonWebKeySet = keySet };
        foreach (var key in keySet.GetSigningKeys())
        {
            configuration.SigningKeys.Add(key);
        }

        return configuration;
    }
}
