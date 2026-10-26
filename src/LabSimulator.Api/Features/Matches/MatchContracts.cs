using System.ComponentModel.DataAnnotations;
using LabSimulator.Api.Domain;
using LabSimulator.Api.Features.Profiles;

namespace LabSimulator.Api.Features.Matches;

public sealed record CreateMatchRequest
{
    [Required]
    [StringLength(Match.RoomNameMaxLength)]
    public string? RoomName { get; init; }
}

public sealed record ParticipantResponse(Guid ProfileId, string DisplayName, DateTimeOffset JoinedAt);

public sealed record MatchSummaryResponse(
    Guid Id,
    string RoomName,
    ProfileSummary Host,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int PlayerCount);

public sealed record MatchResponse(
    Guid Id,
    string RoomName,
    ProfileSummary Host,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int PlayerCount,
    IReadOnlyList<ParticipantResponse> Participants)
{
    /// <remarks>Requires <see cref="Match.Host"/> and the participants' profiles to be loaded.</remarks>
    public static MatchResponse From(Match match) => new(
        match.Id,
        match.RoomName,
        ProfileSummary.From(match.Host),
        match.StartedAt,
        match.EndedAt,
        match.Participants.Count,
        match.Participants
            .OrderBy(p => p.JoinedAt)
            .Select(p => new ParticipantResponse(p.ProfileId, p.Profile.DisplayName, p.JoinedAt))
            .ToList());
}
