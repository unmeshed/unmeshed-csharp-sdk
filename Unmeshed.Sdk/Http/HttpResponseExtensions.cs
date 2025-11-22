using System.Net.Http;
using Unmeshed.Sdk.Exceptions;

namespace Unmeshed.Sdk.Http;

/// <summary>
/// Extension methods for HttpResponseMessage.
/// </summary>
public static class HttpResponseExtensions
{
    /// <summary>
    /// Ensures the response status code is successful, otherwise throws an UnmeshedApiException with details.
    /// </summary>
    public static async Task EnsureSuccessStatusCodeWithContentAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var message = $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Content: {content}";

        throw new UnmeshedApiException(response.StatusCode, content, message);
    }
}
