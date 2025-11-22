using Microsoft.Extensions.Logging;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Configuration;
using System.Runtime.InteropServices;

namespace Unmeshed.Sdk.Workers;

/// <summary>
/// Main entry point for the Unmeshed SDK Workers example application.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Unmeshed SDK Workers (C#)");
        Console.WriteLine("===================================");

        // Configure logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(GetLogLevel());
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            // Load configuration from environment variables
            var config = LoadConfiguration();

            logger.LogInformation("Connecting to Unmeshed engine at {Url}", config.ServerUrl);
            logger.LogInformation("Client ID: {ClientId}", config.ClientId);

            // Create the Unmeshed client
            using var client = new UnmeshedClient(config, loggerFactory);

            // Register workers based on OS
            await RegisterWorkersAsync(client, logger);

            // Start the client (begins polling)
            logger.LogInformation("Starting client...");
            await client.StartAsync();

            logger.LogInformation("SDK workers started successfully. Press Ctrl+C to stop.");

            // Wait for cancellation
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                logger.LogInformation("Shutdown requested...");
            };

            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Application stopped");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error: {Message}", ex.Message);
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Loads configuration from environment variables.
    /// </summary>
    private static ClientConfig LoadConfiguration()
    {
        var clientId = GetEnvironmentVariable("UNMESHED_AUTH_ID", "");
        var authToken = GetEnvironmentVariable("UNMESHED_AUTH_TOKEN", "");
        var baseUrl = GetEnvironmentVariable("UNMESHED_ENGINE_URL", "http://localhost");
        var port = int.Parse(GetEnvironmentVariable("UNMESHED_ENGINE_PORT", "8080"));
        var batchSize = int.Parse(GetEnvironmentVariable("UNMESHED_WORK_BATCH_SIZE", "200"));
        var responseBatchSize = int.Parse(GetEnvironmentVariable("UNMESHED_WORK_RESPONSE_BATCH_SIZE", "50"));
        var maxSubmitAttempts = int.Parse(GetEnvironmentVariable("UNMESHED_MAX_SUBMIT_ATTEMPTS", "100"));
        var connectionTimeoutSeconds = int.Parse(GetEnvironmentVariable("UNMESHED_CONNECTION_TIMEOUT_SECONDS", "60"));
        var fixedThreadPoolSize = int.Parse(GetEnvironmentVariable("UNMESHED_FIXED_THREAD_POOL_SIZE", "2"));
        var enableBatchProcessing = bool.Parse(GetEnvironmentVariable("UNMESHED_ENABLE_BATCH_PROCESSING", "true"));

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(authToken))
        {
            throw new InvalidOperationException(
                "Required environment variables not set:\n" +
                "  * UNMESHED_AUTH_ID\n" +
                "  * UNMESHED_AUTH_TOKEN\n" +
                "  * UNMESHED_ENGINE_URL");
        }

        return new ClientConfig
        {
            ClientId = clientId,
            AuthToken = authToken,
            BaseUrl = baseUrl,
            Port = port,
            WorkRequestBatchSize = batchSize,
            ResponseSubmitBatchSize = responseBatchSize,
            MaxSubmitAttempts = maxSubmitAttempts,
            ConnectionTimeoutSeconds = connectionTimeoutSeconds,
            FixedThreadPoolSize = fixedThreadPoolSize,
            EnableBatchProcessing = enableBatchProcessing,
            InitialDelayMillis = 20,
            StepTimeoutMillis = 1000L * 60 * 60 * 24 * 365 // 1 year (effectively no timeout)
        };
    }

    /// <summary>
    /// Registers workers based on the operating system.
    /// </summary>
    private static async Task RegisterWorkersAsync(UnmeshedClient client, ILogger logger)
    {
        // Register workers using attribute scanning
        logger.LogInformation("Scanning for workers in namespace: Unmeshed.Sdk.Workers.Examples");
        await client.RegisterWorkersAsync("Unmeshed.Sdk.Workers.Examples");

        // Optionally register additional workers programmatically
        var customWorkerNamespace = GetEnvironmentVariable("UNMESHED_CUSTOM_WORKERS", "");
        if (!string.IsNullOrWhiteSpace(customWorkerNamespace))
        {
            logger.LogInformation("Registering custom workers from: {Namespace}", customWorkerNamespace);
            await client.RegisterWorkersAsync(customWorkerNamespace);
        }

        // Register a simple programmatic worker as an example
        await client.RegisterWorkerFunctionAsync(
            workerFunction: async (input) =>
            {
                await Task.Delay(100); // Simulate work
                return new
                {
                    message = "Hello from programmatic worker!",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    input = input
                };
            },
            @namespace: "default",
            name: "hello_world",
            maxInProgress: 10,
            ioThread: true
        );

        logger.LogInformation("All workers registered successfully");
    }

    /// <summary>
    /// Gets an environment variable with a default value.
    /// </summary>
    private static string GetEnvironmentVariable(string name, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(name) ?? defaultValue;
    }

    /// <summary>
    /// Gets the log level from environment variable.
    /// </summary>
    private static LogLevel GetLogLevel()
    {
        var logLevel = GetEnvironmentVariable("LOG_LEVEL", "Information");
        return Enum.TryParse<LogLevel>(logLevel, true, out var level) 
            ? level 
            : LogLevel.Information;
    }
}
