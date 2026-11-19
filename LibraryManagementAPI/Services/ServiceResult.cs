namespace LibraryManagementAPI.Services;

public enum ServiceError
{
    None,
    NotFound,
    Conflict,
    Validation,
    Forbidden
}

/// <summary>Outcome of a business operation: a value, or an error kind plus a message for the caller.</summary>
public class ServiceResult<T>
{
    public bool Succeeded => Error == ServiceError.None;
    public T? Value { get; private init; }
    public ServiceError Error { get; private init; }
    public string Message { get; private init; } = string.Empty;

    public static ServiceResult<T> Ok(T value, string message = "Success") => new() { Value = value, Message = message };
    public static ServiceResult<T> Fail(ServiceError error, string message) => new() { Error = error, Message = message };
}
