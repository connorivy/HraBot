using System.Text.Json.Serialization;

namespace HraBot.Core;

[method: JsonConstructor]
[method: Obsolete("JSON ctor. Don't use")]
public class HraBotError(
    string code,
    string description,
    ErrorType type,
    IDictionary<string, object?>? metadata
)
{
    public string Code { get; } = code;

    public string Description { get; } = description;

    public ErrorType Type { get; } = type;

    public int NumericType { get; } = (int)type;

    public IDictionary<string, object?>? Metadata { get; } = metadata;

    public static HraBotError Failure(
        string code = "General.Failure",
        string description = "A failure has occurred.",
        IDictionary<string, object?>? metadata = null
#pragma warning disable CS0618 // Type or member is obsolete
    ) => new(code, description, ErrorType.Failure, metadata);
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Creates an <see cref="HraBotError"/> of type <see cref="ErrorType.Validation"/> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CS0618",
        Justification = "Factory method intentionally uses obsolete JSON constructor"
    )]
    public static HraBotError Validation(
        string code = "General.Validation",
        string description = "A validation error has occurred.",
        IDictionary<string, object?>? metadata = null
#pragma warning disable CS0618 // Type or member is obsolete
    ) => new(code, description, ErrorType.Validation, metadata);
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Creates an <see cref="HraBotError"/> of type <see cref="ErrorType.Conflict"/> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CS0618",
        Justification = "Factory method intentionally uses obsolete JSON constructor"
    )]
    public static HraBotError Conflict(
        string code = "General.Conflict",
        string description = "A conflict error has occurred.",
        IDictionary<string, object?>? metadata = null
#pragma warning disable CS0618 // Type or member is obsolete
    ) => new(code, description, ErrorType.Conflict, metadata);
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Creates an <see cref="HraBotError"/> of type <see cref="ErrorType.NotFound"/> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CS0618",
        Justification = "Factory method intentionally uses obsolete JSON constructor"
    )]
    public static HraBotError NotFound(
        string code = "General.NotFound",
        string description = "A 'Not Found' error has occurred.",
        IDictionary<string, object?>? metadata = null
#pragma warning disable CS0618 // Type or member is obsolete
    ) => new(code, description, ErrorType.NotFound, metadata);
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Creates an <see cref="HraBotError"/> of type <see cref="ErrorType.Unauthorized"/> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CS0618",
        Justification = "Factory method intentionally uses obsolete JSON constructor"
    )]
    public static HraBotError Unauthorized(
        string code = "General.Unauthorized",
        string description = "An 'Unauthorized' error has occurred.",
        IDictionary<string, object?>? metadata = null
#pragma warning disable CS0618 // Type or member is obsolete
    ) => new(code, description, ErrorType.Unauthorized, metadata);
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Creates an <see cref="HraBotError"/> of type <see cref="ErrorType.Forbidden"/> from a code and description.
    /// </summary>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CS0618",
        Justification = "Factory method intentionally uses obsolete JSON constructor"
    )]
    public static HraBotError Forbidden(
        string code = "General.Forbidden",
        string description = "A 'Forbidden' error has occurred.",
        IDictionary<string, object?>? metadata = null
#pragma warning disable CS0618 // Type or member is obsolete
    ) => new(code, description, ErrorType.Forbidden, metadata);
#pragma warning restore CS0618 // Type or member is obsolete

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CS0618",
        Justification = "Factory method intentionally uses obsolete JSON constructor"
    )]
    public static HraBotError InvalidOperation(
        string code = "General.InvalidOperation",
        string description = "An 'Invalid Operation' error has occurred.",
        IDictionary<string, object?>? metadata = null
#pragma warning disable CS0618 // Type or member is obsolete
    ) => new(code, description, ErrorType.InvalidOperation, metadata);
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Creates an <see cref="HraBotError"/> with the given numeric <paramref name="type"/>,
    /// <paramref name="code"/>, and <paramref name="description"/>.
    /// </summary>
    /// <param name="type">An integer value which represents the type of error that occurred.</param>
    /// <param name="code">The unique error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="metadata">A dictionary which provides optional space for information.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CS0618",
        Justification = "Factory method intentionally uses obsolete JSON constructor"
    )]
    public static HraBotError Custom(
        int type,
        string code,
        string description,
        IDictionary<string, object?>? metadata = null
#pragma warning disable CS0618 // Type or member is obsolete
    ) => new(code, description, (ErrorType)type, metadata);
#pragma warning restore CS0618 // Type or member is obsolete

    public bool Equals(HraBotError other)
    {
        if (
            this.Type != other.Type
            || this.NumericType != other.NumericType
            || this.Code != other.Code
            || this.Description != other.Description
        )
        {
            return false;
        }

        if (this.Metadata is null)
        {
            return other.Metadata is null;
        }

        return other.Metadata is not null && CompareMetadata(this.Metadata, other.Metadata);
    }

    // public ProblemDetails ToProblemDetails() =>
    //     this.Type switch
    //     {
    //         ErrorType.None => throw new NotImplementedException(),
    //         ErrorType.Failure => new ProblemDetails(
    //             "Internal Server Error",
    //             this.Description,
    //             500,
    //             "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
    //             null,
    //             this.Metadata
    //         ),
    //         ErrorType.Validation => new ProblemDetails(
    //             "Validation Error",
    //             this.Description,
    //             400,
    //             "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    //             null,
    //             this.Metadata
    //         ),
    //         ErrorType.Conflict => new ProblemDetails(
    //             "Conflict Error",
    //             this.Description,
    //             409,
    //             "https://tools.ietf.org/html/rfc7231#section-6.5.8",
    //             null,
    //             this.Metadata
    //         ),
    //         ErrorType.NotFound => new ProblemDetails(
    //             "Not Found Error",
    //             this.Description,
    //             404,
    //             "https://tools.ietf.org/html/rfc7231#section-6.5.4",
    //             null,
    //             this.Metadata
    //         ),
    //         ErrorType.Unauthorized => new ProblemDetails(
    //             "Unauthorized Error",
    //             this.Description,
    //             401,
    //             "https://tools.ietf.org/html/rfc7231#section-6.5.2",
    //             null,
    //             this.Metadata
    //         ),
    //         ErrorType.Forbidden => new ProblemDetails(
    //             "Forbidden Error",
    //             this.Description,
    //             403,
    //             "https://tools.ietf.org/html/rfc7231#section-6.5.3",
    //             null,
    //             this.Metadata
    //         ),
    //         ErrorType.InvalidOperation => new ProblemDetails(
    //             "Invalid Operation Error",
    //             this.Description,
    //             422,
    //             "https://tools.ietf.org/html/rfc4918#section-11.2",
    //             null,
    //             this.Metadata
    //         ),
    //         _ => throw new NotImplementedException(),
    //     };

    public override int GetHashCode() =>
        this.Metadata is null
            ? HashCode.Combine(this.Code, this.Description, this.Type, this.NumericType)
            : this.ComposeHashCode();

    private int ComposeHashCode()
    {
#pragma warning disable SA1129 // HashCode needs to be instantiated this way
        var hashCode = new HashCode();
#pragma warning restore SA1129

        hashCode.Add(this.Code);
        hashCode.Add(this.Description);
        hashCode.Add(this.Type);
        hashCode.Add(this.NumericType);

        foreach (var keyValuePair in this.Metadata!)
        {
            hashCode.Add(keyValuePair.Key);
            hashCode.Add(keyValuePair.Value);
        }

        return hashCode.ToHashCode();
    }

    private static bool CompareMetadata(
        IDictionary<string, object?> metadata,
        IDictionary<string, object?> otherMetadata
    )
    {
        if (ReferenceEquals(metadata, otherMetadata))
        {
            return true;
        }

        if (metadata.Count != otherMetadata.Count)
        {
            return false;
        }

        foreach (var keyValuePair in metadata)
        {
            if (
                !otherMetadata.TryGetValue(keyValuePair.Key, out var otherValue)
                || keyValuePair.Value != otherValue
            )
            {
                return false;
            }
        }

        return true;
    }
}

public enum ErrorType
{
    None,
    Failure,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    InvalidOperation,
}
