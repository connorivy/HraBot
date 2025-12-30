using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HraBot.Core.Features.Feedback;

public class MessageFeedback
{
    public long Id { get; private set; }
    public long MessageId { get; set; }
    public Message? Message { get; set; }
    public long MessageFeedbackItemId { get; set; }
    public MessageFeedbackItem? MessageFeedbackItem { get; set; }
    public string? AdditionalComments { get; set; }
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

        builder
            .HasOne(b => b.MessageFeedbackItem)
            .WithOne()
            .HasForeignKey<MessageFeedback>(e => e.MessageFeedbackItem)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MessageFeedbackItem
{
    public long Id { get; set; }
    public required string ShortDescription { get; set; }
    public required FeedbackItem FeedbackItem { get; set; }
}

public enum FeedbackItem : byte
{
    Undefined = 0,
    MessageContent,
    Citation,
    Ux,
    Other = 255,
}

public class MessageFeedbackItemConfiguration : IEntityTypeConfiguration<MessageFeedbackItem>
{
    public void Configure(EntityTypeBuilder<MessageFeedbackItem> builder)
    {
        builder.Property(b => b.ShortDescription).HasMaxLength(100);
        builder.HasData(
            // message content
            new MessageFeedbackItem()
            {
                Id = 1,
                ShortDescription = "incorrect",
                FeedbackItem = FeedbackItem.MessageContent,
            },
            new MessageFeedbackItem()
            {
                Id = 2,
                ShortDescription = "missing information",
                FeedbackItem = FeedbackItem.MessageContent,
            },
            new MessageFeedbackItem()
            {
                Id = 3,
                ShortDescription = "not applicable to question",
                FeedbackItem = FeedbackItem.MessageContent,
            },
            new MessageFeedbackItem()
            {
                Id = 4,
                ShortDescription = "not informed by citations",
                FeedbackItem = FeedbackItem.MessageContent,
            },
            // citation
            new MessageFeedbackItem()
            {
                Id = 5,
                ShortDescription = "missing",
                FeedbackItem = FeedbackItem.Citation,
            },
            new MessageFeedbackItem()
            {
                Id = 6,
                ShortDescription = "incorrect",
                FeedbackItem = FeedbackItem.Citation,
            },
            new MessageFeedbackItem()
            {
                Id = 7,
                ShortDescription = "not applicable to question",
                FeedbackItem = FeedbackItem.Citation,
            },
            // ux
            new MessageFeedbackItem()
            {
                Id = 8,
                ShortDescription = "too slow",
                FeedbackItem = FeedbackItem.Ux,
            },
            // other
            new MessageFeedbackItem()
            {
                Id = 9,
                ShortDescription = "other",
                FeedbackItem = FeedbackItem.Other,
            }
        );
    }
}
