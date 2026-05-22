namespace TripGeniusBackend.Application.Settings;

public static class ModerationHttpTimeouts
{
    public const int MinClientSeconds = 10;
    public const int MaxClientSeconds = 120;
    public const int StartupProbeSeconds = 180;

    public static int ClientSeconds(int configuredSeconds) =>
        Math.Clamp(configuredSeconds, MinClientSeconds, MaxClientSeconds);

    public static TimeSpan Client(int configuredSeconds) =>
        TimeSpan.FromSeconds(ClientSeconds(configuredSeconds));

    public static TimeSpan StartupProbe => TimeSpan.FromSeconds(StartupProbeSeconds);
}
