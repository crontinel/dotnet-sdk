namespace Crontinel.Models;

/// <summary>
/// Result of a monitored schedule call.
/// </summary>
/// <typeparam name="T">Return type of the wrapped function.</typeparam>
public class MonitorScheduleResult<T>
{
    /// <summary>
    /// The return value from the wrapped function.
    /// </summary>
    public T Result { get; }

    /// <summary>
    /// How long the function took to execute, in milliseconds.
    /// </summary>
    public int DurationMs { get; }

    /// <summary>
    /// The exit code: 0 for success, 1 for failure.
    /// </summary>
    public int ExitCode { get; }

    public MonitorScheduleResult(T result, int durationMs, int exitCode)
    {
        Result = result;
        DurationMs = durationMs;
        ExitCode = exitCode;
    }
}
