using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.ToTable("PostLikes");
        builder.HasKey(like => like.Id);

        builder.HasIndex(like => new { like.PostId, like.UserId })
            .IsUnique();

        builder.HasOne(like => like.Post)
            .WithMany(post => post.Likes)
            .HasForeignKey(like => like.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // SQL Server refuses two cascade paths to one table. Likes already cascade from Post, and a post points at a
        // user too, so likes do not cascade from User: the purge of soft-deleted users deletes the likes they gave
        // itself (UserRepository.DeleteSoftDeletedUsersBatchAsync). Optional, like Post's link to User, so a like by a
        // deactivated or soft-deleted user stays readable while the global query filter hides that user.
        builder.HasOne(like => like.User)
            .WithMany()
            .HasForeignKey(like => like.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}
