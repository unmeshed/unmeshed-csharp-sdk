using System.Net;

namespace Unmeshed.Sdk.Exceptions;

/// <summary>
/// Exception thrown when the Unmeshed API returns an error response.
/// </summary>
public class UnmeshedApiException : Exception
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the error content returned by the API.
    /// </summary>
    public string? ErrorContent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmeshedApiException"/> class.
    /// </summary>
    public UnmeshedApiException(HttpStatusCode statusCode, string? errorContent, string message) 
        : base(message)
    {
        StatusCode = statusCode;
        ErrorContent = errorContent;
    }
}
