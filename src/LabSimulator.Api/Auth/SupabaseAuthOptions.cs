using System.ComponentModel.DataAnnotations;

namespace LabSimulator.Api.Auth;

public sealed class SupabaseAuthOptions
{
    public const string SectionName = "Supabase";

    /// <summary>Project URL, e.g. <c>https://abcdefghijkl.supabase.co</c>.</summary>
    [Required]
    public Uri? ProjectUrl { get; init; }

    /// <summary>Audience Supabase Auth puts in access tokens for signed-in users.</summary>
    [Required]
    public string Audience { get; init; } = "authenticated";

    public string Issuer => $"{ProjectUrl!.GetLeftPart(UriPartial.Authority)}/auth/v1";

    public string JwksUrl => $"{Issuer}/.well-known/jwks.json";
}
