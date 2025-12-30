using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using HraBot.Core.Common;
using HraBot.Core.Features.Chat;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HraBot.Core.Features.Feedback;

public record FeedbackContract(
    long MessageId,
    long MessageFeedbackItemId,
    string? AdditionalComments
);

[HraBotEndpoint(Http.Post, "/feedback")]
public partial class AddFeedback : BaseEndpoint<FeedbackContract, int>
{
    public override void Configure(IEndpointRouteBuilder builder)
    {
        throw new NotImplementedException();
    }

    public override Task<Result<int>> ExecuteRequestAsync(
        FeedbackContract req,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }
}

[HraBotEndpoint(Http.Get, "/feedback/{id:int}")]
public partial class GetFeedback(HraBotDbContext context) : BaseEndpoint<int, FeedbackContract>
{
    public override void Configure(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/feedback/{id:int}", (int id) => TypedResults.Ok(id));
    }

    public override async Task<Result<FeedbackContract>> ExecuteRequestAsync(
        int req,
        CancellationToken ct = default
    )
    {
        var feedback = await context.MessageFeedbacks.FirstOrDefaultAsync(m => m.Id == req);
        if (feedback is null)
        {
            return HraBotError.NotFound();
        }
        return new FeedbackContract(
            feedback.MessageId,
            feedback.MessageFeedbackItemId,
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
