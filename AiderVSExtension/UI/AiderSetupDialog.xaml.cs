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

        public AiderSetupDialog(IAiderDependencyChecker dependencyChecker, IErrorHandler errorHandler)
        {
            InitializeComponent();
            _dependencyChecker = dependencyChecker ?? throw new ArgumentNullException(nameof(dependencyChecker));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
            Loaded += AiderSetupDialog_Loaded;
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
                MissingDependenciesList.ItemsSource = status.MissingDependencies;
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
                StatusText.Text = "All dependencies are satisfied";
                OkButton.IsEnabled = true;
                SetupCompleted = true;
            }
            else if (!status.IsPythonInstalled)
            {
                StatusText.Text = "Python installation required";
                OkButton.IsEnabled = false;
            }
            else if (!status.IsAiderInstalled)
            {
                StatusText.Text = "Aider installation required";
                OkButton.IsEnabled = false;
            }
            else if (!string.IsNullOrEmpty(status.ErrorMessage))
            {
                StatusText.Text = $"Error: {status.ErrorMessage}";
                OkButton.IsEnabled = false;
            }
        }

        private void SetProgressVisibility(bool visible)
        {
            ProgressPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            InstallationProgress.IsIndeterminate = visible;
        }

        private async void InstallAiderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;

            try
            {
                _isInstalling = true;
                InstallAiderButton.IsEnabled = false;
                SetProgressVisibility(true);
                ProgressText.Text = "Installing Aider...";
                StatusText.Text = "Installing Aider via pip...";

                var success = await _dependencyChecker.InstallAiderAsync();

                if (success)
                {
                    StatusText.Text = "Aider installation completed successfully";
                    await Task.Delay(1000); // Brief pause to show success
                    await CheckDependenciesAsync(); // Refresh status
                }
                else
                {
                    StatusText.Text = "Aider installation failed. Please try manual installation.";
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error installing Aider", ex, "AiderSetupDialog.InstallAiderButton_Click");
                StatusText.Text = $"Installation error: {ex.Message}";
            }
            finally
            {
                _isInstalling = false;
                InstallAiderButton.IsEnabled = true;
                SetProgressVisibility(false);
            }
        }

        private async void UpgradeAiderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;

            try
            {
                _isInstalling = true;
                UpgradeAiderButton.IsEnabled = false;
                SetProgressVisibility(true);
                ProgressText.Text = "Upgrading Aider...";
                StatusText.Text = "Upgrading Aider to latest version...";

                var success = await _dependencyChecker.UpgradeAiderAsync();

                if (success)
                {
                    StatusText.Text = "Aider upgrade completed successfully";
                    await Task.Delay(1000); // Brief pause to show success
                    await CheckDependenciesAsync(); // Refresh status
                }
                else
                {
                    StatusText.Text = "Aider upgrade failed. Please try manual upgrade.";
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error upgrading Aider", ex, "AiderSetupDialog.UpgradeAiderButton_Click");
                StatusText.Text = $"Upgrade error: {ex.Message}";
            }
            finally
            {
                _isInstalling = false;
                UpgradeAiderButton.IsEnabled = true;
                SetProgressVisibility(false);
            }
        }

        private async void CheckAgainButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;
            await CheckDependenciesAsync();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}