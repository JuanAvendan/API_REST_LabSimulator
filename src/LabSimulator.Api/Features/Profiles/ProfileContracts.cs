using System.ComponentModel.DataAnnotations;
using LabSimulator.Api.Domain;

namespace LabSimulator.Api.Features.Profiles;

public sealed record ProfileResponse(Guid Id, string DisplayName, Role Role, DateTimeOffset CreatedAt)
{
    public static ProfileResponse From(Profile profile) =>
        new(profile.Id, profile.DisplayName, profile.Role, profile.CreatedAt);
}

public sealed record ProfileSummary(Guid Id, string DisplayName)
{
    public static ProfileSummary From(Profile profile) => new(profile.Id, profile.DisplayName);
}

public sealed record UpsertProfileRequest
{
    [Required]
    [StringLength(Profile.DisplayNameMaxLength, MinimumLength = 2)]
    public string? DisplayName { get; init; }
}

public sealed record ChangeRoleRequest
{
    [Required]
    public Role? Role { get; init; }
}
