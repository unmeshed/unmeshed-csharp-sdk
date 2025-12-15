namespace Unmeshed.Sdk;

using Unmeshed.Sdk.Schedulers;
using Unmeshed.Sdk.Models;

public class WorkContext
{
    /// <summary>The method to get CurrentWorkRequest.</summary>
    public static WorkRequest CurrentWorkRequest()
    {
        WorkRequest workRequest = WorkScheduler.CurrentWorkRequest.Value;
        return workRequest;
    }
}