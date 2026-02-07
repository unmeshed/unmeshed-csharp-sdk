using System.Reflection;

namespace Unmeshed.Sdk.Workers;

/// <summary>
/// Scans assemblies for worker functions.
/// </summary>
public static class WorkerScanner
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Configures a service provider used to resolve worker instances.
    /// </summary>
    public static void ConfigureServiceProvider(IServiceProvider? serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Finds all workers in the specified namespace.
    /// </summary>
    /// <param name="namespacePath">The namespace to scan for workers.</param>
    /// <returns>List of discovered workers.</returns>
    public static List<Worker> FindWorkers(string namespacePath)
    {
        var workers = new List<Worker>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Namespace != null && t.Namespace.StartsWith(namespacePath));

                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                    foreach (var method in methods)
                    {
                        var attr = method.GetCustomAttribute<WorkerFunctionAttribute>();
                        if (attr != null)
                        {
                            object? instance = null;
                            // Create instance if method is not static
                            if (!method.IsStatic)
                            {
                                instance = _serviceProvider?.GetService(type);
                                if (instance == null)
                                {
                                    try
                                    {
                                        instance = Activator.CreateInstance(type);
                                    }
                                    catch
                                    {
                                        // Allow deferred resolution in scheduler (for DI-only constructors).
                                        instance = null;
                                    }
                                }
                            }

                            foreach (var stepName in attr.WorkStepNames.Distinct())
                            {
                                 var w = new Worker
                                  {
                                        Method = method,
                                        Name = stepName,
                                        Namespace = attr.Namespace,
                                        MaxInProgress = attr.MaxInProgress,
                                        IoThread = attr.IoThread,
                                        WorkerFunction = attr,
                                        Instance = instance
                                  };
                                  workers.Add(w);
                            }

                            var worker = new Worker
                            {
                                Method = method,
                                Name = attr.Name,
                                Namespace = attr.Namespace,
                                MaxInProgress = attr.MaxInProgress,
                                IoThread = attr.IoThread,
                                WorkerFunction = attr,
                                Instance = instance
                            };

                            workers.Add(worker);
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        return workers;
    }
}
