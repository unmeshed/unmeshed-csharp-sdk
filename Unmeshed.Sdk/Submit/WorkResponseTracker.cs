using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Submit;

/// <summary>
/// Tracks work response submission attempts.
/// </summary>
internal class WorkResponseTracker
{
    public required WorkResponse Response { get; set; }
    public SemaphoreSlim? Semaphore { get; set; }
    public int AttemptCount { get; set; }
    public long EnqueuedAt { get; set; }
}
