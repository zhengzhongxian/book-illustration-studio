using System.Net;

namespace Studio.Api.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; } = (int)HttpStatusCode.OK;
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null, int statusCode = (int)HttpStatusCode.OK)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = (int)HttpStatusCode.BadRequest)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Data = default,
            Message = message,
            Errors = errors ?? (string.IsNullOrEmpty(message) ? null : new List<string> { message })
        };
    }
}

public static class ApiResponse
{
    public static ApiResponse<object> Ok(string? message = null, int statusCode = (int)HttpStatusCode.OK)
    {
        return new ApiResponse<object>
        {
            Success = true,
            StatusCode = statusCode,
            Data = null,
            Message = message
        };
    }

    public static ApiResponse<object> Fail(string message, List<string>? errors = null, int statusCode = (int)HttpStatusCode.BadRequest)
    {
        return new ApiResponse<object>
        {
            Success = false,
            StatusCode = statusCode,
            Data = null,
            Message = message,
            Errors = errors ?? (string.IsNullOrEmpty(message) ? null : new List<string> { message })
        };
    }
}
