using aspnetproject.Models;
using aspnetproject.ProjectConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table => table.HasCheckConstraint("CK_User_DateOfBirth", 
            "DATEDIFF(year, DateOfBirth, GETUTCDATE()) BETWEEN 13 AND 100") );
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username)
            .IsRequired()
            .HasMaxLength(Constants.UsernameMaxlength);
        
        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(Constants.EmailMaxLength);

        builder.Property(user => user.DateOfBirth)
            .IsRequired();

        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .IsFixedLength()
            .HasMaxLength(Constants.PasswordHashMaxLength);
        
        builder.Property(user => user.PasswordSalt)
            .IsRequired()
            .IsFixedLength()
            .HasMaxLength(Constants.PasswordSaltMaxLength);

        builder.Property(user => user.Bio)
            .HasMaxLength(Constants.BioMaxLength);
    }
}