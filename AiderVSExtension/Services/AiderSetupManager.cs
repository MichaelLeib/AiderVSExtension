using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using AiderVSExtension.Interfaces;
using AiderVSExtension.UI;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for managing Aider setup and dependencies
    /// </summary>
    public class AiderSetupManager : IAiderSetupManager
    {
        private readonly IAiderDependencyChecker _dependencyChecker;
        private readonly IAgentApiService _agentApiService;
        private readonly IErrorHandler _errorHandler;
        private readonly ITelemetryService _telemetryService;

        public AiderSetupManager(
            IAiderDependencyChecker dependencyChecker,
            IAgentApiService agentApiService,
            IErrorHandler errorHandler,
            ITelemetryService telemetryService)
        {
            _dependencyChecker = dependencyChecker ?? throw new ArgumentNullException(nameof(dependencyChecker));
            _agentApiService = agentApiService ?? throw new ArgumentNullException(nameof(agentApiService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        }

        /// <summary>
        /// Checks if Aider dependencies are satisfied
        /// </summary>
        public async Task<bool> AreDependenciesSatisfiedAsync()
        {
            try
            {
                var status = await _dependencyChecker.CheckDependenciesAsync();
                return status.IsPythonInstalled && status.IsAiderInstalled;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error checking dependencies", ex, "AiderSetupManager.AreDependenciesSatisfiedAsync");
                return false;
            }
        }

        /// <summary>
        /// Shows the setup dialog if dependencies are not satisfied
        /// </summary>
        public async Task<bool> EnsureDependenciesAsync()
        {
            try
            {
                var areSatisfied = await AreDependenciesSatisfiedAsync();
                if (areSatisfied)
                {
                    return true;
                }

                return await ShowSetupDialogAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error ensuring dependencies", ex, "AiderSetupManager.EnsureDependenciesAsync");
                return false;
            }
        }

        /// <summary>
        /// Shows the setup dialog regardless of current status
        /// </summary>
        public async Task<bool> ShowSetupDialogAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dialog = new AiderSetupDialog(_dependencyChecker, _errorHandler);
                
                // Try to set owner to VS main window
                try
                {
                    var mainWindow = Application.Current?.MainWindow;
                    if (mainWindow != null)
                    {
                        dialog.Owner = mainWindow;
                    }
                }
                catch
                {
                    // Ignore if we can't set owner
                }

                var result = dialog.ShowDialog();
                var setupCompleted = result == true && dialog.SetupCompleted;

                _telemetryService?.TrackEvent("AiderSetup.DialogShown", new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Result"] = result?.ToString() ?? "null",
                    ["SetupCompleted"] = setupCompleted.ToString()
                });

                return setupCompleted;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error showing setup dialog", ex, "AiderSetupManager.ShowSetupDialogAsync");
                
                // Fallback: show a simple message box
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                MessageBox.Show(
                    "Aider is required but setup failed. Please install Aider manually:\n\n" +
                    "1. Install Python 3.8+ from python.org\n" +
                    "2. Run: pip install aider-chat\n" +
                    "3. Restart Visual Studio",
                    "Aider Setup Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return false;
            }
        }

        /// <summary>
        /// Validates that AgentAPI can be started
        /// </summary>
        public async Task<bool> ValidateAgentApiAsync()
        {
            try
            {
                // First check dependencies
                var dependenciesSatisfied = await AreDependenciesSatisfiedAsync();
                if (!dependenciesSatisfied)
                {
                    await _errorHandler.LogWarningAsync("AgentAPI validation failed: dependencies not satisfied", "AiderSetupManager.ValidateAgentApiAsync");
                    return false;
                }

                // Try to get AgentAPI status
                var status = await _agentApiService.GetStatusAsync();
                
                // If server is not running, try to start it
                if (!_agentApiService.IsRunning)
                {
                    await _errorHandler.LogInfoAsync("Starting AgentAPI server for validation", "AiderSetupManager.ValidateAgentApiAsync");
                    var started = await _agentApiService.StartAsync();
                    
                    if (!started)
                    {
                        await _errorHandler.LogErrorAsync("Failed to start AgentAPI server during validation", null, "AiderSetupManager.ValidateAgentApiAsync");
                        return false;
                    }
                }

                _telemetryService?.TrackEvent("AiderSetup.AgentApiValidated", new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Status"] = status?.Status ?? "unknown",
                    ["IsRunning"] = _agentApiService.IsRunning.ToString()
                });

                return _agentApiService.IsRunning;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error validating AgentAPI", ex, "AiderSetupManager.ValidateAgentApiAsync");
                return false;
            }
        }
    }
}