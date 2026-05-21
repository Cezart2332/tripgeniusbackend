namespace TripGeniusBackend.Application.Settings;

public class ModerationSettings
{
    public bool Enabled { get; set; } = true;

    public bool ImageEnabled { get; set; } = true;

    public bool TextEnabled { get; set; } = true;

    public string BaseUrl { get; set; } = "http://moderation:8000";

    public int TimeoutSeconds { get; set; } = 3;

    public double NsfwThreshold { get; set; } = 0.85;

    public double ToxicThreshold { get; set; } = 0.5;
}
