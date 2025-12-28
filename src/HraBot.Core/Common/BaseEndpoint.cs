using Amazon.Lambda.Annotations.APIGateway;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HraBot.Core.Common;

public abstract class BaseEndpoint<TRequest, TResponse>
{
    public abstract Task<Result<TResponse>> ExecuteRequestAsync(
        TRequest req,
        CancellationToken ct = default
    );

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
}
