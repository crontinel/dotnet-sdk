using System;
using System.Collections.Generic;

namespace Crontinel.Models;

/// <summary>
/// Options for sending a custom alert or informational event.
/// </summary>
public class CustomEventOptions
{
    /// <summary>
    /// Unique key identifying this event (e.g. "disk-space-warning").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message describing the event.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Event state. Defaults to "info".
    /// </summary>
    public string State { get; set; } = "info";

    /// <summary>
    /// Arbitrary key-value metadata attached to the event.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// When the event occurred. Defaults to DateTime.UtcNow.
    /// </summary>
    public DateTime? RanAt { get; set; }
}
