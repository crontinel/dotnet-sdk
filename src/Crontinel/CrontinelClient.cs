using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Crontinel.Models;

namespace Crontinel;

/// <summary>
/// Crontinel monitoring client for .NET applications.
/// Send cron, queue, and job monitoring events from any .NET application.
/// </summary>
/// <example>
/// <code>
/// var client = new CrontinelClient(new CrontinelOptions
/// {
///     ApiKey = Environment.GetEnvironmentVariable("CRONTINEL_API_KEY")!
/// });
///
/// await client.ScheduleRunAsync(new ScheduleRunOptions
/// {
///     Command = "reports:generate",
///     DurationMs = 2340,
///     ExitCode = 0
/// });
/// </code>
/// </example>
public class CrontinelClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CrontinelOptions _options;
    private readonly Uri _apiUri;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Creates a new Crontinel client.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    /// <exception cref="CrontinelException">Thrown when ApiKey is not set.</exception>
    public CrontinelClient(CrontinelOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new CrontinelException("ApiKey is required. Provide it via CrontinelOptions.ApiKey.");
        }

        _options = options;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", options.EffectiveUserAgent);

        var baseUrl = options.ApiUrl.TrimEnd('/');
        _apiUri = new Uri($"{baseUrl}/api/mcp");
    }

    /// <summary>
    /// Creates a new Crontinel client with default options.
    /// </summary>
    /// <param name="apiKey">Your Crontinel API key.</param>
    /// <exception cref="CrontinelException">Thrown when apiKey is null or whitespace.</exception>
    public CrontinelClient(string apiKey)
        : this(new CrontinelOptions { ApiKey = apiKey })
    {
    }

    /// <summary>
    /// Report a scheduled command run.
    /// Call this after your scheduler completes a job.
    /// </summary>
    /// <example>
    /// <code>
    /// await client.ScheduleRunAsync(new ScheduleRunOptions
    /// {
    ///     Command = "send-daily-reports",
    ///     DurationMs = 1840,
    ///     ExitCode = 0
    /// });
    /// </code>
    /// </example>
    public async Task ScheduleRunAsync(
        ScheduleRunOptions options,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var payload = new Dictionary<string, object>
        {
            { "command", options.Command },
            { "exit_code", options.ExitCode },
            { "ran_at", (options.RanAt ?? DateTime.UtcNow).ToString("o") },
        };

        if (options.DurationMs.HasValue)
        {
            payload["duration_ms"] = options.DurationMs.Value;
        }

        await SendRequestAsync("notify/schedule_run", payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Report queue worker activity.
    /// </summary>
    /// <example>
    /// <code>
    /// await client.QueueProcessedAsync(new QueueProcessedOptions
    /// {
    ///     Queue = "emails",
    ///     Processed = 12,
    ///     Failed = 0,
    ///     DurationMs = 8901
    /// });
    /// </code>
    /// </example>
    public async Task QueueProcessedAsync(
        QueueProcessedOptions options,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var payload = new Dictionary<string, object>
        {
            { "queue", options.Queue },
            { "processed", options.Processed },
            { "failed", options.Failed },
            { "ran_at", (options.RanAt ?? DateTime.UtcNow).ToString("o") },
            { "app", _options.AppName },
        };

        if (options.DurationMs.HasValue)
        {
            payload["duration_ms"] = options.DurationMs.Value;
        }

        await SendRequestAsync("notify/queue_processed", payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a custom alert or informational event.
    /// </summary>
    /// <example>
    /// <code>
    /// await client.EventAsync(new CustomEventOptions
    /// {
    ///     Key = "disk-space-warning",
    ///     Message = "Disk usage above 90%",
    ///     State = "firing"
    /// });
    /// </code>
    /// </example>
    public async Task EventAsync(
        CustomEventOptions options,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var payload = new Dictionary<string, object>
        {
            { "key", options.Key },
            { "message", options.Message },
            { "state", options.State },
            { "metadata", options.Metadata ?? new Dictionary<string, object>() },
            { "ran_at", (options.RanAt ?? DateTime.UtcNow).ToString("o") },
            { "app", _options.AppName },
        };

        await SendRequestAsync("notify/event", payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Convenience: wrap any async function and report its outcome as a scheduled command.
    /// Failures in the monitoring call do not propagate — they are silently swallowed.
    /// </summary>
    /// <example>
    /// <code>
    /// var result = await client.MonitorScheduleAsync("reports:generate", async () =>
    /// {
    ///     await SendDailyReports();
    ///     return true;
    /// });
    /// </code>
    /// </example>
    public async Task<MonitorScheduleResult<T>> MonitorScheduleAsync<T>(
        string command,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var start = DateTimeOffset.UtcNow;
        int exitCode = 0;
        T result = default!;

        try
        {
            result = await action().ConfigureAwait(false);
        }
        catch
        {
            exitCode = 1;
            throw;
        }
        finally
        {
            var durationMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

            try
            {
                await ScheduleRunAsync(
                    new ScheduleRunOptions
                    {
                        Command = command,
                        DurationMs = durationMs,
                        ExitCode = exitCode,
                    },
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch
            {
                // Silently swallow monitoring failures — don't crash the caller's job
            }
        }

        var totalMs = (int)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
        return new MonitorScheduleResult<T>(result, totalMs, exitCode);
    }

    /// <summary>
    /// Convenience overload of MonitorScheduleAsync for void actions.
    /// </summary>
    public async Task<MonitorScheduleResult<VoidResult>> MonitorScheduleAsync(
        string command,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        var result = await MonitorScheduleAsync<VoidResult>(
            command,
            async () =>
            {
                await action().ConfigureAwait(false);
                return new VoidResult();
            },
            cancellationToken
        ).ConfigureAwait(false);

        return result;
    }

    private async Task SendRequestAsync(
        string method,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            method,
            @params = parameters,
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient
            .PostAsync(_apiUri, content, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new CrontinelException((int)response.StatusCode, body);
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CrontinelClient));
        }
    }

    /// <summary>
    /// Disposes the underlying HttpClient. Prefer using the client as a singleton.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient.Dispose();
    }

    /// <summary>
    /// Placeholder for void MonitorScheduleAsync return type.
    /// </summary>
    public class VoidResult { }
}
