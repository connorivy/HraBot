using HraBot.Core.Common;
using HraBot.Core.Features.Chat;
using Microsoft.EntityFrameworkCore;

namespace HraBot.Core.Features.Feedback;

public record FeedbackContract(long MessageId, bool IsPositive, byte ImportanceToTakeCommand);

public record FeedbackItemContract(
    long Id,
    string ShortDescription,
    string FeedbackItem,
    string FeedbackType
);

public record EntityResponse<TId>(TId Id);

[HraBotEndpoint(Http.Put, "/feedback")]
public partial class AddFeedback(HraBotDbContext context)
    : BaseEndpoint<FeedbackContract, EntityResponse<long>>
{
    public override async Task<Result<EntityResponse<long>>> ExecuteRequestAsync(
        FeedbackContract req,
        CancellationToken ct = default
    )
    {
        if (req.ImportanceToTakeCommand is < 1 or > 5)
        {
            return HraBotError.Validation(description: "Importance must be between 1 and 5.");
        }

        var aiMessage = await context.Messages.FirstOrDefaultAsync(
            message => message.Id == req.MessageId && message.Role == Role.Ai,
            ct
        );

        if (aiMessage is null)
        {
            return HraBotError.NotFound(
                description: $"Could not find AI message for id {req.MessageId}."
            );
        }

        var feedback = await context.MessageFeedbacks.FirstOrDefaultAsync(
            f => f.MessageId == aiMessage.Id,
            ct
        );

        if (feedback is null)
        {
            feedback = new MessageFeedback { MessageId = aiMessage.Id };
            context.MessageFeedbacks.Add(feedback);
        }

        feedback.ImportanceToTakeCommand = req.ImportanceToTakeCommand;
        feedback.IsPositive = req.IsPositive;

        await context.SaveChangesAsync(ct);
        return new EntityResponse<long>(feedback.Id);
    }
}

[HraBotEndpoint(Http.Get, "/feedback/{id:long}")]
public partial class GetFeedback(HraBotDbContext context) : BaseEndpoint<long, FeedbackContract>
{
    public override async Task<Result<FeedbackContract>> ExecuteRequestAsync(
        long req,
        CancellationToken ct = default
    )
    {
        var feedback = await context
            .MessageFeedbacks.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == req, cancellationToken: ct);
        if (feedback is null)
        {
            return HraBotError.NotFound();
        }
        return new FeedbackContract(
            feedback.MessageId,
            feedback.IsPositive,
            feedback.ImportanceToTakeCommand
        );
    }
}
