using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

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

    // public ApiResponse<TValue> ToApiResponse()
    // {
    //     if (this.IsSuccess)
    //     {
    //         return ApiResponse.FromValue(this.Value!);
    //     }
    //     return this.Error!.ToProblemDetails();
    // }
}
