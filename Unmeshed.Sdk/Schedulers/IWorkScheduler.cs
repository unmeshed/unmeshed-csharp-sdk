using Unmeshed.Sdk.Models;
using Unmeshed.Sdk.Workers;

namespace Unmeshed.Sdk.Schedulers;

/// <summary>
/// Interface for scheduling work execution.
/// </summary>
public interface IWorkScheduler
{
    /// <summary>
    /// Schedules a work request for execution.
    /// </summary>
    Task<StepResult> ScheduleAsync(WorkRequest workRequest, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a worker to the scheduler.
    /// </summary>
    void AddWorker(string name, Worker worker);
}
