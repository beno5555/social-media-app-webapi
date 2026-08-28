using aspnetproject.Data.Models;
using aspnetproject.Models;
using aspnetproject.ProjectConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aspnetproject.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.MessageContent)
            .IsRequired()
            .HasMaxLength(Constants.MessageMaxLength);

        builder.Property(message => message.SeenAt)
            .HasDefaultValue(null);

        builder.HasIndex(message => new { message.SenderUserId, message.ReceiverUserId, message.CreatedAt });

        // service implementation must manually delete the user's messages before deleting the user.
        builder.HasOne(message => message.SenderUser)
            .WithMany(senderUser => senderUser.SentMessages)
            .HasForeignKey(message => message.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(message => message.ReceiverUser)
            .WithMany(receiverUser => receiverUser.ReceivedMessages)
            .HasForeignKey(message => message.ReceiverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}