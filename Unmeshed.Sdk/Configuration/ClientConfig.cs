namespace Unmeshed.Sdk.Configuration;

using System.Text.RegularExpressions;

/// <summary>
/// Configuration settings for the Unmeshed client.
/// </summary>
public class ClientConfig
{
    /// <summary>
    /// Gets or sets the default namespace for workers.
    /// </summary>
    public string DefaultNamespace { get; set; } = "default";

    /// <summary>
    /// Gets or sets the base URL of the Unmeshed engine.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost";

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the step timeout in milliseconds.
    /// </summary>
    public long StepTimeoutMillis { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the initial delay before polling in milliseconds.
    /// </summary>
    public long InitialDelayMillis { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the work request batch size for polling.
    /// </summary>
    public int WorkRequestBatchSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the fixed thread pool size.
    /// </summary>
    public int FixedThreadPoolSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the client identifier (required).
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the authentication token (required).
    /// </summary>
    public required string AuthToken { get; set; }

    /// <summary>
    /// Gets or sets the response submit batch size.
    /// </summary>
    public int ResponseSubmitBatchSize { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum number of submit attempts.
    /// </summary>
    public int MaxSubmitAttempts { get; set; } = 50;

    /// <summary>
    /// Gets or sets the list of permanent error keywords.
    /// </summary>
    public List<string> PermanentErrorKeywords { get; set; } = new()
    {
        "Invalid request, step is not in RUNNING state",
        "please poll the latest and update"
    };

    /// <summary>
    /// Gets or sets whether to enable batch processing for submit operations.
    /// When true, submissions are batched and processed asynchronously in the background.
    /// When false, submissions are sent immediately without batching.
    /// </summary>
    public bool EnableBatchProcessing { get; set; } = true;

    /// <summary>
    /// Gets the full server URL.
    /// </summary>
    /// <summary>
    /// Gets the full server URL.
    /// Logic matches Java SDK:
    /// 1. If BaseUrl starts with https, use it as is.
    /// 2. If BaseUrl already has a port (explicitly), use it as is.
    /// 3. Otherwise, append the configured Port.
    /// </summary>
    public string ServerUrl
    {
        get
        {
            var baseUrl = BaseUrl;
            if (baseUrl.EndsWith("/"))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
            }

            // If https, assume port is handled or default 443 is desired, don't append Port.
            if (baseUrl.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
            {
                return baseUrl;
            }

            // Check if port is explicitly present in the authority part.
            // Regex checks for :digits at the end of the string or before a slash.
            // We exclude the http:// part to avoid matching the scheme.
            // Pattern: ^https?://[^/]*:\d+($|/)
            if (Regex.IsMatch(baseUrl, @"^https?://[^/]*:\d+($|/)"))
            {
                return baseUrl;
            }

            return $"{baseUrl}:{Port}";
        }
    }

    /// <summary>
    /// Checks if credentials are properly configured.
    /// </summary>
    /// <returns>True if both ClientId and AuthToken are provided.</returns>
    public bool HasCredentials()
    {
        return !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(AuthToken);
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (!HasCredentials())
        {
            throw new InvalidOperationException(
                "Credentials not configured correctly. Auth client id and token are required.");
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("BaseUrl cannot be empty.");
        }

        if (Port <= 0 || Port > 65535)
        {
            throw new InvalidOperationException("Port must be between 1 and 65535.");
        }
    }
}
