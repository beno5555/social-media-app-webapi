using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasData(
            new Role
            {
                Id = 1,
                Name = nameof(RoleName.User),
                CreatedAt = new DateTime(2026, 8, 31, 15, 4, 0, DateTimeKind.Utc),
                LastUpdatedAt = null
            },
            new Role
            {
                Id = 2,
                CreatedAt = new DateTime(2026, 8, 31, 15, 0, 0, DateTimeKind.Utc),
                Name = nameof(RoleName.Administrator)
            }
        );
    }
}