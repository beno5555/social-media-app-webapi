using aspnetproject.Common.ProjectConstants;
using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
{
    public void Configure(EntityTypeBuilder<PostImage> builder)
    {
        builder.ToTable("PostImages");
        builder.HasKey(image => image.Id);

        builder.Property(image => image.Address)
            .IsRequired()
            .HasMaxLength(Constants.ImageAddressMaxLength);

        builder.HasOne(image => image.Post)
            .WithMany(post => post.Images)
            .HasForeignKey(image => image.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
