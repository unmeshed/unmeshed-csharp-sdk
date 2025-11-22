# Unmeshed C# SDK

The unmeshed-csharp-sdk repository provides the client SDKs to build workers in C#.

Building the workers in C# mainly consists of the following steps:

1. Setup unmeshed-csharp-sdk package
2. Create and run workers
3. Create process using code
4. API Documentation

## Prerequisites

*   **.NET 8.0** or higher

## Setup Unmeshed C# Package

```bash
dotnet add package unmeshed-csharp-sdk
```

## Configurations

### Authentication Settings (Optional)

Configure the authentication settings if your Unmeshed server requires authentication.

*   `ClientId`: Key for authentication.
*   `AuthToken`: Secret for the key.

### Configure API Client

```csharp
using Unmeshed.Sdk;
using Unmeshed.Sdk.Configuration;
using Microsoft.Extensions.Logging;

var config = new ClientConfig
{
    BaseUrl = "http://localhost",
    Port = 8080,
    ClientId = "your-client-id",
    AuthToken = "your-auth-token"
};

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var client = new UnmeshedClient(config, loggerFactory);

// Start the client to poll for work
await client.StartAsync();
```

### Create and Run Workers

```csharp
await client.RegisterWorkerFunctionAsync(
    workerFunction: async (input) => 
    {
        return new { result = "success" };
    },
    namespace: "default",
    name: "my-worker"
);
```

### Create Process using Code

```csharp
await client.RunProcessAsyncAsync(new ProcessRequestData
{
    Namespace = "default",
    Name = "my-process",
    Input = new Dictionary<string, object>()
});
```