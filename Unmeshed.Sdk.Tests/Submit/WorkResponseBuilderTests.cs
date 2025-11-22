using FluentAssertions;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Models;
using Unmeshed.Sdk.Submit;
using Xunit;

namespace Unmeshed.Sdk.Tests.Submit;

public class WorkResponseBuilderTests
{
    private readonly WorkResponseBuilder _builder;
    private readonly ClientConfig _config;

    public WorkResponseBuilderTests()
    {
        _builder = new WorkResponseBuilder();
        _config = new ClientConfig
        {
            ClientId = "test",
            AuthToken = "token"
        };
    }

    [Fact]
    public void SuccessResponse_ShouldCreateCorrectResponse()
    {
        // Arrange
        var request = new WorkRequest
        {
            StepId = 123,
            ProcessId = 456,
            StepExecutionId = 1,
            RunCount = 1
        };
        var result = new StepResult
        {
            Output = new Dictionary<string, object> { { "key", "value" } },
            StartedAt = 1000,
            CompletedAt = 2000
        };

        // Act
        var response = _builder.SuccessResponse(request, result);

        // Assert
        response.StepId.Should().Be(123);
        response.ProcessId.Should().Be(456);
        response.StepExecutionId.Should().Be(1);
        response.RunCount.Should().Be(1);
        response.Status.Should().Be("COMPLETED");
        response.Output.Should().BeEquivalentTo(result.Output);
        response.StartedAt.Should().Be(1000);
    }

    [Fact]
    public void RunningResponse_ShouldCreateCorrectResponse()
    {
        // Arrange
        var request = new WorkRequest
        {
            StepId = 123,
            ProcessId = 456,
            StepExecutionId = 1,
            RunCount = 1
        };
        var result = new StepResult
        {
            Output = new Dictionary<string, object> { { "progress", 50 } },
            KeepRunning = true,
            RescheduleAfterSeconds = 60,
            StartedAt = 1000
        };

        // Act
        var response = _builder.RunningResponse(request, result);

        // Assert
        response.StepId.Should().Be(123);
        response.ProcessId.Should().Be(456);
        response.StepExecutionId.Should().Be(1);
        response.RunCount.Should().Be(1);
        response.Status.Should().Be("RUNNING");
        response.Output.Should().BeEquivalentTo(result.Output);
        response.RescheduleAfterSeconds.Should().Be(60);
        response.StartedAt.Should().Be(1000);
    }

    [Fact]
    public void FailResponse_ShouldCreateCorrectResponse()
    {
        // Arrange
        var request = new WorkRequest
        {
            StepId = 123,
            ProcessId = 456,
            StepExecutionId = 1,
            RunCount = 1
        };
        var exception = new Exception("Test error");
        var config = new ClientConfig 
        { 
            ClientId = "test", 
            AuthToken = "test" 
        };

        // Act
        var response = _builder.FailResponse(request, exception, config);

        // Assert
        response.StepId.Should().Be(123);
        response.ProcessId.Should().Be(456);
        response.StepExecutionId.Should().Be(1);
        response.RunCount.Should().Be(1);
        response.Status.Should().Be("FAILED");
        
        response.Output.Should().ContainKey("error");
        response.Output["error"].Should().Be("Test error");
        response.Output.Should().NotContainKey("errorStackTrace");
    }

    [Fact]
    public void FailResponse_ShouldTruncateLongMessages()
    {
        // Arrange
        var request = new WorkRequest
        {
            StepId = 123,
            ProcessId = 456,
            StepExecutionId = 1,
            RunCount = 1
        };
        var longMessage = new string('a', 2000);
        var exception = new Exception(longMessage);
        var config = new ClientConfig 
        { 
            ClientId = "test", 
            AuthToken = "test" 
        };

        // Act
        var response = _builder.FailResponse(request, exception, config);

        // Assert
        var errorMessage = response.Output["error"] as string;
        errorMessage.Should().EndWith("... (truncated)");
        errorMessage.Should().HaveLength(1000 + 15); // 1000 chars + suffix
    }
}
