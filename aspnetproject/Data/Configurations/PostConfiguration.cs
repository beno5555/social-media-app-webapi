using aspnetproject.Common.ProjectConstants;
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

        builder.HasOne(post => post.User)
            .WithMany()
            .HasForeignKey(post => post.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}