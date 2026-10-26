using LabSimulator.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LabSimulator.Api.Data;

public sealed class LabSimulatorDbContext(DbContextOptions<LabSimulatorDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Dedicated schema: Supabase only exposes <c>public</c> through its Data API, so tables
    /// here are reachable exclusively through this API.
    /// </summary>
    public const string Schema = "labsim";

    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();

    public DbSet<Scenario> Scenarios => Set<Scenario>();

    public DbSet<ScenarioAttempt> ScenarioAttempts => Set<ScenarioAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LabSimulatorDbContext).Assembly);
    }
}
