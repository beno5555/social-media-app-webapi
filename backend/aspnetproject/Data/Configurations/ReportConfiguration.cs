using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(report => report.Id);

        builder.Property(report => report.TargetType)
            .IsRequired()
            .HasMaxLength(Constants.ReportEnumMaxLength)
            .HasConversion<string>();

        builder.Property(report => report.Status)
            .IsRequired()
            .HasMaxLength(Constants.ReportEnumMaxLength)
            .HasConversion<string>();

        builder.Property(report => report.Reason)
            .IsRequired()
            .HasMaxLength(Constants.ReportReasonMaxLength);

        // One pending report per reporter and target; once it is decided, the same user may report the target again.
        // The service checks first; this index stops two requests that both pass the check at the same moment.
        builder.HasIndex(report => new { report.ReporterUserId, report.TargetType, report.TargetId })
            .IsUnique()
            .HasFilter($"[{nameof(Report.Status)}] = '{nameof(ReportStatus.Pending)}'");

        // The administrator's list filters by status and reads newest first.
        builder.HasIndex(report => new { report.Status, report.CreatedAt });

        // SQL Server refuses two cascade paths from Users to Reports, so neither key cascades: the purge of soft-deleted
        // users deletes the reports they made and clears their reviewer references itself
        // (UserRepository.DeleteSoftDeletedUsersBatchAsync). Optional, like PostLike's link to User, so a report by a
        // deactivated or soft-deleted user stays readable while the global query filter hides that user.
        builder.HasOne(report => report.ReporterUser)
            .WithMany()
            .HasForeignKey(report => report.ReporterUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(report => report.ReviewedByUser)
            .WithMany()
            .HasForeignKey(report => report.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}
