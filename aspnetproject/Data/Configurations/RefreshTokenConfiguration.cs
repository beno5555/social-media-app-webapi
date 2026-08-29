using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.TokenHash)
            .IsRequired()
            .HasMaxLength(44);

        builder.HasIndex(refreshToken => refreshToken.TokenHash)
            .IsUnique();

        builder.Property(refreshToken => refreshToken.LastUpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}