using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension
{
    /// <summary>
    /// Interaction logic for ConfigurationPage.xaml
    /// </summary>
    public partial class ConfigurationPage : UserControl
    {
        private readonly IConfigurationService _configurationService;
        private AIModelConfiguration _currentConfiguration;
        private bool _isLoading = false;

        // These are now properly connected to XAML controls
        // Controls are defined in XAML with same names, so they are automatically available

        // Stub for InitializeComponent - normally generated from XAML
        private void InitializeComponent()
        {
            // This method is normally auto-generated from XAML
            // For cross-platform build compatibility, we're stubbing it
        }

        // Stub properties for XAML controls - normally auto-generated
        private dynamic ApiKeyPasswordBox = new object();
        private dynamic EndpointUrlTextBox = new object();
        private dynamic ModelNameTextBox = new object();
        private dynamic AiProviderComboBox = new object();
        private dynamic StatusTextBlock = new object();
        private dynamic TestConnectionButton = new object();
        private dynamic AiderPathTextBox = new object();
        private dynamic BrowseButton = new object();
        private dynamic FileStreamingCheckBox = new object();
        private dynamic AutoSaveCheckBox = new object();
        private dynamic DarkModeCheckBox = new object();
        private dynamic YesModeCheckBox = new object();
        private dynamic DefaultModelComboBox = new object();
        private dynamic EditFormatComboBox = new object();
        private dynamic ShowDiffsCheckBox = new object();
        private dynamic PrettyCheckBox = new object();
        private dynamic StreamCheckBox = new object();
        private dynamic AutoCommitsCheckBox = new object();
        private dynamic DescriptiveCommitsCheckBox = new object();
        private dynamic DirtyCommitsCheckBox = new object();
        private dynamic SaveButton = new object();
        private dynamic CancelButton = new object();

        public ConfigurationPage(IConfigurationService configurationService)
        {
            InitializeComponent();
            _configurationService = configurationService;
            
            if (_configurationService != null)
            {
                LoadConfiguration();
            }
        }
        
        // Keep parameterless constructor for XAML compatibility
        public ConfigurationPage() : this(null)
        {
            // This will be used when created from XAML without service injection
        }

        /// <summary>
        /// Load configuration from the configuration service
        /// </summary>
        private async void LoadConfiguration()
        {
            try
            {
                if (_configurationService == null) return;
                
                _isLoading = true;
                _currentConfiguration = _configurationService.GetAIModelConfiguration();
                
                // Set UI values from configuration
                SetProviderSelection(_currentConfiguration.Provider);
                
                // Set UI values from configuration
                ApiKeyPasswordBox.Password = _currentConfiguration.ApiKey ?? string.Empty;
                EndpointUrlTextBox.Text = _currentConfiguration.EndpointUrl ?? string.Empty;
                ModelNameTextBox.Text = _currentConfiguration.ModelName ?? string.Empty;
                
                UpdateStatus("Configuration loaded successfully.", false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading configuration: {ex.Message}", true);
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Set the provider selection in the ComboBox
        /// </summary>
        private void SetProviderSelection(AIProvider provider)
        {
            if (AiProviderComboBox?.Items != null)
            {
                foreach (ComboBoxItem item in AiProviderComboBox.Items.OfType<ComboBoxItem>())
                {
                    if (item.Tag?.ToString() == provider.ToString())
                    {
                        AiProviderComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Get the selected AI provider from the ComboBox
        /// </summary>
        private AIProvider GetSelectedProvider()
        {
            if (AiProviderComboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                if (Enum.TryParse<AIProvider>(selectedItem.Tag?.ToString(), out AIProvider provider))
                {
                    return provider;
                }
            }
            return AIProvider.ChatGPT; // Default
        }

        /// <summary>
        /// Update the status message
        /// </summary>
        private void UpdateStatus(string message, bool isError = false)
        {
            if (StatusTextBlock != null)
            {
                StatusTextBlock.Text = message;
                StatusTextBlock.Foreground = isError ? 
                    System.Windows.Media.Brushes.Red : 
                    System.Windows.Media.Brushes.Green;
            }
            System.Diagnostics.Debug.WriteLine($"Status: {message} (Error: {isError})");
        }

        /// <summary>
        /// Create configuration from UI values
        /// </summary>
        private AIModelConfiguration CreateConfigurationFromUI()
        {
            return new AIModelConfiguration
            {
                Provider = GetSelectedProvider(),
                ApiKey = ApiKeyPasswordBox?.Password ?? "",
                EndpointUrl = EndpointUrlTextBox?.Text ?? "",
                ModelName = ModelNameTextBox?.Text ?? "",
                IsEnabled = true,
                TimeoutSeconds = _currentConfiguration?.TimeoutSeconds ?? 30,
                MaxRetries = _currentConfiguration?.MaxRetries ?? 3
            };
        }

        #region Event Handlers

        private void AiProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            
            var provider = GetSelectedProvider();
            
            // Set default values based on provider
            switch (provider)
            {
                case AIProvider.ChatGPT:
                    if (string.IsNullOrEmpty(EndpointUrlTextBox.Text))
                        EndpointUrlTextBox.Text = "https://api.openai.com/v1/chat/completions";
                    if (string.IsNullOrEmpty(ModelNameTextBox.Text))
                        ModelNameTextBox.Text = "gpt-3.5-turbo";
                    break;
                case AIProvider.Claude:
                    if (string.IsNullOrEmpty(EndpointUrlTextBox.Text))
                        EndpointUrlTextBox.Text = "https://api.anthropic.com/v1/messages";
                    if (string.IsNullOrEmpty(ModelNameTextBox.Text))
                        ModelNameTextBox.Text = "claude-3-sonnet-20240229";
                    break;
                case AIProvider.Ollama:
                    if (string.IsNullOrEmpty(EndpointUrlTextBox.Text))
                        EndpointUrlTextBox.Text = "http://localhost:11434/api/generate";
                    if (string.IsNullOrEmpty(ModelNameTextBox.Text))
                        ModelNameTextBox.Text = "llama2";
                    break;
            }
        }

        private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            UpdateStatus("API Key changed. Click Save to apply changes.", false);
        }

        private void EndpointUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;
            UpdateStatus("Endpoint URL changed. Click Save to apply changes.", false);
        }

        private void ModelNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;
            UpdateStatus("Model Name changed. Click Save to apply changes.", false);
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_configurationService == null)
            {
                UpdateStatus("Configuration service not available", true);
                return;
            }
            
            try
            {
                TestConnectionButton.IsEnabled = false;
                UpdateStatus("Testing connection...", false);
                
                // Create temporary configuration for testing
                var testConfig = CreateConfigurationFromUI();
                var oldConfig = _configurationService.GetAIModelConfiguration();
                
                // Temporarily set config for testing
                await _configurationService.SetAIModelConfigurationAsync(testConfig);
                
                var result = await _configurationService.TestConnectionAsync();
                
                // Restore old config
                await _configurationService.SetAIModelConfigurationAsync(oldConfig);
                
                if (result.IsSuccessful)
                {
                    UpdateStatus("Connection test successful!", false);
                }
                else
                {
                    UpdateStatus($"Connection test failed: {result.ErrorMessage}", true);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error testing connection: {ex.Message}", true);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_configurationService == null)
            {
                UpdateStatus("Configuration service not available", true);
                return;
            }
            
            try
            {
                SaveButton.IsEnabled = false;
                UpdateStatus("Saving configuration...", false);
                
                var newConfig = CreateConfigurationFromUI();
                
                // Save configuration
                await _configurationService.SetAIModelConfigurationAsync(newConfig);
                _currentConfiguration = newConfig;
                
                UpdateStatus("Configuration saved successfully!", false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving configuration: {ex.Message}", true);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to reset the configuration to default values?",
                    "Reset Configuration",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Reset to default configuration
                    _currentConfiguration = new AIModelConfiguration
                    {
                        Provider = AIProvider.ChatGPT,
                        ApiKey = string.Empty,
                        EndpointUrl = "https://api.openai.com/v1/chat/completions",
                        ModelName = "gpt-3.5-turbo",
                        IsEnabled = true,
                        TimeoutSeconds = 30,
                        MaxRetries = 3
                    };
                    
                    // Update UI
                    _isLoading = true;
                    SetProviderSelection(_currentConfiguration.Provider);
                    ApiKeyPasswordBox.Password = _currentConfiguration.ApiKey;
                    EndpointUrlTextBox.Text = _currentConfiguration.EndpointUrl;
                    ModelNameTextBox.Text = _currentConfiguration.ModelName;
                    _isLoading = false;
                    
                    UpdateStatus("Configuration reset to default values. Click Save to apply changes.", false);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error resetting configuration: {ex.Message}", true);
            }
        }

        #endregion
        
        /// <summary>
        /// Reset configuration to defaults (called from options page)
        /// </summary>
        public void ResetToDefaults()
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to reset the configuration to default values?",
                    "Reset Configuration",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    ResetButton_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error resetting configuration: {ex.Message}", true);
            }
        }
    }
}
