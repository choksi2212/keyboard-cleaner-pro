using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace KeyboardCleanerPro;

/// <summary>
/// Code-behind for <see cref="MainWindow"/>. Handles only UI plumbing —
/// all business logic lives in <see cref="ViewModels.MainViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    private const string GitHubRepoUrl = "https://github.com/choksi2212/keyboard-cleaner-pro";

    public MainWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += (_, _) => DragMove(); // allow drag from anywhere
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Tell the ViewModel to clean up (cancel timer, ensure keyboard is enabled)
        if (DataContext is ViewModels.MainViewModel vm)
            vm.OnWindowClosing();

        Close();
    }

    private void GitHubButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(GitHubRepoUrl) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show($"Navigate to:\n{GitHubRepoUrl}", "GitHub",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
