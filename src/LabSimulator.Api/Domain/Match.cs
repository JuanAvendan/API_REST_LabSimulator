namespace LabSimulator.Api.Domain;

/// <summary>A class session hosted by a teacher in which students play scenarios.</summary>
public sealed class Match
{
    public const int RoomNameMaxLength = 100;

    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string RoomName { get; set; }

    public Guid HostId { get; init; }

    public Profile Host { get; init; } = null!;

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? EndedAt { get; private set; }

    public List<MatchParticipant> Participants { get; } = [];

    public bool IsEnded => EndedAt is not null;

    public void End(DateTimeOffset endedAt)
    {
        if (IsEnded)
        {
            throw new InvalidOperationException("The match has already ended.");
        }

        EndedAt = endedAt;
    }
}
