namespace Crontinel.Models;

/// <summary>
/// Options for reporting queue worker activity.
/// </summary>
public class QueueProcessedOptions
{
    /// <summary>
    /// Name of the queue (e.g. "emails", "default").
    /// </summary>
    public string Queue { get; set; } = string.Empty;

    /// <summary>
    /// Number of jobs processed successfully. Defaults to 0.
    /// </summary>
    public int Processed { get; set; }

    /// <summary>
    /// Number of jobs that failed. Defaults to 0.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Total duration of the processing in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// When the processing occurred. Defaults to DateTime.UtcNow.
    /// </summary>
    public DateTime? RanAt { get; set; }
}
