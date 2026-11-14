using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.CommentContent)
            .IsRequired()
            .HasMaxLength(Constants.CommentMaxLength);

        // The database default makes existing comments visible when the column is added.
        // New comments always carry a status, so EF sends it on every insert and the database default is never used for them.
        builder.Property(comment => comment.ModerationStatus)
            .IsRequired()
            .HasMaxLength(Constants.ModerationStatusMaxLength)
            .HasConversion<string>()
            .HasDefaultValue(ModerationStatus.Visible)
            .HasSentinel(default(ModerationStatus));

        builder.HasOne(comment => comment.CommenterUser)
            .WithMany()
            .HasForeignKey(comment => comment.CommenterUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(comment => comment.Post)
            .WithMany(post => post.Comments)
            .HasForeignKey(comment => comment.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}