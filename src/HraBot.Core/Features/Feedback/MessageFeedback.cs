using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HraBot.Core.Features.Feedback;

public class MessageFeedback
{
    public long Id { get; private set; }
    public required long MessageId { get; set; }
    public Message? Message { get; set; }
    public byte Rating { get; set; }
    public byte ImportanceToTakeCommand { get; set; }
}

public class MessageFeedbackConfiguration : IEntityTypeConfiguration<MessageFeedback>
{
    public void Configure(EntityTypeBuilder<MessageFeedback> builder)
    {
        builder
            .HasOne(b => b.Message)
            .WithOne()
            .HasForeignKey<MessageFeedback>(e => e.MessageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
