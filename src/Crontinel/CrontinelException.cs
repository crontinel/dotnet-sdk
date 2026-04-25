using System;

namespace Crontinel;

/// <summary>
/// Exception thrown when the Crontinel API returns a non-success response.
/// </summary>
public class CrontinelException : Exception
{
    /// <summary>
    /// The HTTP status code returned by the API.
    /// </summary>
    public int StatusCode { get; }

    public CrontinelException(int statusCode, string message)
        : base($"Crontinel API error ({statusCode}): {message}")
    {
        StatusCode = statusCode;
    }

    public CrontinelException(string message)
        : base(message)
    {
        StatusCode = 0;
    }
}
