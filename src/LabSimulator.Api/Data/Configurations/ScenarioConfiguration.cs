using LabSimulator.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabSimulator.Api.Data.Configurations;

internal sealed class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
{
    public void Configure(EntityTypeBuilder<Scenario> builder)
    {
        builder.ToTable("scenarios");
        builder.HasKey(s => s.Code);
        builder.Property(s => s.Code).HasMaxLength(Scenario.CodeMaxLength);
        builder.Property(s => s.Name).HasMaxLength(100);

        builder.HasMany(s => s.Steps)
            .WithOne()
            .HasForeignKey(s => s.ScenarioCode)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(new Scenario { Code = ScenarioSeed.InfusionPumpCode, Name = "Bomba de infusión" });
    }
}

internal sealed class ScenarioStepConfiguration : IEntityTypeConfiguration<ScenarioStep>
{
    public void Configure(EntityTypeBuilder<ScenarioStep> builder)
    {
        builder.HasKey(s => new { s.ScenarioCode, s.Code });
        builder.Property(s => s.Code).HasMaxLength(ScenarioStep.CodeMaxLength);
        builder.Property(s => s.Stage).HasMaxLength(32);
        builder.Property(s => s.Name).HasMaxLength(100);
        builder.ToTable("scenario_steps", t => t.HasCheckConstraint("ck_scenario_steps_max_points", "max_points > 0"));

        builder.HasData(ScenarioSeed.InfusionPumpSteps);
    }
}
