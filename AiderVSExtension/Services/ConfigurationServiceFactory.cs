using System;
using AiderVSExtension.Interfaces;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Factory for creating configuration service instances
    /// </summary>
    public static class ConfigurationServiceFactory
    {
        /// <summary>
        /// Creates a new configuration service instance using the Visual Studio settings manager
        /// </summary>
        /// <param name="serviceProvider">The Visual Studio service provider</param>
        /// <returns>A new configuration service instance</returns>
        public static IConfigurationService Create(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var settingsManager = new ShellSettingsManager(serviceProvider);
                var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                
                return new ConfigurationService(settingsStore);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating configuration service: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a configuration service for testing purposes
        /// </summary>
        /// <param name="settingsStore">Mock settings store for testing</param>
        /// <returns>A configuration service instance for testing</returns>
        public static IConfigurationService CreateForTesting(WritableSettingsStore settingsStore)
        {
            return new ConfigurationService(settingsStore);
        }
    }
}
