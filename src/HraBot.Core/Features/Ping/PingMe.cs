using Amazon.Lambda.Annotations.APIGateway;

namespace HraBot.Core.Features.Ping;

public class PingMe
{
    [Amazon.Lambda.Annotations.LambdaFunction()]
    [Amazon.Lambda.Annotations.APIGateway.HttpApi(
        Amazon.Lambda.Annotations.APIGateway.LambdaHttpMethod.Get,
        "/pingme"
    )]
    public async System.Threading.Tasks.Task<Amazon.Lambda.Annotations.APIGateway.IHttpResult> PingLambda(
        Amazon.Lambda.Core.ILambdaContext _
    )
    {
        return HttpResults.Ok("hello");
    }
}
