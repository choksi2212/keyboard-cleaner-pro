using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using KeyboardCleanerPro.Core.Services.Configuration;
using KeyboardCleanerPro.Core.Services.Control;
using KeyboardCleanerPro.Core.Services.Detection;
using KeyboardCleanerPro.Core.Services.Logging;
using KeyboardCleanerPro.Core.Services.Recovery;
using KeyboardCleanerPro.Core.Services.State;

namespace KeyboardCleanerPro;

/// <summary>
/// Application entry point. Handles dependency injection, UAC verification,
/// and unhandled exception capture before the main window is shown.
/// </summary>
public partial class App : Application
{
    private static readonly string DataDirectory =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "KeyboardCleaner");

    private static readonly string LogDirectory = System.IO.Path.Combine(DataDirectory, "Logs");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Guard: must run as Administrator ─────────────────────────────────
        if (!AdminGuard.IsRunningAsAdmin())
        {
            AdminGuard.RelaunchAsAdmin();
            Shutdown(0);
            return;
        }

        // ── Wire up global exception handlers ────────────────────────────────
        DispatcherUnhandledException             += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException       += OnUnobservedTaskException;

        // ── Compose the object graph (manual DI — no container overhead) ─────
        var logger           = new LoggingService(LogDirectory, enabled: true);
        var configService    = new ConfigurationService(DataDirectory);
        var stateManager     = new StateManager(DataDirectory);
        var scorer           = new DeviceScorer();
        var detectionService = new KeyboardDetectionService(scorer, logger);
        var controlService   = new DeviceControlService(logger);
        var taskManager      = new ScheduledTaskManager();
        var recoveryService  = new RecoveryService(logger, taskManager);

        var settings = configService.Load();

        // ── Check startup recovery (Method B — app relaunch after crash) ─────
        var savedState = stateManager.LoadState();
        if (savedState is { IsKeyboardDisabled: true })
        {
            logger.Log("StartupRecovery", savedState.DeviceInstanceId, "RecoveringAfterCrash", null, Environment.UserName);
            var restoreResult = controlService.EnableDevice(savedState.DeviceInstanceId);
            if (restoreResult.IsSuccess)
                stateManager.ClearState();
        }

        var viewModel = new ViewModels.MainViewModel(
            detectionService, controlService, recoveryService,
            stateManager, configService, logger);

        var mainWindow = new MainWindow { DataContext = viewModel };
        mainWindow.Show();
    }

    // ── Unhandled exception safety nets ──────────────────────────────────────

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatalError(e.Exception);
        e.Handled = true;
        Current.Shutdown(1);
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowFatalError(ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        // Log but don't crash for unobserved task exceptions
    }

    private static void ShowFatalError(Exception ex) =>
        MessageBox.Show(
            $"A fatal error occurred:\n\n{ex.Message}\n\nThe application will now close.",
            "Keyboard Cleaner Pro — Fatal Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}
