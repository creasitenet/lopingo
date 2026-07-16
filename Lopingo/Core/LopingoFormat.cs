namespace Lopingo.Core;

public static class LopingoFormat
{
    public static string Freq(int sec) =>
        sec is >= 3600 and > 0 && sec % 3600 == 0 ? $"{sec / 3600} h" :
        sec is > 0 && sec % 60 == 0 ? $"{sec / 60} min" : $"{sec} s";

    public static string LocalTime(DateTime? utc) =>
        utc is null ? "—" : utc.Value.ToLocalTime().ToString("g");

    public static string Ms(int? ms) => ms is null ? "—" : $"{ms} ms";

    public static string AvgMs(double? ms) => ms is null ? "—" : $"{Math.Round(ms.Value)} ms";

    public static string Uptime(double? pct) => pct is null ? "—" : $"{pct.Value:F2}%";
}
