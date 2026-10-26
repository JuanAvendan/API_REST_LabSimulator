using LabSimulator.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabSimulator.Api.Data.Configurations;

internal sealed class ScenarioAttemptConfiguration : IEntityTypeConfiguration<ScenarioAttempt>
{
    public void Configure(EntityTypeBuilder<ScenarioAttempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.ScenarioCode).HasMaxLength(Scenario.CodeMaxLength);
        builder.Property(a => a.SelectedMedication).HasMaxLength(ScenarioAttempt.SelectionMaxLength);
        builder.Property(a => a.SelectedPathology).HasMaxLength(ScenarioAttempt.SelectionMaxLength);

        builder.HasOne(a => a.Profile)
            .WithMany()
            .HasForeignKey(a => a.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Scenario>()
            .WithMany()
            .HasForeignKey(a => a.ScenarioCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(a => a.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.StepResults)
            .WithOne()
            .HasForeignKey(r => r.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.ProfileId, a.CompletedAt });
        builder.HasIndex(a => a.MatchId);

        builder.ToTable("scenario_attempts", t =>
        {
            t.HasCheckConstraint("ck_scenario_attempts_score", "score >= 0 AND score <= max_score");
            t.HasCheckConstraint("ck_scenario_attempts_remaining_seconds", "remaining_seconds >= 0");
        });
    }
}

internal sealed class AttemptStepResultConfiguration : IEntityTypeConfiguration<AttemptStepResult>
{
    public void Configure(EntityTypeBuilder<AttemptStepResult> builder)
    {
        builder.HasKey(r => new { r.AttemptId, r.StepCode });
        builder.Property(r => r.StepCode).HasMaxLength(ScenarioStep.CodeMaxLength);
        builder.ToTable("attempt_step_results", t => t.HasCheckConstraint(
            "ck_attempt_step_results_points", "earned_points >= 0 AND earned_points <= max_points"));
    }
}
