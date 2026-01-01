using Amazon.Lambda.Annotations.APIGateway;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HraBot.Core.Features.Ping;

public class Ping_ConfigureWebApi : HraBot.Core.Common.IBaseEndpoint
{
    public static void Configure(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/ping", () => "hello");
    }
}

public class Ping
{
    [Amazon.Lambda.Annotations.LambdaFunction()]
    [Amazon.Lambda.Annotations.APIGateway.HttpApi(
        Amazon.Lambda.Annotations.APIGateway.LambdaHttpMethod.Get,
        "/ping"
    )]
    public async System.Threading.Tasks.Task<Amazon.Lambda.Annotations.APIGateway.IHttpResult> PingLambda(
        Amazon.Lambda.Core.ILambdaContext _
    )
    {
        return HttpResults.Ok("hello");
    }
}
