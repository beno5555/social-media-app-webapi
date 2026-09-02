using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("Friendships");
        builder.HasKey(friendship => friendship.Id);

        builder.HasIndex(friendship => new { friendship.RequesterUserId, friendship.AddresseeUserId })
            .IsUnique();

        builder.Property(friendship => friendship.FriendshipStatus)
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.Property(friendship => friendship.SentAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        // service implementation must manually set the user's friendships to null before deleting the user.
        builder.HasOne(friendship => friendship.RequesterUser)
            .WithMany()
            .HasForeignKey(friendship => friendship.RequesterUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(friendship => friendship.AddresseeUser)
            .WithMany()
            .HasForeignKey(friendship => friendship.AddresseeUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}