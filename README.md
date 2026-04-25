# Crontinel.NET

[![NuGet](https://img.shields.io/nuget/v/Crontinel?style=flat-square)](https://www.nuget.org/packages/Crontinel)
[![Build](https://img.shields.io/github/actions/workflow/status/crontinel/dotnet-sdk/ci.yml?style=flat-square)](https://github.com/crontinel/dotnet-sdk/actions)
[![MIT License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](LICENSE)

Crontinel monitoring SDK for .NET applications. Send cron, queue, and job monitoring events from any .NET application — standalone or alongside the `crontinel/laravel` PHP package.

## Install

```bash
dotnet add package Crontinel
```

## Requirements

- .NET 8.0+ or .NET Standard 2.0

## Quick Start

```csharp
using Crontinel;
using Crontinel.Models;

var client = new CrontinelClient(
    Environment.GetEnvironmentVariable("CRONTINEL_API_KEY")!
);

// Report a cron job run
await client.ScheduleRunAsync(new ScheduleRunOptions
{
    Command = "reports:generate",
    DurationMs = 2340,
    ExitCode = 0  // 0 = success, non-zero = failure
});

// Report queue worker activity
await client.QueueProcessedAsync(new QueueProcessedOptions
{
    Queue = "emails",
    Processed = 12,
    Failed = 0,
    DurationMs = 8901
});

// Send a custom event or alert
await client.EventAsync(new CustomEventOptions
{
    Key = "disk-space-warning",
    Message = "Disk usage above 90%",
    State = "firing"
});
```

## `MonitorScheduleAsync` — Wrap Any Async Function

Automatically report the outcome of any async job:

```csharp
var result = await client.MonitorScheduleAsync("reports:generate", async () =>
{
    // your cron job logic
    await SendDailyReports();
    return true;  // return value is preserved
});

Console.WriteLine($"Duration: {result.DurationMs}ms, ExitCode: {result.ExitCode}");
```

If the inner function throws, the exception propagates normally and `ExitCode` is set to `1`.

## Configuration

```csharp
var client = new CrontinelClient(new CrontinelOptions
{
    ApiKey = "your-api-key",
    ApiUrl = "https://app.crontinel.com",  // optional, default
    AppName = "my-service",                  // optional, default: "dotnet"
    UserAgent = "my-app/1.0"               // optional
});
```

Set environment variables and create the client:

```csharp
var client = new CrontinelClient(
    Environment.GetEnvironmentVariable("CRONTINEL_API_KEY")!
);
```

## Error Handling

`CrontinelClient` throws `CrontinelException` on API errors (non-2xx responses). In `MonitorScheduleAsync`, monitoring failures are silently swallowed — your job is never affected by monitoring issues.

```csharp
try
{
    await client.ScheduleRunAsync(options);
}
catch (CrontinelException ex)
{
    Console.WriteLine($"Monitoring failed: {ex.StatusCode} {ex.Message}");
}
```

## Recipes

### ASP.NET Core / Worker Service

Register as a singleton:

```csharp
// Program.cs
builder.Services.AddSingleton<CrontinelClient>(_ =>
    new CrontinelClient(
        Environment.GetEnvironmentVariable("CRONTINEL_API_KEY")!
    ));

// In a hosted service:
public class MyWorker : BackgroundService
{
    private readonly CrontinelClient _crontinel;

    public MyWorker(CrontinelClient crontinel) => _crontinel = crontinel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _crontinel.MonitorScheduleAsync("my-worker-tick", async () =>
            {
                await DoWorkAsync();
            }, stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

### Scheduled Job with Coravel

```csharp
using Coravel.Invocable;
using Crontinel.Models;

public class ReportsJob : IInvocable
{
    private readonly CrontinelClient _crontinel;

    public ReportsJob(CrontinelClient crontinel) => _crontinel = crontinel;

    public async Task Invoke()
    {
        await _crontinel.MonitorScheduleAsync("reports:daily", async () =>
        {
            await GenerateReportsAsync();
        });
    }
}
```

## License

MIT © Crontinel
