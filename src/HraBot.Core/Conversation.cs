using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HraBot.Core;

public class Conversation
{
    public long Id { get; private set; }

    public int NumMessages { get; private set; }

    public void AddMessage(Role role, string content)
    {
        if (this.Messages is null)
        {
            throw new InvalidOperationException("Messages prop is null on conversation object");
        }
        this.Messages.Add(
            new()
            {
                Id = ++NumMessages,
                ConversationId = Id,
                Role = role,
                Content = content,
            }
        );
    }

    public List<Message>? Messages { get; }

    public byte[] RowVersion { get; set; } = null!;
}

public class ConversationConfig : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
