using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");
        builder.HasKey(post => post.Id);

        builder.Property(post => post.PostTitle)
            .IsRequired()
            .HasMaxLength(Constants.PostTitleMaxLength);
        
        builder.Property(post => post.PostContent)
            .IsRequired()
            .HasMaxLength(Constants.PostContentMaxLength);

        // The database default makes existing posts visible when the column is added.
        // New posts always carry a status, so EF sends it on every insert and the database default is never used for them.
        builder.Property(post => post.ModerationStatus)
            .IsRequired()
            .HasMaxLength(Constants.ModerationStatusMaxLength)
            .HasConversion<string>()
            .HasDefaultValue(ModerationStatus.Visible)
            .HasSentinel(default(ModerationStatus));

        builder.HasOne(post => post.User)
            .WithMany()
            .HasForeignKey(post => post.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}