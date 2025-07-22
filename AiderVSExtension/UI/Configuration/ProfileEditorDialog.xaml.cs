using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.UI.Configuration
{
    /// <summary>
    /// Interaction logic for ProfileEditorDialog.xaml
    /// </summary>
    public partial class ProfileEditorDialog : Window
    {
        private readonly IAdvancedConfigurationService _configurationService;
        private readonly IErrorHandler _errorHandler;
        private ConfigurationProfile _profile;
        private bool _isLoading = false;

        // Stub controls for non-Windows compilation
#if !WINDOWS
        private class StubControl
        {
            public string Text { get; set; } = "";
            public bool IsChecked { get; set; }
            public object ItemsSource { get; set; }
            public object SelectedItem { get; set; }
            public Visibility Visibility { get; set; } = Visibility.Visible;
            public bool IsEnabled { get; set; } = true;
            public event EventHandler<RoutedEventArgs> Click;
        }
        
        private StubControl NameTextBox = new StubControl();
        private StubControl DescriptionTextBox = new StubControl();
        private StubControl ProviderComboBox = new StubControl();
        private StubControl ModelComboBox = new StubControl();
        private StubControl ApiKeyTextBox = new StubControl();
        private StubControl BaseUrlTextBox = new StubControl();
        private StubControl TemperatureTextBox = new StubControl();
        private StubControl MaxTokensTextBox = new StubControl();
        private StubControl TimeoutTextBox = new StubControl();
        private StubControl MaxRetriesTextBox = new StubControl();
        private StubControl IsDefaultCheckBox = new StubControl();
        private StubControl SaveButton = new StubControl();
        private StubControl CancelButton = new StubControl();
#endif

        public ProfileEditorDialog(IAdvancedConfigurationService configurationService, IErrorHandler errorHandler, ConfigurationProfile profile = null)
        {
#if WINDOWS
            InitializeComponent();
#endif
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _profile = profile ?? new ConfigurationProfile();

#if WINDOWS
            Loaded += ProfileEditorDialog_Loaded;
#endif
        }

        private async void ProfileEditorDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isLoading = true;
                LoadProfile();
                await LoadProvidersAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ProfileEditorDialog.ProfileEditorDialog_Loaded");
#if WINDOWS
                MessageBox.Show($"Error loading profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadProfile()
        {
            if (_profile == null) return;

            // Basic profile loading - simplified for compilation
            NameTextBox.Text = _profile.Name ?? "";
            DescriptionTextBox.Text = _profile.Description ?? "";
            ProviderComboBox.SelectedItem = _profile.AIModelConfiguration?.Provider ?? AIProvider.ChatGPT;
            ModelComboBox.Text = _profile.AIModelConfiguration?.ModelName ?? "";
            ApiKeyTextBox.Text = _profile.AIModelConfiguration?.ApiKey ?? "";
            BaseUrlTextBox.Text = _profile.AIModelConfiguration?.EndpointUrl ?? "";
            TemperatureTextBox.Text = "0.7"; // Default value
            MaxTokensTextBox.Text = "2000"; // Default value
            TimeoutTextBox.Text = _profile.AIModelConfiguration?.TimeoutSeconds.ToString() ?? "30";
            MaxRetriesTextBox.Text = _profile.AIModelConfiguration?.MaxRetries.ToString() ?? "3";
            IsDefaultCheckBox.IsChecked = _profile.IsDefault;
        }

        private async Task LoadProvidersAsync()
        {
            try
            {
                var providers = Enum.GetValues(typeof(AIProvider)).Cast<AIProvider>().ToList();
                ProviderComboBox.ItemsSource = providers;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ProfileEditorDialog.LoadProvidersAsync");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
#if WINDOWS
                    MessageBox.Show("Profile name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
#endif
                    return;
                }

                // Update profile properties
                _profile.Name = NameTextBox.Text.Trim();
                _profile.Description = DescriptionTextBox.Text?.Trim();
                
                // Initialize AIModelConfiguration if null
                if (_profile.AIModelConfiguration == null)
                    _profile.AIModelConfiguration = new AIModelConfiguration();
                
                _profile.AIModelConfiguration.Provider = ProviderComboBox.SelectedItem as AIProvider? ?? AIProvider.ChatGPT;
                _profile.AIModelConfiguration.ModelName = ModelComboBox.Text?.Trim();
                _profile.AIModelConfiguration.ApiKey = ApiKeyTextBox.Text?.Trim();
                _profile.AIModelConfiguration.EndpointUrl = BaseUrlTextBox.Text?.Trim();
                
                // Parse numeric values
                if (int.TryParse(TimeoutTextBox.Text, out int timeout))
                    _profile.AIModelConfiguration.TimeoutSeconds = timeout;
                if (int.TryParse(MaxRetriesTextBox.Text, out int maxRetries))
                    _profile.AIModelConfiguration.MaxRetries = maxRetries;
                
                _profile.IsDefault = IsDefaultCheckBox.IsChecked;

                // Save profile
                if (_profile.Id == null)
                {
                    await _configurationService.CreateProfileAsync(_profile);
                }
                else
                {
                    await _configurationService.UpdateProfileAsync(_profile);
                }

#if WINDOWS
                DialogResult = true;
#endif
                Close();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ProfileEditorDialog.SaveButton_Click");
#if WINDOWS
                MessageBox.Show($"Error saving profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
#if WINDOWS
            DialogResult = false;
#endif
            Close();
        }
    }
}