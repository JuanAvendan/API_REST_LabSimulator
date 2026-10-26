namespace LabSimulator.Api.Domain;

public sealed class MatchParticipant
{
    public Guid MatchId { get; init; }

    public Guid ProfileId { get; init; }

    public Profile Profile { get; init; } = null!;

    public DateTimeOffset JoinedAt { get; init; }
}
