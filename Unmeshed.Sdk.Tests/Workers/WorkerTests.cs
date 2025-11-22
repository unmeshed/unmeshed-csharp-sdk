using FluentAssertions;
using Unmeshed.Sdk.Workers;
using Xunit;

namespace Unmeshed.Sdk.Tests.Workers;

public class WorkerTests
{
    [Fact]
    public void Worker_FormattedId_ShouldCombineNamespaceAndName()
    {
        // Arrange
        var worker = new Worker
        {
            Name = "test-worker",
            Namespace = "my-namespace"
        };

        // Act
        var formattedId = worker.FormattedId;

        // Assert
        formattedId.Should().Be("my-namespace:-#-:test-worker");
    }

    [Fact]
    public void Worker_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var worker = new Worker
        {
            Name = "test"
        };

        // Assert
        worker.Namespace.Should().Be("default");
        worker.MaxInProgress.Should().Be(10);
        worker.IoThread.Should().BeFalse();
    }
}

public class WorkerScannerTests
{
    [Fact]
    public void WorkerScanner_FindWorkers_ShouldDiscoverAnnotatedWorkers()
    {
        // Arrange
        var namespacePath = "Unmeshed.Sdk.Tests.Workers";

        // Act
        var workers = WorkerScanner.FindWorkers(namespacePath);

        // Assert
        workers.Should().NotBeNull();
        workers.Should().Contain(w => w.Name == "test-worker");
    }
}

// Test worker for scanner
public class TestWorkerClass
{
    [WorkerFunction(Name = "test-worker", Namespace = "test", MaxInProgress = 5)]
    public async Task<object> TestWorkerMethod(Dictionary<string, object> input)
    {
        await Task.Delay(10);
        return new { result = "success" };
    }
}
