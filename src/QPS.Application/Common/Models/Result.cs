namespace QPS.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Message { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private Result() { }

    public static Result<T> Ok(T value, string? message = null)
        => new() { IsSuccess = true, Value = value, Message = message };

    public static Result<T> Fail(string error)
        => new() { IsSuccess = false, Errors = new List<string> { error } };

    public static Result<T> Fail(List<string> errors)
        => new() { IsSuccess = false, Errors = errors };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Message { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private Result() { }

    public static Result Ok(string? message = null)
        => new() { IsSuccess = true, Message = message };

    public static Result Fail(string error)
        => new() { IsSuccess = false, Errors = new List<string> { error } };
}