using Microsoft.AspNetCore.Routing;

namespace HraBot.Core.Common;

public abstract class BaseEndpoint<TRequest, TResponse>
{
    public abstract Task<Result<TResponse>> ExecuteRequestAsync(
        TRequest req,
        CancellationToken ct = default
    );

#if GENERATING_OPENAPI
    public Task<TResponse> ReturnResponse() => Task.FromResult(default(TResponse)!);
#endif

    public async Task<Result<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken ct = default
    )
    {
        try
        {
            return await this.ExecuteRequestAsync(request, ct);
        }
        catch (Exception ex)
        {
            return HraBotError.Failure(
                description: "An unexpected error has occurred. " + ex.Message,
                metadata: new Dictionary<string, object?>
                {
                    ["Request"] = request,
                    ["ExceptionMessage"] = ex.Message,
                    ["StackTrace"] = ex.StackTrace,
                    ["InnerExceptionMessage"] = ex.InnerException?.Message,
                    ["InnerStackTrace"] = ex.InnerException?.StackTrace,
                }
            );
        }
    }

    // public virtual void Configure(IEndpointRouteBuilder builder) { }
}

public interface IBaseEndpoint
{
    public static abstract void Configure(IEndpointRouteBuilder builder);
}
