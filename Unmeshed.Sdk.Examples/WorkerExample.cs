using Microsoft.Extensions.Logging;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Examples;

/// <summary>
/// Demonstrates worker registration and execution with the Unmeshed SDK.
/// Shows both namespace scanning and programmatic worker registration.
/// </summary>
public class WorkerExample
{
    private static string GetEnvOrDefault(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    public static async Task Main(string[] args)
    {
        // Configure logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<WorkerExample>();

        // Get configuration from environment variables
        var authId = GetEnvOrDefault("UNMESHED_AUTH_ID", "your-auth-id");
        var authToken = GetEnvOrDefault("UNMESHED_AUTH_TOKEN", "your-auth-token");
        var baseUrl = GetEnvOrDefault("UNMESHED_ENGINE_HOST", "http://localhost");
        var port = int.Parse(GetEnvOrDefault("UNMESHED_ENGINE_PORT", "8080"));

        if (string.IsNullOrWhiteSpace(authId) || string.IsNullOrWhiteSpace(authToken))
        {
            logger.LogError(@"
Required parameters have not been provided. Please ensure you have the following environment variables set:
  * UNMESHED_AUTH_ID
  * UNMESHED_AUTH_TOKEN
  * UNMESHED_ENGINE_HOST (optional, defaults to http://localhost)
  * UNMESHED_ENGINE_PORT (optional, defaults to 8080)");
            Environment.Exit(-1);
        }

        // Create client configuration
        var config = new ClientConfig
        {
            ClientId = authId,
            AuthToken = authToken,
            BaseUrl = baseUrl,
            Port = port,
            WorkRequestBatchSize = 100,
            ResponseSubmitBatchSize = 500,
            StepTimeoutMillis = 30000,
            EnableResultsSubmission = true // Enable batch processing for workers
        };

        // Create Unmeshed client
        using var client = new UnmeshedClient(config, loggerFactory);

        logger.LogInformation("=== Unmeshed C# SDK Worker Examples ===\n");

        try
        {
            // Example 1: Register workers by scanning namespace
            logger.LogInformation("--- Example 1: Register Workers by Namespace Scanning ---");
            await client.RegisterWorkersAsync("Unmeshed.Sdk.Workers");
            logger.LogInformation("Workers registered from namespace scan\n");

            // Example 2: Register a worker function programmatically
            logger.LogInformation("--- Example 2: Register Worker Function Programmatically ---");
            await client.RegisterWorkerFunctionAsync(
                workerFunction: async (input) =>
                {
                    logger.LogInformation("Custom worker executing with input: {Input}", 
                        string.Join(", ", input.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                    
                    // Simulate some work
                    await Task.Delay(100);
                    
                    // Return result
                    return new Dictionary<string, object>
                    {
                        { "status", "success" },
                        { "processedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                        { "inputCount", input.Count }
                    };
                },
                @namespace: "default",
                name: "custom_worker",
                maxInProgress: 10,
                ioThread: false
            );


            await client.RegisterWorkerFunctionAsync(
                workerFunction: async (input) =>
                {
                    logger.LogInformation("Custom worker executing with input: {Input}", 
                        string.Join(", ", input.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                    
                    // Simulate some work
                    await Task.Delay(100);
                    
                    // Return result
                    return new Dictionary<string, object>
                    {
                        { "status", "success" },
                        { "processedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                        { "inputCount", input.Count }
                    };
                },
                @namespace: "ns1",
                name: "custom_worker",
                maxInProgress: 10,
                ioThread: true
            );
            logger.LogInformation("Custom worker function registered\n");

            // Example 3: Start the client and begin polling for work
            logger.LogInformation("--- Example 3: Start Client and Poll for Work ---");
            await client.StartAsync();
            logger.LogInformation("Client started and polling for work...");
            logger.LogInformation("Press Ctrl+C to stop\n");

            // Keep the application running
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                logger.LogInformation("\nShutdown requested...");
            };

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected when Ctrl+C is pressed
            }

            // Stop the client
            await client.StopAsync();
            logger.LogInformation("Client stopped successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running worker example");
            Environment.Exit(-1);
        }
    }
}
