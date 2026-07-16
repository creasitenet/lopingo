namespace Lopingo.Core;

public static class MonitorValidation
{
    public static readonly (int Sec, string Label)[] FrequencyOptions =
    {
        (60, "1 minute"),
        (300, "5 minutes"),
        (900, "15 minutes"),
        (1800, "30 minutes"),
        (3600, "1 hour"),
    };

    public static readonly int[] AllowedFrequencies =
        FrequencyOptions.Select(o => o.Sec).ToArray();

    public static bool IsValidUrl(string s) =>
        Uri.TryCreate(s, UriKind.Absolute, out var u) &&
        (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}
