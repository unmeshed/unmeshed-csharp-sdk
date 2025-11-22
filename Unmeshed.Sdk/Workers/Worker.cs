using System.Reflection;

namespace Unmeshed.Sdk.Workers;

/// <summary>
/// Represents a registered worker.
/// </summary>
public class Worker
{
    /// <summary>The method to execute (reflection).</summary>
    public MethodInfo? Method { get; set; }
    /// <summary>The function to execute (delegate).</summary>
    public Func<Dictionary<string, object>, Task<object>>? Function { get; set; }
    /// <summary>The name of the worker.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The namespace of the worker.</summary>
    public string Namespace { get; set; } = "default";
    /// <summary>Maximum number of concurrent executions.</summary>
    public int MaxInProgress { get; set; } = 10;
    /// <summary>Whether to use IO-bound thread pool.</summary>
    public bool IoThread { get; set; }
    /// <summary>The attribute associated with the worker (optional).</summary>
    public WorkerFunctionAttribute? WorkerFunction { get; set; }
    /// <summary>The instance of the class containing the worker method.</summary>
    public object? Instance { get; set; }

    /// <summary>
    /// Gets the formatted worker ID.
    /// </summary>
    public string FormattedId => $"{Namespace}:-#-:{Name}";
}
