using System.Text.Json.Serialization;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Workers;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Workers.Examples;

/// <summary>
/// Request model for simple echo worker.
/// </summary>
public class EchoRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("delayMs")]
    public int DelayMs { get; set; } = 0;
}

/// <summary>
/// Response model for echo worker.
/// </summary>
public class EchoResponse
{
    [JsonPropertyName("echo")]
    public string Echo { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("processedBy")]
    public string ProcessedBy { get; set; } = string.Empty;
}

/// <summary>
/// Simple echo worker for testing purposes.
/// </summary>
public class EchoWorker
{
    [WorkerFunction(Name = "echo", Namespace = "default", MaxInProgress = 100, IoThread = true)]
    public async Task<EchoResponse> EchoMessageAsync(Dictionary<string, object> input)
    {
        var request = System.Text.Json.JsonSerializer.Deserialize<EchoRequest>(
            System.Text.Json.JsonSerializer.Serialize(input));

        if (request == null)
        {
            throw new ArgumentException("Invalid request");
        }

        // Simulate processing delay if specified
         var currentWorkRequest = WorkContext.CurrentWorkRequest();
         if (currentWorkRequest != null)
         {
            Console.WriteLine($"[EchoWorker] Executing step {currentWorkRequest.StepName} (ID: {currentWorkRequest.StepId})");
         }
        if (request.DelayMs > 0)
        {
            await Task.Delay(request.DelayMs);
        }

        return new EchoResponse
        {
            Echo = request.Message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ProcessedBy = Environment.MachineName
        };
    }
}

/// <summary>
/// Worker that performs mathematical calculations.
/// </summary>
public class CalculatorWorker
{
    private static int _attempt = 0;

    [WorkerFunction(Name = "calculate", Namespace = "default", MaxInProgress = 50)]
    public async Task<object> CalculateAsync(Dictionary<string, object> input)
    {
        // Helper to get string value from input (handles JsonElement)
        string? GetStringValue(string key)
        {
            if (!input.TryGetValue(key, out var value))
                return null;
            
            if (value is string str)
                return str;
            
            if (value is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                return jsonElement.GetString();
            
            return value?.ToString();
        }

        // Helper to get double value from input (handles JsonElement)
        double? GetDoubleValue(string key)
        {
            if (!input.TryGetValue(key, out var value))
                return null;
            
            if (value is double d)
                return d;
            
            if (value is int i)
                return i;
            
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                    return jsonElement.GetDouble();
            }
            
            if (double.TryParse(value?.ToString(), out var parsed))
                return parsed;
            
            return null;
        }

        var operation = GetStringValue("operation");
        if (string.IsNullOrEmpty(operation))
        {
            throw new ArgumentException("Operation is required");
        }

        var a = GetDoubleValue("a");
        if (!a.HasValue)
        {
            throw new ArgumentException("Parameter 'a' must be a number");
        }

        var b = GetDoubleValue("b");
        if (!b.HasValue)
        {
            throw new ArgumentException("Parameter 'b' must be a number");
        }

        double result = operation.ToLower() switch
        {
            "add" => a.Value + b.Value,
            "subtract" => a.Value - b.Value,
            "multiply" => a.Value * b.Value,
            "divide" => b.Value != 0 ? a.Value / b.Value : throw new DivideByZeroException("Cannot divide by zero"),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };

        await Task.CompletedTask;

        return new
        {
            operation,
            a = a.Value,
            b = b.Value,
            result,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// A worker that intentionally fails to test error handling.
    /// </summary>
    [WorkerFunction(Name = "fail", Namespace = "default", WorkStepNames = new[] { "step1", "step2" })]
    public Task FailAsync(Dictionary<string, object> input)
    {
        string message = "This is a deliberate failure.";
        
        if (input.TryGetValue("message", out var msgObj) && msgObj != null)
        {
            message = msgObj.ToString() ?? message;
        }

        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Tests returning a primitive value.
    /// </summary>
    [WorkerFunction(Name = "return_primitive", Namespace = "default")]
    public string ReturnPrimitive(Dictionary<string, object> input)
    {
        return "Hello from primitive worker!";
    }

    /// <summary>
    /// Tests returning a dictionary/map.
    /// </summary>
    [WorkerFunction(Name = "return_map", Namespace = "default")]
    public Dictionary<string, object> ReturnMap(Dictionary<string, object> input)
    {
        return new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 },
            { "nested", new { foo = "bar" } }
        };
    }

    /// <summary>
    /// Tests returning a list.
    /// </summary>
    [WorkerFunction(Name = "return_list", Namespace = "default")]
    public List<string> ReturnList(Dictionary<string, object> input)
    {
        return new List<string> { "item1", "item2", "item3" };
    }

    /// <summary>
    /// Tests rescheduling a worker.
    /// </summary>
    [WorkerFunction(Name = "reschedule", Namespace = "default")]
    public StepResult RescheduleAsync(Dictionary<string, object> input)
    {
        // Increase BEFORE sending response
        _attempt++;

        Console.WriteLine($"[RescheduleWorker] Global Attempt = {_attempt}");

        if (_attempt <= 3)
        {
            return new StepResult
            {
                Output = new Dictionary<string, object>
                {
                    { "attempt", _attempt },
                    { "message", $"Rescheduling attempt {_attempt}" }
                },
                Status = UnmeshedConstants.StepStatus.Running,
                RescheduleAfterSeconds = 5
            };
        }

        // Complete
        return new StepResult
        {
            Output = new Dictionary<string, object>
            {
                { "attempt", _attempt },
                { "message", "Completed after global increments" }
            },
            Status = UnmeshedConstants.StepStatus.Completed
        };
    }
}
