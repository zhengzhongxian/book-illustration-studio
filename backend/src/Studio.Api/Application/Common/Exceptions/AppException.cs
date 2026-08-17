using System.Net;

namespace Studio.Api.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public List<string>? Errors { get; }

    protected AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.", HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string message)
        : base(message, HttpStatusCode.NotFound)
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, HttpStatusCode.Conflict)
    {
    }
}

public class ValidationException : AppException
{
    public ValidationException(string message, List<string>? errors = null)
        : base(message, HttpStatusCode.BadRequest, errors)
    {
    }
}

public class GeminiApiException : AppException
{
    public GeminiApiException(string message, HttpStatusCode statusCode = HttpStatusCode.BadGateway)
        : base(message, statusCode)
    {
    }
}
