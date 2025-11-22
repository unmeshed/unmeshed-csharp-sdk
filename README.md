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

### Run AttributeWorker

```csharp
await client.RegisterWorkersAsync("Unmeshed.Sdk.Workers.Examples");   
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

## 🔗 Examples

Full working examples can be found below:

- **[Process Client Examples](https://github.com/unmeshed/unmeshed-csharp-sdk/blob/main/Unmeshed.Sdk.Examples/ProcessClientExample.cs)**

- **[Custom Worker Examples (Attribute-based and Non-Attribute)](https://github.com/unmeshed/unmeshed-csharp-sdk/blob/main/Unmeshed.Sdk.Examples/WorkerExample.cs)**

- **[Simple Worker Examples (Failed Worker, Rescheduled Worker, Completion Worker)](https://github.com/unmeshed/unmeshed-csharp-sdk/blob/main/Unmeshed.Sdk.Workers/Examples/SimpleWorkers.cs)**

## ▶️ Running the Process and Workers Examples Locally

Follow the steps below to build and run the sample projects locally.

### **1. Clone Unmeshed C# SDK Repository**

```bash
git clone https://github.com/unmeshed/unmeshed-csharp-sdk.git
```

### **2. Build the Solution**

```bash
cd unmeshed-csharp-sdk
dotnet build
```

### **3. Run the Worker Example**
```bash
cd unmeshed-csharp-sdk/Unmeshed.Sdk.Examples
dotnet run worker
```

### **4. Run the Process Example**
```bash
cd unmeshed-csharp-sdk/Unmeshed.Sdk.Examples
dotnet run process
```


## ▶️ Running the Standalone Workers Project Examples Locally

Follow the steps below to build and run the Workers Project Examples Locally.

### **1. Clone Unmeshed C# SDK Repository**

```bash
git clone https://github.com/unmeshed/unmeshed-csharp-sdk.git
```

### **2. Build the Solution**

```bash
cd unmeshed-csharp-sdk
dotnet build
```

### **2. Run the Project**
```bash
cd unmeshed-csharp-sdk/Unmeshed.Sdk.Examples
dotnet run --project Unmeshed.Sdk.Workers/csharp-workers.csproj
```
