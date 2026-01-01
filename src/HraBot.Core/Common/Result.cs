using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Amazon.Lambda.Annotations.APIGateway;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HraBot.Core;

public class Result
{
    public virtual HraBotError? Error { get; }

    protected Result()
    {
        this.IsError = false;
    }

    protected Result(HraBotError error)
    {
        this.IsError = true;
        this.Error = error;
    }

    [JsonConstructor]
    [Obsolete("Deserialization constructor. Don't use")]
    public Result(HraBotError? error, bool isError)
    {
        this.Error = error;
        this.IsError = isError;
    }

    [MemberNotNullWhen(true, nameof(Error))]
    public virtual bool IsError { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    [JsonIgnore]
    public virtual bool IsSuccess => !this.IsError;

    public static implicit operator Result(HraBotError error) => new(error);

    public void ThrowIfError()
    {
        if (this.IsError)
        {
            throw new HraBotException(
                $@"
Result is in error state.
Message: {this.Error.Description}
ErrorCode: {this.Error.Code}
ErrorType: {this.Error.Type}
StackTrace: {(this.Error.Metadata?.TryGetValue("StackTrace", out var stackTrace) ?? false ? stackTrace : "")}
Metadata: {this.Error.Metadata}
"
            );
        }
    }

    public static Result Success { get; } = new();
}

public sealed class Result<TValue> : Result
{
    public TValue? Value { get; }

    public override HraBotError? Error => base.Error;

    private Result(TValue value)
        : base()
    {
        this.Value = value;
    }

    private Result(HraBotError error)
        : base(error) { }

    [JsonConstructor]
    [Obsolete("Deserialization constructor. Don't use")]
    public Result(TValue? value, HraBotError? error, bool isError)
        : base(error, isError)
    {
        this.Value = value;
    }

#if NET6_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
#endif
    public override bool IsError => base.IsError;

#if NET6_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
#endif

    [JsonIgnore]
    public override bool IsSuccess => !this.IsError;

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(HraBotError error) => new(error);

    // public static Result<TValue> Success() => new(default(TValue), null, false);

    public IResult ToWebResult(CancellationToken _ = default)
    {
        if (this.IsSuccess)
        {
            return TypedResults.Ok(this.Value);
        }
        return MapErrorToResult(this.Error);
    }

    public IHttpResult ToLambdaResult()
    {
        if (IsSuccess)
        {
            return HttpResults.Ok(Value);
        }
        return MapLambdaErrorToResult(Error);
    }

    internal static IHttpResult MapLambdaErrorToResult(HraBotError error) =>
        error.Type switch
        {
            ErrorType.None => throw new NotImplementedException(),
            ErrorType.Failure => HttpResults.InternalServerError(error.Description),
            ErrorType.Validation => HttpResults.BadRequest(error.Description),
            ErrorType.Conflict => HttpResults.Conflict(error.Description),
            ErrorType.NotFound => HttpResults.NotFound(error.Description),
            ErrorType.Unauthorized => HttpResults.Unauthorized(),
            ErrorType.Forbidden => HttpResults.Forbid(error.Description),
            // ErrorType.InvalidOperation => HttpResults.(
            //     title: "Invalid Operation Error",
            //     detail: error.Description,
            //     statusCode: StatusCodes.Status422UnprocessableEntity,
            //     type: "https://tools.ietf.org/html/rfc4918#section-11.2",
            //     extensions: error.Metadata
            // ),
            _ => throw new NotImplementedException(),
        };

    internal static ProblemHttpResult MapErrorToResult(HraBotError error) =>
        error.Type switch
        {
            ErrorType.None => throw new NotImplementedException(),
            ErrorType.Failure => TypedResults.Problem(
                title: "Internal Server Error",
                detail: error.Description,
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                extensions: error.Metadata
            ),
            ErrorType.Validation => TypedResults.Problem(
                title: "Validation Error",
                detail: error.Description,
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                extensions: error.Metadata
            ),
            ErrorType.Conflict => TypedResults.Problem(
                title: "Conflict Error",
                detail: error.Description,
                statusCode: StatusCodes.Status409Conflict,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                extensions: error.Metadata
            ),
            ErrorType.NotFound => TypedResults.Problem(
                title: "Not Found Error",
                detail: error.Description,
                statusCode: StatusCodes.Status404NotFound,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                extensions: error.Metadata
            ),
            ErrorType.Unauthorized => TypedResults.Problem(
                title: "Unauthorized Error",
                detail: error.Description,
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.2",
                extensions: error.Metadata
            ),
            ErrorType.Forbidden => TypedResults.Problem(
                title: "Forbidden Error",
                detail: error.Description,
                statusCode: StatusCodes.Status403Forbidden,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                extensions: error.Metadata
            ),
            ErrorType.InvalidOperation => TypedResults.Problem(
                title: "Invalid Operation Error",
                detail: error.Description,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "https://tools.ietf.org/html/rfc4918#section-11.2",
                extensions: error.Metadata
            ),
            _ => throw new NotImplementedException(),
        };
}
