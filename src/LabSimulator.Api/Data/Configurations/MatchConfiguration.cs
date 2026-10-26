using LabSimulator.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabSimulator.Api.Data.Configurations;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.RoomName).HasMaxLength(Match.RoomNameMaxLength);

        builder.HasOne(m => m.Host)
            .WithMany()
            .HasForeignKey(m => m.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Participants)
            .WithOne()
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.HostId, m.StartedAt });
    }
}

internal sealed class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.ToTable("match_participants");
        builder.HasKey(p => new { p.MatchId, p.ProfileId });

        builder.HasOne(p => p.Profile)
            .WithMany()
            .HasForeignKey(p => p.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.ProfileId);
    }
}
