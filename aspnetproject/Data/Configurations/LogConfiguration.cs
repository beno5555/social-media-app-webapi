using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class LogConfiguration : IEntityTypeConfiguration<Log>
{
    public void Configure(EntityTypeBuilder<Log> builder)
    {
        builder.ToTable("Logs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.AuthorizedRequest)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(log => log.Succeeded)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(log => log.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(log => log.Details)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(log => log.EntityName)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(log => log.User)
            .WithMany(user => user.Logs)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}