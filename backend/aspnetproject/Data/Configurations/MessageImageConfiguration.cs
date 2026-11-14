using aspnetproject.Common.ProjectConstants;
using aspnetproject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class MessageImageConfiguration : IEntityTypeConfiguration<MessageImage>
{
    public void Configure(EntityTypeBuilder<MessageImage> builder)
    {
        builder.ToTable("MessageImages");
        builder.HasKey(image => image.Id);

        builder.Property(image => image.Address)
            .IsRequired()
            .HasMaxLength(Constants.ImageAddressMaxLength);

        // Bulk message deletes (a removed friendship, the purge) use ExecuteDeleteAsync, so the database cascades the rows.
        builder.HasOne(image => image.Message)
            .WithMany(message => message.Images)
            .HasForeignKey(image => image.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
