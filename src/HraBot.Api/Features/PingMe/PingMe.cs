namespace HraBot.Api.Features.PingMe;

public class Ping_ConfigureWebApi : HraBot.Core.Common.IBaseEndpoint
{
    public static void Configure(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/pingme", () => "hello");
    }
}
