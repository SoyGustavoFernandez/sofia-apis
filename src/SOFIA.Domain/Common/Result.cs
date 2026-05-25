using System.Diagnostics.CodeAnalysis;

namespace SOFIA.Domain.Common;

public class Result
{
    protected Result(bool isSuccess, Error error, int statusCode)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException();
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public int StatusCode { get; }

    public static Result Success(int statusCode = 200) => new(true, Error.None, statusCode);
    public static Result<TValue> Success<TValue>(TValue value, int statusCode = 200) => new(value, true, Error.None, statusCode);

    public static Result Failure(Error error, int statusCode = 400) => new(false, error, statusCode);
    public static Result<TValue> Failure<TValue>(Error error, int statusCode = 400) => new(default!, false, error, statusCode);
}

public class Result<TValue> : Result
{
    private readonly TValue? _internalValue;

    protected internal Result(TValue? value, bool isSuccess, Error error, int statusCode)
        : base(isSuccess, error, statusCode) =>
        _internalValue = value;

    /// <summary>
    /// Indicates whether this result represents a successful operation.
    /// When <see langword="true"/>, <see cref="Value"/> is guaranteed to be non-null.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public new bool IsSuccess => base.IsSuccess;

    /// <summary>Gets the value. Only accessible when <see cref="IsSuccess"/> is <see langword="true"/>.</summary>
    public TValue? Value => GetValue();

    private TValue? GetValue() => IsSuccess
        ? _internalValue
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
