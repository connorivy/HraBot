using System.Diagnostics;
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
public partial class AddFeedback : BaseEndpoint<FeedbackContract, EntityResponse<long>>
{
    public override Task<Result<EntityResponse<long>>> ExecuteRequestAsync(
        FeedbackContract req,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
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
