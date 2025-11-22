using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Unmeshed.Sdk.Configuration;

namespace Unmeshed.Sdk.Http;

/// <summary>
/// Factory for creating HTTP clients.
/// </summary>
public interface IHttpClientFactory
{
    /// <summary>
    /// Creates a configured HttpClient.
    /// </summary>
    HttpClient CreateClient();
}

/// <summary>
/// Factory for creating authenticated HttpClients.
/// </summary>
public class HttpClientFactory : IHttpClientFactory
{
    private readonly ClientConfig _config;
    private readonly ILogger<HttpClientFactory> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientFactory"/> class.
    /// </summary>
    public HttpClientFactory(ClientConfig config, ILoggerFactory loggerFactory)
    {
        _config = config;
        _logger = loggerFactory.CreateLogger<HttpClientFactory>();
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.ServerUrl),
            Timeout = TimeSpan.FromSeconds(_config.ConnectionTimeoutSeconds)
        };

        // Create Bearer token in the format: Bearer client.sdk.{clientId}.{sha256Hash}
        var bearerToken = CreateBearerToken(_config.ClientId, _config.AuthToken);
        
        // Add authorization header
        _httpClient.DefaultRequestHeaders.Add("Authorization", bearerToken);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _logger.LogDebug("HTTP client created for {BaseUrl}", _config.ServerUrl);
    }

    /// <summary>
    /// Creates a Bearer token in the format required by Unmeshed engine.
    /// </summary>
    private static string CreateBearerToken(string clientId, string authToken)
    {
        var hash = CreateSecureHash(authToken);
        return $"Bearer client.sdk.{clientId}.{hash}";
    }

    /// <summary>
    /// Creates a SHA-256 hash of the input string.
    /// </summary>
    private static string CreateSecureHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        
        var hexString = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            hexString.Append(b.ToString("x2"));
        }
        
        return hexString.ToString();
    }

    /// <inheritdoc />
    public HttpClient CreateClient()
    {
        return _httpClient;
    }
}
