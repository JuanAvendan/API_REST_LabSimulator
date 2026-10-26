using LabSimulator.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabSimulator.Api.Data.Configurations;

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.DisplayName).HasMaxLength(Profile.DisplayNameMaxLength);
        builder.Property(p => p.Role).HasConversion<string>().HasMaxLength(16);
    }
}
