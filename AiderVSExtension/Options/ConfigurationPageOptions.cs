using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Microsoft.VisualStudio.Shell;
using AiderVSExtension.UI;
using AiderVSExtension.Services;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Options
{
    /// <summary>
    /// Options page for Aider VS Extension configuration
    /// </summary>
    [ComVisible(true)]
    [Guid("12345678-1234-1234-1234-123456789012")]
    public class ConfigurationPageOptions : DialogPage
    {
        private ConfigurationPage _configurationPage;
        private IConfigurationService _configurationService;

        /// <summary>
        /// Gets the WPF control for the options page
        /// </summary>
        protected override System.Windows.Forms.IWin32Window Window
        {
            get
            {
                if (_configurationPage == null)
                {
                    InitializeConfigurationPage();
                }
                var elementHost = new ElementHost { Child = _configurationPage };
                var userControl = new UserControl();
                userControl.Controls.Add(elementHost);
                elementHost.Dock = DockStyle.Fill;
                return userControl;
            }
        }
        
        /// <summary>
        /// Initializes the options page
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
            if (_configurationPage == null)
            {
                InitializeConfigurationPage();
            }
            base.LoadSettingsFromStorage();
        }

        /// <summary>
        /// Initializes the configuration page with the configuration service
        /// </summary>
        private void InitializeConfigurationPage()
        {
            try
            {
                // Get the package instance
                var package = GetService(typeof(AiderVSExtensionPackage)) as AiderVSExtensionPackage;
                if (package != null)
                {
                    // Get the service container from the package
                    var serviceContainer = package.GetServiceContainer();
                    if (serviceContainer != null)
                    {
                        // Get the configuration service
                        _configurationService = serviceContainer.GetService<IConfigurationService>();
                    }
                }

                // Create the configuration page with the service
                _configurationPage = new ConfigurationPage(_configurationService);
            }
            catch (Exception ex)
            {
                // Log error and create page without service
                System.Diagnostics.Debug.WriteLine($"Failed to initialize configuration service: {ex.Message}");
                _configurationPage = new ConfigurationPage(null);
            }
        }

        /// <summary>
        /// Called when the user clicks OK in the options dialog
        /// </summary>
        public override void SaveSettingsToStorage()
        {
            try
            {
                // The ConfigurationPage handles saving through its own event handlers
                // We don't need to do anything here as the WPF control manages its own state
                base.SaveSettingsToStorage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }


        /// <summary>
        /// Called when the user clicks the Reset button
        /// </summary>
        public override void ResetSettings()
        {
            try
            {
                // Reset the configuration page if it exists
                if (_configurationPage != null)
                {
                    _configurationPage.ResetToDefaults();
                }
                base.ResetSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting settings: {ex.Message}");
            }
        }
    }
}
