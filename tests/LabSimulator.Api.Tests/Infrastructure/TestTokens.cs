using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace LabSimulator.Api.Tests.Infrastructure;

/// <summary>Issues ES256 access tokens shaped like the ones Supabase Auth signs with asymmetric keys.</summary>
public sealed class TestTokens
{
    public const string ProjectUrl = "https://test-project.supabase.co";
    public const string Issuer = ProjectUrl + "/auth/v1";

    private readonly ECDsaSecurityKey _signingKey = CreateKey("test-key");

    public TestTokens()
    {
        OpenIdConfiguration = new OpenIdConnectConfiguration { Issuer = Issuer };
        OpenIdConfiguration.SigningKeys.Add(_signingKey);
    }

    public OpenIdConnectConfiguration OpenIdConfiguration { get; }

    public string Create(Guid userId, string issuer = Issuer, SecurityKey? signingKey = null, DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = "authenticated",
            IssuedAt = now.AddMinutes(-10),
            NotBefore = now.AddMinutes(-10),
            Expires = expires ?? now.AddHours(1),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                ["role"] = "authenticated",
                ["email"] = $"{userId:N}@example.test",
            },
            SigningCredentials = new SigningCredentials(signingKey ?? _signingKey, SecurityAlgorithms.EcdsaSha256),
        });
    }

    public static ECDsaSecurityKey CreateKey(string keyId) =>
        new(ECDsa.Create(ECCurve.NamedCurves.nistP256)) { KeyId = keyId };
}
