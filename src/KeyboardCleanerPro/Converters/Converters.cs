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
            AppState.Idle            => "Ready",
            AppState.Detecting       => "Detecting keyboard…",
            AppState.KeyboardEnabled => "Keyboard Active",
            AppState.Disabling       => "Locking keyboard…",
            AppState.KeyboardDisabled => "Keyboard Locked",
            AppState.Enabling        => "Unlocking keyboard…",
            AppState.Recovering      => "Recovering…",
            AppState.Error           => "Error",
            _                        => "Unknown"
        } : "Unknown";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}

// ── AppState → Status dot colour ─────────────────────────────────────────────

[ValueConversion(typeof(AppState), typeof(Color))]
public sealed class StateToStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is AppState state ? state switch
        {
            AppState.KeyboardEnabled  => Color.FromRgb(0x4C, 0xAF, 0x50), // green
            AppState.KeyboardDisabled => Color.FromRgb(0xEF, 0x53, 0x50), // red
            AppState.Detecting
                or AppState.Disabling
                or AppState.Enabling
                or AppState.Recovering  => Color.FromRgb(0x4F, 0xC3, 0xF7), // blue
            AppState.Error              => Color.FromRgb(0xFF, 0x70, 0x43), // orange
            _                           => Color.FromRgb(0x55, 0x55, 0x77)  // muted
        } : Color.FromRgb(0x55, 0x55, 0x77);

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}

// ── AppState → Button Style key (unused in final design, kept for extensibility)

public sealed class StateToButtonStyleConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is AppState.KeyboardDisabled ? "MainToggleButtonDanger" : "MainToggleButton";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}

// ── AppState → Button label text ─────────────────────────────────────────────

public sealed class StateToButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is AppState.KeyboardDisabled ? "UNLOCK KEYBOARD" : "LOCK KEYBOARD";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
        Binding.DoNothing;
}

// ── TimeSpan? → MM:SS display string ─────────────────────────────────────────

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
