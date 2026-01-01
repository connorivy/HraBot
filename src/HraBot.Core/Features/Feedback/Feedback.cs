using System.Diagnostics;
using HraBot.Core;
using HraBot.Core.Common;
using HraBot.Core.Features.Chat;
using Microsoft.EntityFrameworkCore;

namespace HraBot.Core.Features.Feedback;

public record FeedbackContract(
    long MessageId,
    List<long> MessageFeedbackItemIds,
    string? AdditionalComments
);

public record EntityResponse<TId>(TId Id);

[HraBotEndpoint(Http.Post, "/feedback")]
public partial class AddFeedback(HraBotDbContext context)
    : BaseEndpoint<FeedbackContract, EntityResponse<long>>
{
    public override async Task<Result<EntityResponse<long>>> ExecuteRequestAsync(
        FeedbackContract req,
        CancellationToken ct = default
    )
    {
        if (req.MessageFeedbackItemIds.Count == 0)
        {
            return HraBotError.Validation(
                description: "At least one feedback item id is required."
            );
        }

        var aiMessage = await context.Messages.FirstOrDefaultAsync(
            message => message.Id == req.MessageId && message.Role == Role.Ai,
            ct
        );
        if (aiMessage is null)
        {
            var conversation = await context
                .Conversations.Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == req.MessageId, ct);
            aiMessage = conversation
                ?.Messages?.Where(message => message.Role == Role.Ai)
                .OrderByDescending(message => message.Sequence)
                .FirstOrDefault();
        }

        if (aiMessage is null)
        {
            return HraBotError.NotFound(
                description: $"Could not find AI message for id {req.MessageId}."
            );
        }

        var hasExistingFeedback = await context.MessageFeedbacks.AnyAsync(
            feedback => feedback.MessageId == aiMessage.Id,
            ct
        );
        if (hasExistingFeedback)
        {
            return HraBotError.Conflict(
                description: $"Feedback already exists for message {aiMessage.Id}."
            );
        }

        var feedbackItems = await context
            .MessageFeedbackItems.Where(item => req.MessageFeedbackItemIds.Contains(item.Id))
            .ToListAsync(ct);
        if (feedbackItems.Count != req.MessageFeedbackItemIds.Count)
        {
            return HraBotError.Validation(
                description: "One or more feedback items could not be found."
            );
        }

        var feedback = new MessageFeedback
        {
            MessageId = aiMessage.Id,
            AdditionalComments = req.AdditionalComments,
            MessageFeedbackItems = feedbackItems,
        };
        context.MessageFeedbacks.Add(feedback);
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
            .Include(m => m.MessageFeedbackItems)
            .FirstOrDefaultAsync(m => m.Id == req);
        if (feedback is null)
        {
            return HraBotError.NotFound();
        }
        if (feedback.MessageFeedbackItems is null)
        {
            throw new UnreachableException("Cannot happen after proper query");
        }
        return new FeedbackContract(
            feedback.MessageId,
            [.. feedback.MessageFeedbackItems.Select(i => i.Id)],
            feedback.AdditionalComments
        );
    }
}

// public partial class GetFeedback
// {
//     [LambdaFunction()]
//     [HttpApi(LambdaHttpMethod.Post, "/feedback/{id}")]
//     public async Task<IHttpResult> FeedbackLambda(int id, ILambdaContext hello)
//     {
//         return (await this.ExecuteAsync(request)).ToWebResult();
//     }
// }
