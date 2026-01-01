using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HraBot.Core.Features.Feedback;

public class MessageFeedback
{
    public long Id { get; private set; }
    public long MessageId { get; set; }
    public Message? Message { get; set; }
    public List<MessageFeedbackItem>? MessageFeedbackItems { get; set; }
    public string? AdditionalComments { get; set; }
    public byte? ImportanceToTakeCommand { get; set; }
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

        builder.HasMany(b => b.MessageFeedbackItems).WithMany(b => b.MessageFeedbacks);
    }
}

public class MessageFeedbackItem
{
    public long Id { get; set; }
    public required string ShortDescription { get; set; }
    public required FeedbackItem FeedbackItem { get; set; }
    public required FeedbackType FeedbackType { get; set; }
    public List<MessageFeedback>? MessageFeedbacks { get; set; }
}

public enum FeedbackItem : byte
{
    Undefined = 0,
    MessageContent,
    Citation,
    Ux,
    Other = 255,
}

public enum FeedbackType : byte
{
    Undefined = 0,
    Positive,
    Negative,
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
                ShortDescription = "no issues",
                FeedbackItem = FeedbackItem.MessageContent,
                FeedbackType = FeedbackType.Positive,
            },
            new MessageFeedbackItem()
            {
                Id = 4,
                ShortDescription = "incorrect",
                FeedbackItem = FeedbackItem.MessageContent,
                FeedbackType = FeedbackType.Negative,
            },
            new MessageFeedbackItem()
            {
                Id = 5,
                ShortDescription = "missing information",
                FeedbackItem = FeedbackItem.MessageContent,
                FeedbackType = FeedbackType.Negative,
            },
            new MessageFeedbackItem()
            {
                Id = 6,
                ShortDescription = "not applicable to question",
                FeedbackItem = FeedbackItem.MessageContent,
                FeedbackType = FeedbackType.Negative,
            },
            new MessageFeedbackItem()
            {
                Id = 7,
                ShortDescription = "not informed by citations",
                FeedbackItem = FeedbackItem.MessageContent,
                FeedbackType = FeedbackType.Negative,
            },
            new MessageFeedbackItem()
            {
                Id = 8,
                ShortDescription = "other",
                FeedbackItem = FeedbackItem.MessageContent,
                FeedbackType = FeedbackType.Negative,
            },
            // citation
            new MessageFeedbackItem()
            {
                Id = 2,
                ShortDescription = "no issues",
                FeedbackItem = FeedbackItem.Citation,
                FeedbackType = FeedbackType.Positive,
            },
            new MessageFeedbackItem()
            {
                Id = 9,
                ShortDescription = "missing",
                FeedbackItem = FeedbackItem.Citation,
                FeedbackType = FeedbackType.Negative,
            },
            new MessageFeedbackItem()
            {
                Id = 10,
                ShortDescription = "incorrect",
                FeedbackItem = FeedbackItem.Citation,
                FeedbackType = FeedbackType.Negative,
            },
            new MessageFeedbackItem()
            {
                Id = 11,
                ShortDescription = "not applicable to question",
                FeedbackItem = FeedbackItem.Citation,
                FeedbackType = FeedbackType.Negative,
            },
            new MessageFeedbackItem()
            {
                Id = 12,
                ShortDescription = "other",
                FeedbackItem = FeedbackItem.Citation,
                FeedbackType = FeedbackType.Negative,
            },
            // ux
            new MessageFeedbackItem()
            {
                Id = 3,
                ShortDescription = "no issues",
                FeedbackItem = FeedbackItem.Citation,
                FeedbackType = FeedbackType.Positive,
            },
            new MessageFeedbackItem()
            {
                Id = 13,
                ShortDescription = "too slow",
                FeedbackItem = FeedbackItem.Ux,
                FeedbackType = FeedbackType.Negative,
            },
            new MessageFeedbackItem()
            {
                Id = 14,
                ShortDescription = "other",
                FeedbackItem = FeedbackItem.Ux,
                FeedbackType = FeedbackType.Negative,
            },
            // other
            new MessageFeedbackItem()
            {
                Id = 15,
                ShortDescription = "other",
                FeedbackItem = FeedbackItem.Other,
                FeedbackType = FeedbackType.Negative,
            }
        );
    }
}
