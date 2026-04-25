namespace Crontinel;

/// <summary>
/// Configuration options for the Crontinel client.
/// </summary>
public class CrontinelOptions
{
    /// <summary>
    /// Your Crontinel API key. Required.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Crontinel API. Defaults to https://app.crontinel.com.
    /// </summary>
    public string ApiUrl { get; set; } = "https://app.crontinel.com";

    /// <summary>
    /// Application name reported to Crontinel. Defaults to "dotnet".
    /// </summary>
    public string AppName { get; set; } = "dotnet";

    /// <summary>
    /// Custom User-Agent string. Defaults to "crontinel-dotnet/{version}".
    /// </summary>
    public string? UserAgent { get; set; }

    internal string Version { get; } = "1.0.0";

    internal string EffectiveUserAgent =>
        UserAgent ?? $"crontinel-dotnet/{Version}";
}
