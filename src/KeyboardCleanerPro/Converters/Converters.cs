using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Converters;

// ── Bool → Visibility ─────────────────────────────────────────────────────────

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        value is Visibility.Visible;
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibility : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is false ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        value is not Visibility.Visible;
}

// ── AppState → Status text ────────────────────────────────────────────────────

[ValueConversion(typeof(AppState), typeof(string))]
public sealed class StateToStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is AppState state ? state switch
        {
            AppState.Idle             => "Ready",
            AppState.Detecting        => "Detecting keyboard…",
            AppState.KeyboardEnabled  => "Keyboard Active",
            AppState.Disabling        => "Locking…",
            AppState.KeyboardDisabled => "Keyboard Locked",
            AppState.Enabling         => "Unlocking…",
            AppState.Recovering       => "Recovering…",
            AppState.Error            => "Error",
            _                         => "—"
        } : "—";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}

// ── AppState → Status dot colour (only green/red/neutral — no purple/blue) ───

[ValueConversion(typeof(AppState), typeof(Color))]
public sealed class StateToStatusColorConverter : IValueConverter
{
    // #22C55E — green  (keyboard active / healthy)
    // #EF4444 — red    (keyboard locked / error)
    // #3C3C3C — muted  (transitional / idle)
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is AppState state ? state switch
        {
            AppState.KeyboardEnabled  => Color.FromRgb(0x22, 0xC5, 0x5E), // green
            AppState.KeyboardDisabled => Color.FromRgb(0xEF, 0x44, 0x44), // red
            AppState.Error            => Color.FromRgb(0xF5, 0x9E, 0x0B), // amber
            AppState.Detecting
                or AppState.Disabling
                or AppState.Enabling
                or AppState.Recovering => Color.FromRgb(0x52, 0x52, 0x52), // muted
            _                          => Color.FromRgb(0x38, 0x38, 0x38)  // idle
        } : Color.FromRgb(0x38, 0x38, 0x38);

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}

// ── AppState → Button Style key ───────────────────────────────────────────────

public sealed class StateToButtonStyleConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is AppState.KeyboardDisabled ? "MainToggleButtonDanger" : "MainToggleButton";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}

// ── AppState → Button label ───────────────────────────────────────────────────

public sealed class StateToButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is AppState.KeyboardDisabled ? "UNLOCK" : "LOCK";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}

// ── TimeSpan? → MM:SS ─────────────────────────────────────────────────────────

[ValueConversion(typeof(TimeSpan?), typeof(string))]
public sealed class TimeSpanToDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
    {
        if (value is not TimeSpan ts) return "--:--";
        if (ts <= TimeSpan.Zero)     return "00:00";
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
    }

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}
