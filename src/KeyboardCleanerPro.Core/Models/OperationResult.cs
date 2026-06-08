namespace KeyboardCleanerPro.Core.Models;

/// <summary>
/// Discriminated union result type — used by all service methods instead of throwing exceptions
/// for expected failure paths. Unhandled exceptions still propagate normally.
/// </summary>
public sealed class OperationResult
{
    public bool       IsSuccess    { get; }
    public string?    ErrorMessage { get; }
    public int?       ErrorCode    { get; }
    public Exception? Exception    { get; }

    private OperationResult(bool isSuccess, string? errorMessage, int? errorCode, Exception? exception)
    {
        IsSuccess    = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode    = errorCode;
        Exception    = exception;
    }

    public static OperationResult Success() =>
        new(true, null, null, null);

    public static OperationResult Failure(string errorMessage, int? errorCode = null, Exception? exception = null) =>
        new(false, errorMessage, errorCode, exception);

    public override string ToString() =>
        IsSuccess
            ? "Success"
            : $"Failure: {ErrorMessage}" + (ErrorCode.HasValue ? $" (Win32={ErrorCode})" : string.Empty);
}

/// <summary>
/// Generic variant of <see cref="OperationResult"/> carrying a typed value on success.
/// </summary>
public sealed class OperationResult<T>
{
    public bool       IsSuccess    { get; }
    public T?         Value        { get; }
    public string?    ErrorMessage { get; }
    public int?       ErrorCode    { get; }
    public Exception? Exception    { get; }

    private OperationResult(bool isSuccess, T? value, string? errorMessage, int? errorCode, Exception? exception)
    {
        IsSuccess    = isSuccess;
        Value        = value;
        ErrorMessage = errorMessage;
        ErrorCode    = errorCode;
        Exception    = exception;
    }

    public static OperationResult<T> Success(T value) =>
        new(true, value, null, null, null);

    public static OperationResult<T> Failure(string errorMessage, int? errorCode = null, Exception? exception = null) =>
        new(false, default, errorMessage, errorCode, exception);

    /// <summary>Convenience: propagate a non-generic failure into the generic form.</summary>
    public static OperationResult<T> FromFailure(OperationResult other) =>
        Failure(other.ErrorMessage!, other.ErrorCode, other.Exception);

    public override string ToString() =>
        IsSuccess
            ? $"Success({Value})"
            : $"Failure: {ErrorMessage}" + (ErrorCode.HasValue ? $" (Win32={ErrorCode})" : string.Empty);
}
