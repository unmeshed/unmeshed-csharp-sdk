namespace Unmeshed.Sdk.Examples;

/// <summary>
/// Main program entry point for running examples.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        var exampleName = args[0].ToLowerInvariant();

        try
        {
            switch (exampleName)
            {
                case "process":
                case "processclient":
                    Console.WriteLine("Running ProcessClient Example...\n");
                    await ProcessClientExample.Main(args.Skip(1).ToArray());
                    break;

                case "worker":
                case "workers":
                    Console.WriteLine("Running Worker Example...\n");
                    await WorkerExample.Main(args.Skip(1).ToArray());
                    break;

                default:
                    Console.WriteLine($"Unknown example: {exampleName}\n");
                    PrintUsage();
                    Environment.Exit(1);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nExample failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════════════╗
║              Unmeshed C# SDK Examples                                ║
╚══════════════════════════════════════════════════════════════════════╝

Usage: dotnet run --project Unmeshed.Sdk.Examples <example-name>

Available Examples:
  
  process, processclient
      Comprehensive ProcessClient demonstration showing:
      - Process Definition Management (CRUD)
      - Process Execution (Sync/Async/Rerun)
      - Process Data Retrieval
      - Bulk Process Actions (Terminate/Resume/Reviewed)
      - Process Manipulation
      - Process Search
      - API Mapping (GET/POST)

  worker, workers
      Worker registration and execution demonstration showing:
      - Scanning and registering workers from namespace
      - Programmatic worker function registration
      - Starting client to poll and execute work
      - Graceful shutdown handling

Environment Variables (Required):
  
  UNMESHED_AUTH_ID          Your authentication client ID
  UNMESHED_AUTH_TOKEN       Your authentication token
  UNMESHED_ENGINE_HOST      Engine host URL (default: http://localhost)
  UNMESHED_ENGINE_PORT      Engine port (default: 8080)

Examples:
  
  # Run ProcessClient example
  dotnet run --project Unmeshed.Sdk.Examples process

  # Run Worker example
  dotnet run --project Unmeshed.Sdk.Examples worker

  # Set environment variables and run
  export UNMESHED_AUTH_ID=""your-id""
  export UNMESHED_AUTH_TOKEN=""your-token""
  dotnet run --project Unmeshed.Sdk.Examples process

For more information, see: Unmeshed.Sdk.Examples/README.md
");
    }
}
