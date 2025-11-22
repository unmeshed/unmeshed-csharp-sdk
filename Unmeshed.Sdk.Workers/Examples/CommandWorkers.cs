using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Unmeshed.Sdk.Workers;

namespace Unmeshed.Sdk.Workers.Examples;

/// <summary>
/// Request model for command execution.
/// </summary>
public class CommandExecutionRequest
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 300;

    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
}

/// <summary>
/// Response model for command execution.
/// </summary>
public class CommandExecutionResponse
{
    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }

    [JsonPropertyName("stdout")]
    public string StandardOutput { get; set; } = string.Empty;

    [JsonPropertyName("stderr")]
    public string StandardError { get; set; } = string.Empty;

    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

/// <summary>
/// Worker for executing bash commands on Unix-like systems.
/// </summary>
public class BashCommandWorker
{
    [WorkerFunction(Name = "bash", Namespace = "default", MaxInProgress = 1000, IoThread = false)]
    public async Task<CommandExecutionResponse> RunBashScriptAsync(Dictionary<string, object> input)
    {
        var request = System.Text.Json.JsonSerializer.Deserialize<CommandExecutionRequest>(
            System.Text.Json.JsonSerializer.Serialize(input));

        if (request == null || string.IsNullOrWhiteSpace(request.Command))
        {
            throw new ArgumentException("Command is required");
        }

        var startTime = DateTimeOffset.UtcNow;

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{request.Command.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory
        };

        // Add environment variables
        if (request.EnvironmentVariables != null)
        {
            foreach (var (key, value) in request.EnvironmentVariables)
            {
                processStartInfo.Environment[key] = value;
            }
        }

        using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait with timeout
        var completed = await Task.Run(() => 
            process.WaitForExit(request.TimeoutSeconds * 1000));

        if (!completed)
        {
            process.Kill(true);
            throw new TimeoutException(
                $"Command execution timed out after {request.TimeoutSeconds} seconds");
        }

        var executionTime = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

        return new CommandExecutionResponse
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString(),
            ExecutionTimeMs = (long)executionTime,
            Success = process.ExitCode == 0
        };
    }
}

/// <summary>
/// Worker for executing CMD commands on Windows systems.
/// </summary>
public class CmdCommandWorker
{
    [WorkerFunction(Name = "cmd", Namespace = "default", MaxInProgress = 1000, IoThread = false)]
    public async Task<CommandExecutionResponse> RunCmdScriptAsync(Dictionary<string, object> input)
    {
        var request = System.Text.Json.JsonSerializer.Deserialize<CommandExecutionRequest>(
            System.Text.Json.JsonSerializer.Serialize(input));

        if (request == null || string.IsNullOrWhiteSpace(request.Command))
        {
            throw new ArgumentException("Command is required");
        }

        var startTime = DateTimeOffset.UtcNow;

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {request.Command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory
        };

        // Add environment variables
        if (request.EnvironmentVariables != null)
        {
            foreach (var (key, value) in request.EnvironmentVariables)
            {
                processStartInfo.Environment[key] = value;
            }
        }

        using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait with timeout
        var completed = await Task.Run(() => 
            process.WaitForExit(request.TimeoutSeconds * 1000));

        if (!completed)
        {
            process.Kill(true);
            throw new TimeoutException(
                $"Command execution timed out after {request.TimeoutSeconds} seconds");
        }

        var executionTime = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

        return new CommandExecutionResponse
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString(),
            ExecutionTimeMs = (long)executionTime,
            Success = process.ExitCode == 0
        };
    }
}
