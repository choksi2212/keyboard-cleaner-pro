using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using KeyboardCleanerPro.Core.Models;
using KeyboardCleanerPro.Core.Services.Configuration;
using KeyboardCleanerPro.Core.Services.Control;
using KeyboardCleanerPro.Core.Services.Detection;
using KeyboardCleanerPro.Core.Services.Logging;
using KeyboardCleanerPro.Core.Services.Recovery;
using KeyboardCleanerPro.Core.Services.State;

namespace KeyboardCleanerPro.ViewModels;

/// <summary>
/// Primary ViewModel for the main window. Drives the full state machine
/// (Idle → Detecting → Enabled ↔ Disabling/Enabling → Recovering → Error).
/// All service calls are dispatched to a background thread; UI updates
/// always marshal back to the UI thread via Dispatcher.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    // ── Services ──────────────────────────────────────────────────────────────
    private readonly IKeyboardDetectionService _detectionService;
    private readonly IDeviceControlService     _controlService;
    private readonly IRecoveryService          _recoveryService;
    private readonly IStateManager             _stateManager;
    private readonly IConfigurationService     _configService;
    private readonly ILoggingService           _logger;

    // ── State ─────────────────────────────────────────────────────────────────
    private KeyboardDevice?  _detectedDevice;
    private AppState         _currentState  = AppState.Idle;
    private string?          _errorMessage;
    private TimeSpan?        _remainingTime;
    private TimerOption      _selectedTimerMinutes;

    // ── Timer for UI countdown refresh ────────────────────────────────────────
    private readonly DispatcherTimer _uiRefreshTimer;
    private bool                     _disposed;

    public MainViewModel(
        IKeyboardDetectionService detectionService,
        IDeviceControlService     controlService,
        IRecoveryService          recoveryService,
        IStateManager             stateManager,
        IConfigurationService     configService,
        ILoggingService           logger)
    {
        _detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
        _controlService   = controlService   ?? throw new ArgumentNullException(nameof(controlService));
        _recoveryService  = recoveryService  ?? throw new ArgumentNullException(nameof(recoveryService));
        _stateManager     = stateManager     ?? throw new ArgumentNullException(nameof(stateManager));
        _configService    = configService    ?? throw new ArgumentNullException(nameof(configService));
        _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));

        // Build timer options
        TimerOptions = [
            new TimerOption(1,  "1 minute"),
            new TimerOption(2,  "2 minutes"),
            new TimerOption(3,  "3 minutes"),
            new TimerOption(5,  "5 minutes"),
            new TimerOption(10, "10 minutes"),
            new TimerOption(15, "15 minutes"),
            new TimerOption(30, "30 minutes"),
        ];

        var settings = configService.Load();
        _selectedTimerMinutes = TimerOptions.FirstOrDefault(t => t.Minutes == settings.AutoRestoreMinutes)
                             ?? TimerOptions.First(t => t.Minutes == 5);

        ToggleCommand = new RelayCommand(OnToggle, () => CanToggle);

        // UI refresh every second for countdown
        _uiRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uiRefreshTimer.Tick += (_, _) => RefreshCountdown();
        _uiRefreshTimer.Start();

        // Auto-detect on startup
        _ = DetectAsync();
    }

    // ── Bindable properties ───────────────────────────────────────────────────

    public AppState CurrentState
    {
        get => _currentState;
        private set { _currentState = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsKeyboardEnabled)); OnPropertyChanged(nameof(CanToggle)); OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(ToggleButtonText)); OnPropertyChanged(nameof(HasError)); }
    }

    public bool IsKeyboardEnabled =>
        _currentState == AppState.KeyboardEnabled || _currentState == AppState.Idle || _currentState == AppState.Detecting;

    public bool CanToggle =>
        _currentState is AppState.KeyboardEnabled or AppState.KeyboardDisabled;

    public bool IsBusy =>
        _currentState is AppState.Detecting or AppState.Disabling or AppState.Enabling or AppState.Recovering;

    public bool HasError =>
        _currentState == AppState.Error && !string.IsNullOrEmpty(_errorMessage);

    public bool DeviceDetected => _detectedDevice is not null;

    public string DeviceName => _detectedDevice?.Description is { Length: > 0 } d
        ? d
        : _currentState == AppState.Detecting ? "Detecting keyboard…"
        : _currentState == AppState.Error      ? "No keyboard found"
        : "Searching…";

    public string ConnectionType => _detectedDevice?.ConnectionType ?? string.Empty;

    public string ToggleButtonText => _currentState == AppState.KeyboardDisabled
        ? "UNLOCK"
        : "LOCK";

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }

    public bool IsTimerActive => _recoveryService.IsTimerActive;

    public TimeSpan? RemainingTime
    {
        get => _remainingTime;
        private set { _remainingTime = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<TimerOption> TimerOptions { get; }

    public TimerOption SelectedTimerMinutes
    {
        get => _selectedTimerMinutes;
        set
        {
            if (_selectedTimerMinutes == value) return;
            _selectedTimerMinutes = value;
            OnPropertyChanged();
            SaveTimerPreference(value.Minutes);
        }
    }

    public RelayCommand ToggleCommand { get; }

    // ── Commands ──────────────────────────────────────────────────────────────

    private void OnToggle()
    {
        if (_currentState == AppState.KeyboardEnabled)
            _ = DisableAsync();
        else if (_currentState == AppState.KeyboardDisabled)
            _ = EnableAsync();
    }

    // ── Async operations ──────────────────────────────────────────────────────

    private async Task DetectAsync()
    {
        SetState(AppState.Detecting);
        ErrorMessage = null;

        var result = await Task.Run(_detectionService.DetectInternalKeyboard);

        if (result.IsSuccess)
        {
            _detectedDevice = result.Value;
            OnPropertyChanged(nameof(DeviceName));
            OnPropertyChanged(nameof(ConnectionType));
            OnPropertyChanged(nameof(DeviceDetected));
            SetState(AppState.KeyboardEnabled);
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
            SetState(AppState.Error);
        }
    }

    private async Task DisableAsync()
    {
        if (_detectedDevice is null) return;

        SetState(AppState.Disabling);
        ErrorMessage = null;
        string instanceId = _detectedDevice.InstanceId;

        // Persist state BEFORE disabling (crash-safe)
        _stateManager.SaveState(new DeviceState
        {
            IsKeyboardDisabled = true,
            DeviceInstanceId   = instanceId,
            DisabledAt         = DateTime.UtcNow,
            AutoRestoreAt      = DateTime.UtcNow.AddMinutes(_selectedTimerMinutes.Minutes),
            AppVersion         = "1.0.0"
        });

        var result = await Task.Run(() => _controlService.DisableDevice(instanceId));

        if (result.IsSuccess)
        {
            _detectedDevice = _detectedDevice.WithEnabled(false);
            SetState(AppState.KeyboardDisabled);

            var delay = TimeSpan.FromMinutes(_selectedTimerMinutes.Minutes);
            _recoveryService.StartAutoRestoreTimer(instanceId, delay, () =>
            {
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    _logger.Log("AutoRestoreTimer", instanceId, "Fired-Starting restore", null, Environment.UserName);
                    await EnableAsync();
                });
            });

            OnPropertyChanged(nameof(IsTimerActive));
        }
        else
        {
            // Roll back persisted state since disable failed
            _stateManager.ClearState();
            ErrorMessage = $"Could not lock keyboard: {result.ErrorMessage}";
            SetState(AppState.KeyboardEnabled);
        }
    }

    private async Task EnableAsync()
    {
        if (_detectedDevice is null) return;

        SetState(AppState.Enabling);
        ErrorMessage = null;
        string instanceId = _detectedDevice.InstanceId;

        _recoveryService.CancelAutoRestoreTimer();
        OnPropertyChanged(nameof(IsTimerActive));

        var result = await Task.Run(() => _controlService.EnableDevice(instanceId));

        if (result.IsSuccess)
        {
            _stateManager.ClearState();
            _detectedDevice = _detectedDevice.WithEnabled(true);
            SetState(AppState.KeyboardEnabled);
        }
        else
        {
            ErrorMessage = $"Could not unlock keyboard: {result.ErrorMessage}";
            // Keyboard may still be disabled — stay in disabled state so user can retry
            SetState(AppState.KeyboardDisabled);
        }

        RemainingTime = null;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    /// <summary>Called by the window's close handler — ensures keyboard is restored on exit.</summary>
    public void OnWindowClosing()
    {
        _uiRefreshTimer.Stop();
        _recoveryService.CancelAutoRestoreTimer();

        // If keyboard is currently disabled, re-enable before closing
        if (_currentState == AppState.KeyboardDisabled && _detectedDevice is not null)
        {
            _controlService.EnableDevice(_detectedDevice.InstanceId);
            _stateManager.ClearState();
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetState(AppState state)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CurrentState = state;
            RelayCommand.RaiseCanExecuteChanged();
        });
    }

    private void RefreshCountdown()
    {
        if (_recoveryService.IsTimerActive)
        {
            RemainingTime = _recoveryService.RemainingTime;
            OnPropertyChanged(nameof(IsTimerActive));
        }
        else
        {
            if (RemainingTime.HasValue)
            {
                RemainingTime = null;
                OnPropertyChanged(nameof(IsTimerActive));
            }
        }
    }

    private void SaveTimerPreference(int minutes)
    {
        var settings = _configService.Load();
        settings.AutoRestoreMinutes = minutes;
        _configService.Save(settings);
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _uiRefreshTimer.Stop();
        if (_recoveryService is IDisposable d) d.Dispose();
    }
}

/// <summary>A timer duration option shown in the dropdown.</summary>
public sealed record TimerOption(int Minutes, string Display);
