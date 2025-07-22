using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.UI
{
    /// <summary>
    /// Setup dialog for checking and installing Aider dependencies
    /// </summary>
    public partial class AiderSetupDialog : Window
    {
        private readonly IAiderDependencyChecker _dependencyChecker;
        private readonly IErrorHandler _errorHandler;
        private AiderDependencyStatus _currentStatus;
        private bool _isInstalling = false;

        public bool SetupCompleted { get; private set; } = false;

        // Stub controls for non-Windows compilation
#if !WINDOWS
        private class StubControl
        {
            public string Text { get; set; } = "";
            public Visibility Visibility { get; set; } = Visibility.Visible;
            public Brush Fill { get; set; } = new SolidColorBrush(Colors.Gray);
        }
        
        private StubControl StatusText = new StubControl();
        private StubControl PythonStatusIndicator = new StubControl();
        private StubControl PythonStatusText = new StubControl();
        private StubControl PythonVersionText = new StubControl();
        private StubControl AiderStatusIndicator = new StubControl();
        private StubControl AiderStatusText = new StubControl();
        private StubControl AiderVersionText = new StubControl();
        private StubControl InstallAiderButton = new StubControl();
        private StubControl UpgradeAiderButton = new StubControl();
        private StubControl DependencyPanel = new StubControl();
        private StubControl MissingDependenciesList = new StubControl();
        private StubControl OkButton = new StubControl();
        private StubControl CancelButton = new StubControl();
        private StubControl CheckAgainButton = new StubControl();
        private StubControl ProgressBar = new StubControl();
#endif

        public AiderSetupDialog(IAiderDependencyChecker dependencyChecker, IErrorHandler errorHandler)
        {
#if WINDOWS
            InitializeComponent();
#endif
            _dependencyChecker = dependencyChecker ?? throw new ArgumentNullException(nameof(dependencyChecker));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
#if WINDOWS
            Loaded += AiderSetupDialog_Loaded;
#endif
        }

        private async void AiderSetupDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckDependenciesAsync();
        }

        private async Task CheckDependenciesAsync()
        {
            try
            {
                StatusText.Text = "Checking dependencies...";
                SetProgressVisibility(true);

                _currentStatus = await _dependencyChecker.CheckDependenciesAsync();

                UpdatePythonStatus(_currentStatus);
                UpdateAiderStatus(_currentStatus);
                UpdateMissingDependencies(_currentStatus);
                UpdateOverallStatus(_currentStatus);

                SetProgressVisibility(false);
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error checking dependencies", ex, "AiderSetupDialog.CheckDependenciesAsync");
                StatusText.Text = $"Error checking dependencies: {ex.Message}";
                SetProgressVisibility(false);
            }
        }

        private void UpdatePythonStatus(AiderDependencyStatus status)
        {
            if (status.IsPythonInstalled)
            {
                PythonStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                PythonStatusText.Text = "Python is installed";
                PythonVersionText.Text = $"Version: {status.PythonVersion}";
                PythonVersionText.Visibility = Visibility.Visible;
            }
            else
            {
                PythonStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                PythonStatusText.Text = "Python is not installed";
                PythonVersionText.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateAiderStatus(AiderDependencyStatus status)
        {
            if (!status.IsPythonInstalled)
            {
                AiderStatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
                AiderStatusText.Text = "Python required first";
                AiderVersionText.Visibility = Visibility.Collapsed;
                InstallAiderButton.Visibility = Visibility.Collapsed;
                UpgradeAiderButton.Visibility = Visibility.Collapsed;
            }
            else if (status.IsAiderInstalled)
            {
                AiderStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                AiderStatusText.Text = "Aider is installed";
                AiderVersionText.Text = $"Version: {status.AiderVersion}";
                AiderVersionText.Visibility = Visibility.Visible;
                InstallAiderButton.Visibility = Visibility.Collapsed;
                UpgradeAiderButton.Visibility = Visibility.Visible;
            }
            else
            {
                AiderStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                AiderStatusText.Text = "Aider is not installed";
                AiderVersionText.Visibility = Visibility.Collapsed;
                InstallAiderButton.Visibility = Visibility.Visible;
                UpgradeAiderButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateMissingDependencies(AiderDependencyStatus status)
        {
            if (status.MissingDependencies?.Count > 0)
            {
                DependencyPanel.Visibility = Visibility.Visible;
                // MissingDependenciesList.ItemsSource = status.MissingDependencies;
            }
            else
            {
                DependencyPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateOverallStatus(AiderDependencyStatus status)
        {
            if (status.IsPythonInstalled && status.IsAiderInstalled)
            {
                StatusText.Text = "All dependencies are installed and ready!";
                OkButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                StatusText.Text = "Some dependencies need to be installed.";
                OkButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Visible;
            }
        }

        private void SetProgressVisibility(bool visible)
        {
            ProgressBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void InstallAiderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;

            try
            {
                _isInstalling = true;
                InstallAiderButton.Visibility = Visibility.Collapsed;
                StatusText.Text = "Installing Aider...";
                SetProgressVisibility(true);

                var result = await _dependencyChecker.InstallAiderAsync();
                if (result)
                {
                    StatusText.Text = "Aider installed successfully!";
                    await CheckDependenciesAsync();
                }
                else
                {
                    StatusText.Text = "Failed to install Aider. Please check the error log.";
                    InstallAiderButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error installing Aider", ex, "AiderSetupDialog.InstallAiderButton_Click");
                StatusText.Text = $"Error installing Aider: {ex.Message}";
                InstallAiderButton.Visibility = Visibility.Visible;
            }
            finally
            {
                _isInstalling = false;
                SetProgressVisibility(false);
            }
        }

        private async void UpgradeAiderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;

            try
            {
                _isInstalling = true;
                UpgradeAiderButton.Visibility = Visibility.Collapsed;
                StatusText.Text = "Upgrading Aider...";
                SetProgressVisibility(true);

                var result = await _dependencyChecker.UpgradeAiderAsync();
                if (result)
                {
                    StatusText.Text = "Aider upgraded successfully!";
                    await CheckDependenciesAsync();
                }
                else
                {
                    StatusText.Text = "Failed to upgrade Aider. Please check the error log.";
                    UpgradeAiderButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error upgrading Aider", ex, "AiderSetupDialog.UpgradeAiderButton_Click");
                StatusText.Text = $"Error upgrading Aider: {ex.Message}";
                UpgradeAiderButton.Visibility = Visibility.Visible;
            }
            finally
            {
                _isInstalling = false;
                SetProgressVisibility(false);
            }
        }

        private async void CheckAgainButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckDependenciesAsync();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCompleted = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCompleted = false;
            Close();
        }
    }
}