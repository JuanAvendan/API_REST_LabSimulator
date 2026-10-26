namespace LabSimulator.Api.Auth;

public static class AppClaimTypes
{
    public const string Subject = "sub";

    /// <summary>Application role (<see cref="Domain.Role"/>) loaded from the profile, not from the token.</summary>
    public const string Role = "app_role";
}

public static class Policies
{
    /// <summary>Authenticated user who has created a profile through <c>PUT /api/v1/me</c>.</summary>
    public const string Registered = nameof(Registered);

    public const string TeacherOrAdmin = nameof(TeacherOrAdmin);

    public const string Admin = nameof(Admin);
}
