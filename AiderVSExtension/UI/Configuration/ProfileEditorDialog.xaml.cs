using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AiderVSExtension.Models;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.UI.Configuration
{
    /// <summary>
    /// Profile editor dialog for creating and editing configuration profiles
    /// </summary>
    public partial class ProfileEditorDialog : Window
    {
        private readonly IConfigurationService _configurationService;
        private readonly IAIModelManager _aiModelManager;
        private ConfigurationProfile _profile;
        private bool _isNewProfile;

        public ConfigurationProfile Profile
        {
            get => _profile;
            private set => _profile = value;
        }

        public bool IsNewProfile => _isNewProfile;

        public ProfileEditorDialog(
            IConfigurationService configurationService,
            IAIModelManager aiModelManager,
            ConfigurationProfile profile = null)
        {
            InitializeComponent();
            
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _aiModelManager = aiModelManager ?? throw new ArgumentNullException(nameof(aiModelManager));
            
            _profile = profile ?? new ConfigurationProfile();
            _isNewProfile = profile == null;

            InitializeDialog();
            LoadProfile();
        }

        private void InitializeDialog()
        {
            Title = _isNewProfile ? "Create New Profile" : "Edit Profile";
            
            // Set default values for new profiles
            if (_isNewProfile)
            {
                TemperatureTextBox.Text = "0.7";
                MaxTokensTextBox.Text = "1000";
                TimeoutTextBox.Text = "30";
                MaxRetriesTextBox.Text = "3";
                IsEnabledCheckBox.IsChecked = true;
                VersionTextBox.Text = "1.0.0";
            }
        }

        private void LoadProfile()
        {
            if (_profile == null) return;

            NameTextBox.Text = _profile.Name ?? "";
            DescriptionTextBox.Text = _profile.Description ?? "";
            VersionTextBox.Text = _profile.Version ?? "1.0.0";
            AuthorTextBox.Text = _profile.Author ?? "";

            // Load AI provider settings
            if (_profile.AIConfiguration != null)
            {
                ProviderComboBox.SelectedValue = _profile.AIConfiguration.Provider.ToString();
                ModelTextBox.Text = _profile.AIConfiguration.ModelName ?? "";
                EndpointTextBox.Text = _profile.AIConfiguration.EndpointUrl ?? "";
                ApiKeyPasswordBox.Password = _profile.AIConfiguration.ApiKey ?? "";
                TemperatureTextBox.Text = _profile.AIConfiguration.AdditionalSettings
                    ?.GetValueOrDefault("Temperature", "0.7")?.ToString() ?? "0.7";
                MaxTokensTextBox.Text = _profile.AIConfiguration.AdditionalSettings
                    ?.GetValueOrDefault("MaxTokens", "1000")?.ToString() ?? "1000";
                TimeoutTextBox.Text = _profile.AIConfiguration.TimeoutSeconds.ToString();
                MaxRetriesTextBox.Text = _profile.AIConfiguration.MaxRetries.ToString();
                IsEnabledCheckBox.IsChecked = _profile.AIConfiguration.IsEnabled;
            }

            IsDefaultCheckBox.IsChecked = _profile.IsDefault;
            TagsTextBox.Text = _profile.Tags != null ? string.Join(", ", _profile.Tags) : "";

            // Load custom properties
            LoadCustomProperties();
        }

        private void LoadCustomProperties()
        {
            CustomPropertiesPanel.Children.Clear();

            if (_profile.CustomProperties != null)
            {
                foreach (var property in _profile.CustomProperties)
                {
                    AddPropertyRow(property.Key, property.Value?.ToString());
                }
            }
        }

        private void AddPropertyRow(string key = "", string value = "")
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

            var keyTextBox = new TextBox
            {
                Width = 150,
                Margin = new Thickness(0, 0, 5, 0),
                Text = key,
                ToolTip = "Property name"
            };

            var valueTextBox = new TextBox
            {
                Width = 200,
                Margin = new Thickness(0, 0, 5, 0),
                Text = value,
                ToolTip = "Property value"
            };

            var removeButton = new Button
            {
                Content = "Remove",
                Width = 60,
                Padding = new Thickness(5, 2, 5, 2)
            };

            removeButton.Click += (sender, e) =>
            {
                CustomPropertiesPanel.Children.Remove(panel);
            };

            panel.Children.Add(new Label { Content = "Key:", Width = 40, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(keyTextBox);
            panel.Children.Add(new Label { Content = "Value:", Width = 50, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(valueTextBox);
            panel.Children.Add(removeButton);

            CustomPropertiesPanel.Children.Add(panel);
        }

        private bool ValidateInput()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                errors.Add("Profile name is required");

            if (ProviderComboBox.SelectedValue == null)
                errors.Add("AI provider must be selected");

            if (!double.TryParse(TemperatureTextBox.Text, out double temperature) || temperature < 0 || temperature > 2)
                errors.Add("Temperature must be a number between 0 and 2");

            if (!int.TryParse(MaxTokensTextBox.Text, out int maxTokens) || maxTokens <= 0)
                errors.Add("Max tokens must be a positive number");

            if (!int.TryParse(TimeoutTextBox.Text, out int timeout) || timeout <= 0)
                errors.Add("Timeout must be a positive number");

            if (!int.TryParse(MaxRetriesTextBox.Text, out int retries) || retries < 0)
                errors.Add("Max retries must be zero or positive");

            if (errors.Any())
            {
                MessageBox.Show(
                    $"Please fix the following errors:\n\n{string.Join("\n", errors)}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private ConfigurationProfile SaveProfile()
        {
            if (!ValidateInput())
                return null;

            _profile.Name = NameTextBox.Text.Trim();
            _profile.Description = DescriptionTextBox.Text.Trim();
            _profile.Version = VersionTextBox.Text.Trim();
            _profile.Author = AuthorTextBox.Text.Trim();
            _profile.IsDefault = IsDefaultCheckBox.IsChecked ?? false;
            _profile.LastModified = DateTime.UtcNow;

            // Parse tags
            var tagsText = TagsTextBox.Text.Trim();
            _profile.Tags = string.IsNullOrEmpty(tagsText) 
                ? new List<string>() 
                : tagsText.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

            // Save AI configuration
            if (Enum.TryParse<AIProvider>(ProviderComboBox.SelectedValue?.ToString(), out var provider))
            {
                _profile.AIModelConfiguration = new AIModelConfiguration
                {
                    Provider = provider,
                    ModelName = ModelTextBox.Text.Trim(),
                    EndpointUrl = EndpointTextBox.Text.Trim(),
                    ApiKey = ApiKeyPasswordBox.Password,
                    TimeoutSeconds = int.Parse(TimeoutTextBox.Text),
                    MaxRetries = int.Parse(MaxRetriesTextBox.Text),
                    IsEnabled = IsEnabledCheckBox.IsChecked ?? false,
                    AdditionalSettings = new Dictionary<string, object>
                    {
                        ["Temperature"] = double.Parse(TemperatureTextBox.Text),
                        ["MaxTokens"] = int.Parse(MaxTokensTextBox.Text)
                    }
                };
            }

            // Save custom properties
            _profile.CustomProperties = new Dictionary<string, object>();
            foreach (StackPanel panel in CustomPropertiesPanel.Children.OfType<StackPanel>())
            {
                var keyTextBox = panel.Children.OfType<TextBox>().FirstOrDefault();
                var valueTextBox = panel.Children.OfType<TextBox>().Skip(1).FirstOrDefault();

                if (keyTextBox != null && valueTextBox != null &&
                    !string.IsNullOrWhiteSpace(keyTextBox.Text))
                {
                    _profile.CustomProperties[keyTextBox.Text.Trim()] = valueTextBox.Text.Trim();
                }
            }

            return _profile;
        }

        private void AddPropertyButton_Click(object sender, RoutedEventArgs e)
        {
            AddPropertyRow();
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var tempProfile = SaveProfile();
                if (tempProfile?.AIConfiguration == null)
                    return;

                TestConnectionButton.IsEnabled = false;
                TestConnectionButton.Content = "Testing...";

                var result = await _aiModelManager.TestConnectionAsync(tempProfile.AIConfiguration);

                var message = result.IsSuccessfulful
                    ? $"Connection successful!\nResponse time: {result.ResponseTime.TotalMilliseconds:F0}ms"
                    : $"Connection failed:\n{result.ErrorMessage}";

                MessageBox.Show(
                    message,
                    "Connection Test",
                    MessageBoxButton.OK,
                    result.IsSuccessfulful ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Connection test failed:\n{ex.Message}",
                    "Connection Test Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
                TestConnectionButton.Content = "Test Connection";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var savedProfile = SaveProfile();
            if (savedProfile != null)
            {
                _profile = savedProfile;
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}