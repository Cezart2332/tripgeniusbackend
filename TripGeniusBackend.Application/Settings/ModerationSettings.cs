namespace TripGeniusBackend.Application.Settings;

public class ModerationSettings
{
    public bool Enabled { get; set; } = true;

    public bool ImageEnabled { get; set; } = true;

    public bool TextEnabled { get; set; } = true;

    public string BaseUrl { get; set; } = "http://moderation:8000";

    /// <summary>HTTP timeout for /text-check and /image-check. First call needs model load; use ≥15.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// When false, failed moderation HTTP calls block content instead of allowing it through.
    /// </summary>
    public bool FailOpen { get; set; } = true;

    public double NsfwThreshold { get; set; } = 0.35;

    public double ToxicThreshold { get; set; } = 0.5;
}
