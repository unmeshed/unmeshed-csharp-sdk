using System.Text.Json.Serialization;
using Unmeshed.Sdk.Workers;

namespace Unmeshed.Sdk.Workers.Examples;

public interface IGreetingProvider
{
    string GetGreeting();
}

public class GreetingProvider : IGreetingProvider
{
    public string GetGreeting() => "Hello from ASP.NET Core DI";
}

public class DiWorkerRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "developer";
}

public class DiWorker
{
    private readonly IGreetingProvider _greetingProvider;

    public DiWorker(IGreetingProvider greetingProvider)
    {
        _greetingProvider = greetingProvider;
    }

    [WorkerFunction(Name = "di_greeting", Namespace = "default", MaxInProgress = 50, IoThread = true)]
    public Dictionary<string, object> Run(Dictionary<string, object> input)
    {
        var request = System.Text.Json.JsonSerializer.Deserialize<DiWorkerRequest>(
            System.Text.Json.JsonSerializer.Serialize(input)) ?? new DiWorkerRequest();

        return new Dictionary<string, object>
        {
            ["message"] = $"{_greetingProvider.GetGreeting()}, {request.Name}!",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}
