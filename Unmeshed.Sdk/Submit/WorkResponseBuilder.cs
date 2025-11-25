using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Submit;

/// <summary>
/// Builds work responses from work requests and results.
/// </summary>
public class WorkResponseBuilder
{
    /// <summary>
    /// Creates a success response.
    /// </summary>
    public WorkResponse SuccessResponse(WorkRequest request, StepResult result)
    {
        return new WorkResponse
        {
            StepId = request.StepId,
            ProcessId = request.ProcessId,
            StepExecutionId = request.StepExecutionId,
            RunCount = request.RunCount,
            Status = UnmeshedConstants.StepStatus.Completed,
            Output = result.Output,
            StartedAt = result.StartedAt
        };
    }

    /// <summary>
    /// Creates a running response (for long-running tasks).
    /// </summary>
    public WorkResponse RunningResponse(WorkRequest request, StepResult result)
    {
        return new WorkResponse
        {
            StepId = request.StepId,
            ProcessId = request.ProcessId,
            StepExecutionId = request.StepExecutionId,
            RunCount = request.RunCount,
            Status = UnmeshedConstants.StepStatus.Running,
            Output = result.Output,
            RescheduleAfterSeconds = result.RescheduleAfterSeconds,
            StartedAt = result.StartedAt
        };
    }

    /// <summary>
    /// Creates a failure response.
    /// </summary>
    public WorkResponse FailResponse(WorkRequest request, Exception exception, ClientConfig config)
    {
        var errorMessage = exception.Message;

        // Truncate if too long
        if (errorMessage.Length > 1000)
        {
            errorMessage = errorMessage.Substring(0, 1000) + "... (truncated)";
        }

        var output = new Dictionary<string, object>
        {
            { "error", errorMessage }
        };

        return new WorkResponse
        {
            StepId = request.StepId,
            ProcessId = request.ProcessId,
            StepExecutionId = request.StepExecutionId,
            RunCount = request.RunCount,
            Status = UnmeshedConstants.StepStatus.Failed,
            Output = output,
            StartedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}
