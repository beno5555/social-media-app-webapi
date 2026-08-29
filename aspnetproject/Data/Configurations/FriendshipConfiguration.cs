using aspnetproject.Data.Models;
using aspnetproject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("Friendships");
        builder.HasKey(friendship => new { friendship.RequesterUserId, friendship.AddresseeUserId });

        builder.Property(friendship => friendship.FriendshipStatus)
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.Property(friendship => friendship.SentAt)
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.HasOne(friendship => friendship.RequesterUser)
            .WithMany()
            .HasForeignKey(friendship => friendship.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(friendship => friendship.AddresseeUser)
            .WithMany()
            .HasForeignKey(friendship => friendship.AddresseeUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}