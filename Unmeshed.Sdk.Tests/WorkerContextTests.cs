using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unmeshed.Sdk.Models;
using Unmeshed.Sdk.Schedulers;
using Xunit;

namespace Unmeshed.Sdk.Tests
{
    public class WorkerContextTests
    {
        [Fact]
        public async Task TestAsyncLocalContextIsolation()
        {
            int numberOfWorkers = 20;
            var tasks = new List<Task>();
            var errors = new System.Collections.Concurrent.ConcurrentBag<string>();

            for (int i = 0; i < numberOfWorkers; i++)
            {
                int workerId = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Create a unique request for this "worker"
                    var request = new WorkRequest 
                    { 
                        StepName = $"Step-{workerId}",
                        StepId = workerId
                    };

                    // Set the context
                    WorkScheduler.CurrentWorkRequest.Value = request;

                    // Verify immediate set
                    if (WorkScheduler.CurrentWorkRequest.Value!.StepName != $"Step-{workerId}")
                    {
                        errors.Add($"Worker {workerId}: Immediate read failed.");
                    }

                    // Simulate some work with await to force context switching
                    await Task.Delay(Random.Shared.Next(10, 50));

                    // Verify execution context is preserved after await
                    var current = WorkScheduler.CurrentWorkRequest.Value;
                    if (current == null || current.StepName != $"Step-{workerId}")
                    {
                        errors.Add($"Worker {workerId}: Post-await read failed. Expected Step-{workerId}, got {current?.StepName ?? "null"}");
                    }

                    // Simulate nested async call
                    await NestedMethod(workerId, errors);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Empty(errors);
        }

        private async Task NestedMethod(int workerId, System.Collections.Concurrent.ConcurrentBag<string> errors)
        {
            await Task.Delay(10);
            var current = WorkScheduler.CurrentWorkRequest.Value;
            if (current == null || current.StepName != $"Step-{workerId}")
            {
                errors.Add($"Worker {workerId}: Nested method read failed.");
            }
        }
    }
}
