using MudBlazor;

namespace Lopingo.Components;

public static class LopingoTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#4F46E5",
            Secondary = "#6366F1",
            Success = "#10B981",
            Info = "#0EA5E9",
            Warning = "#F59E0B",
            Error = "#EF4444",

            Background = "#F8FAFC",
            BackgroundGray = "#F1F5F9",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            DrawerText = "rgba(15,23,42,0.78)",
            AppbarText = "rgba(15,23,42,0.78)",
            TextPrimary = "rgba(15,23,42,0.95)",
            TextSecondary = "rgba(71,85,105,0.95)",
            TextDisabled = "rgba(148,163,184,0.95)",

            Divider = "rgba(15,23,42,0.08)",
            LinesDefault = "rgba(15,23,42,0.08)",
            TableLines = "rgba(15,23,42,0.06)",
            TableStriped = "rgba(15,23,42,0.02)",
            TableHover = "rgba(79,70,229,0.04)",

            ActionDefault = "rgba(15,23,42,0.54)",
            ActionDisabled = "rgba(15,23,42,0.26)",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#818CF8",
            Secondary = "#A5B4FC",
            Success = "#34D399",
            Info = "#38BDF8",
            Warning = "#FBBF24",
            Error = "#F87171",

            Background = "#0B1020",
            BackgroundGray = "#111827",
            Surface = "#111827",
            DrawerBackground = "#0B1020",
            AppbarBackground = "#0B1020",
            DrawerText = "rgba(241,245,249,0.78)",
            AppbarText = "rgba(241,245,249,0.78)",
            TextPrimary = "rgba(241,245,249,0.95)",
            TextSecondary = "rgba(203,213,225,0.78)",
            TextDisabled = "rgba(148,163,184,0.42)",

            Divider = "rgba(241,245,249,0.08)",
            LinesDefault = "rgba(241,245,249,0.08)",
            TableLines = "rgba(241,245,249,0.06)",
            TableStriped = "rgba(241,245,249,0.02)",
            TableHover = "rgba(129,140,248,0.08)",

            ActionDefault = "rgba(241,245,249,0.54)",
            ActionDisabled = "rgba(241,245,249,0.26)",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "240px",
            DrawerWidthRight = "240px",
            AppbarHeight = "64px",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.95rem",
                FontWeight = "400",
                LineHeight = "1.5",
            },
            H1 = new H1Typography { FontSize = "2.25rem", FontWeight = "700" },
            H2 = new H2Typography { FontSize = "1.75rem", FontWeight = "600" },
            H3 = new H3Typography { FontSize = "1.375rem", FontWeight = "600" },
            H4 = new H4Typography { FontSize = "1.125rem", FontWeight = "600" },
            H5 = new H5Typography { FontSize = "1rem",     FontWeight = "600" },
            H6 = new H6Typography { FontSize = "0.875rem", FontWeight = "600" },
            Button = new ButtonTypography { FontSize = "0.9rem", FontWeight = "500", TextTransform = "none" },
        },
    };
}
