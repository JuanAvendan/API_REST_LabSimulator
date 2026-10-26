namespace LabSimulator.Api.Domain;

/// <summary>
/// Application profile of a user. Credentials live in Supabase Auth;
/// <see cref="Id"/> is the Supabase user id (the JWT <c>sub</c> claim).
/// </summary>
public sealed class Profile
{
    public const int DisplayNameMaxLength = 50;

    public Guid Id { get; init; }

    public required string DisplayName { get; set; }

    public Role Role { get; set; } = Role.Student;

    public DateTimeOffset CreatedAt { get; init; }
}
