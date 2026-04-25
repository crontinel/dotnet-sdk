namespace Crontinel.Models;

/// <summary>
/// Options for reporting a scheduled command run.
/// </summary>
public class ScheduleRunOptions
{
    /// <summary>
    /// The command that was run (e.g. "php artisan inspire").
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Duration of the run in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Exit code: 0 for success, non-zero for failure. Defaults to 0.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// When the command ran. Defaults to DateTime.UtcNow.
    /// </summary>
    public DateTime? RanAt { get; set; }
}
