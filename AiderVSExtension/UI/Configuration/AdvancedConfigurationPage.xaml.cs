using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using Microsoft.Win32;

namespace AiderVSExtension.UI.Configuration
{
    /// <summary>
    /// Interaction logic for AdvancedConfigurationPage.xaml
    /// </summary>
    public partial class AdvancedConfigurationPage : UserControl
    {
        private readonly IAdvancedConfigurationService _configurationService;
        private readonly IErrorHandler _errorHandler;
        private AIProvider _selectedProvider = AIProvider.ChatGPT;
        private bool _isLoading = false;

        public AdvancedConfigurationPage(IAdvancedConfigurationService configurationService, IErrorHandler errorHandler)
        {
            InitializeComponent();
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isLoading = true;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.OnLoaded");
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadDataAsync()
        {
            await LoadProfilesAsync();
            await LoadTemplatesAsync();
            await LoadBackupsAsync();
            await LoadAdvancedParametersAsync();
            await LoadSettingsAsync();
        }

        #region Profile Management

        private async Task LoadProfilesAsync()
        {
            try
            {
                var profiles = await _configurationService.GetProfilesAsync();
                ProfilesDataGrid.ItemsSource = profiles;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.LoadProfilesAsync");
            }
        }

        private async void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ProfileEditorDialog(_configurationService, _errorHandler);
                if (dialog.ShowDialog() == true)
                {
                    await LoadProfilesAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.CreateProfileButton_Click");
                MessageBox.Show($"Error creating profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
                    MessageBox.Show("Please select a profile to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new ProfileEditorDialog(_configurationService, _errorHandler, selectedProfile);
                if (dialog.ShowDialog() == true)
                {
                    await LoadProfilesAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.EditProfileButton_Click");
                MessageBox.Show($"Error editing profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
                    MessageBox.Show("Please select a profile to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (selectedProfile.IsDefault)
                {
                    MessageBox.Show("Cannot delete the default profile.", "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete the profile '{selectedProfile.Name}'?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.DeleteProfileAsync(selectedProfile.Id);
                    await LoadProfilesAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.DeleteProfileButton_Click");
                MessageBox.Show($"Error deleting profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DuplicateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
                    MessageBox.Show("Please select a profile to duplicate.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var newName = $"{selectedProfile.Name} (Copy)";
                await _configurationService.DuplicateProfileAsync(selectedProfile.Id, newName);
                await LoadProfilesAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.DuplicateProfileButton_Click");
                MessageBox.Show($"Error duplicating profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ActivateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
                    MessageBox.Show("Please select a profile to activate.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await _configurationService.ActivateProfileAsync(selectedProfile.Id);
                await LoadProfilesAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ActivateProfileButton_Click");
                MessageBox.Show($"Error activating profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
                    MessageBox.Show("Please select a profile to export.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Profile",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = $"{selectedProfile.Name}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await _configurationService.ExportProfileAsync(selectedProfile.Id, saveFileDialog.FileName, ConfigurationExportFormat.Json);
                    MessageBox.Show("Profile exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ExportProfileButton_Click");
                MessageBox.Show($"Error exporting profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Import Profile",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    await _configurationService.ImportProfileAsync(openFileDialog.FileName, ConfigurationExportFormat.Json);
                    await LoadProfilesAsync();
                    MessageBox.Show("Profile imported successfully.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ImportProfileButton_Click");
                MessageBox.Show($"Error importing profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Template Management

        private async Task LoadTemplatesAsync()
        {
            try
            {
                var templates = await _configurationService.GetTemplatesAsync();
                TemplatesDataGrid.ItemsSource = templates;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.LoadTemplatesAsync");
            }
        }

        private async void CreateTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new TemplateEditorDialog(_configurationService, _errorHandler);
                if (dialog.ShowDialog() == true)
                {
                    await LoadTemplatesAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.CreateTemplateButton_Click");
                MessageBox.Show($"Error creating template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTemplate = TemplatesDataGrid.SelectedItem as ConfigurationTemplate;
                if (selectedTemplate == null)
                {
                    MessageBox.Show("Please select a template to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new TemplateEditorDialog(_configurationService, _errorHandler, selectedTemplate);
                if (dialog.ShowDialog() == true)
                {
                    await LoadTemplatesAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.EditTemplateButton_Click");
                MessageBox.Show($"Error editing template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTemplate = TemplatesDataGrid.SelectedItem as ConfigurationTemplate;
                if (selectedTemplate == null)
                {
                    MessageBox.Show("Please select a template to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (selectedTemplate.IsBuiltIn)
                {
                    MessageBox.Show("Cannot delete built-in templates.", "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete the template '{selectedTemplate.Name}'?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.DeleteTemplateAsync(selectedTemplate.Id);
                    await LoadTemplatesAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.DeleteTemplateButton_Click");
                MessageBox.Show($"Error deleting template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ApplyTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTemplate = TemplatesDataGrid.SelectedItem as ConfigurationTemplate;
                if (selectedTemplate == null)
                {
                    MessageBox.Show("Please select a template to apply.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
                    MessageBox.Show("Please select a profile to apply the template to.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await _configurationService.ApplyTemplateAsync(selectedTemplate.Id, selectedProfile.Id);
                await LoadProfilesAsync();
                await LoadAdvancedParametersAsync();
                MessageBox.Show("Template applied successfully.", "Apply Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ApplyTemplateButton_Click");
                MessageBox.Show($"Error applying template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTemplate = TemplatesDataGrid.SelectedItem as ConfigurationTemplate;
                if (selectedTemplate == null)
                {
                    MessageBox.Show("Please select a template to export.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Template",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = $"{selectedTemplate.Name}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await _configurationService.ExportTemplateAsync(selectedTemplate.Id, saveFileDialog.FileName, ConfigurationExportFormat.Json);
                    MessageBox.Show("Template exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ExportTemplateButton_Click");
                MessageBox.Show($"Error exporting template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Import Template",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    await _configurationService.ImportTemplateAsync(openFileDialog.FileName, ConfigurationExportFormat.Json);
                    await LoadTemplatesAsync();
                    MessageBox.Show("Template imported successfully.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ImportTemplateButton_Click");
                MessageBox.Show($"Error importing template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Advanced Parameters

        private async Task LoadAdvancedParametersAsync()
        {
            try
            {
                var parameters = await _configurationService.GetAdvancedParametersAsync(_selectedProvider);
                UpdateParameterControls(parameters);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.LoadAdvancedParametersAsync");
            }
        }

        private void UpdateParameterControls(AIModelAdvancedParameters parameters)
        {
            if (parameters == null) return;

            _isLoading = true;
            try
            {
                TemperatureSlider.Value = parameters.Temperature;
                TemperatureValue.Text = parameters.Temperature.ToString("F1");
                
                MaxTokensTextBox.Text = parameters.MaxTokens.ToString();
                
                TopPSlider.Value = parameters.TopP;
                TopPValue.Text = parameters.TopP.ToString("F2");
                
                TopKTextBox.Text = parameters.TopK.ToString();
                
                FrequencyPenaltySlider.Value = parameters.FrequencyPenalty;
                FrequencyPenaltyValue.Text = parameters.FrequencyPenalty.ToString("F1");
                
                PresencePenaltySlider.Value = parameters.PresencePenalty;
                PresencePenaltyValue.Text = parameters.PresencePenalty.ToString("F1");
                
                ContextWindowTextBox.Text = parameters.ContextWindow.ToString();
                TimeoutTextBox.Text = parameters.TimeoutSeconds.ToString();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            var selectedItem = ProviderComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag is string providerName)
            {
                if (Enum.TryParse<AIProvider>(providerName, out var provider))
                {
                    _selectedProvider = provider;
                    await LoadAdvancedParametersAsync();
                }
            }
        }

        private async void LoadDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var defaults = await _configurationService.GetDefaultParametersAsync(_selectedProvider, null);
                UpdateParameterControls(defaults);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.LoadDefaultsButton_Click");
                MessageBox.Show($"Error loading defaults: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestParametersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = GetCurrentParameters();
                var result = await _configurationService.TestParametersAsync(_selectedProvider, parameters);
                
                if (result.IsSuccessful)
                {
                    MessageBox.Show($"Parameters test successful!\nResponse time: {result.ResponseTime.TotalMilliseconds:F0}ms\nOutput: {result.Output}", 
                        "Test Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Parameters test failed: {result.ErrorMessage}", 
                        "Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.TestParametersButton_Click");
                MessageBox.Show($"Error testing parameters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ParameterChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;

            try
            {
                // Update display values
                TemperatureValue.Text = TemperatureSlider.Value.ToString("F1");
                TopPValue.Text = TopPSlider.Value.ToString("F2");
                FrequencyPenaltyValue.Text = FrequencyPenaltySlider.Value.ToString("F1");
                PresencePenaltyValue.Text = PresencePenaltySlider.Value.ToString("F1");

                // Save parameters
                var parameters = GetCurrentParameters();
                await _configurationService.SetAdvancedParametersAsync(_selectedProvider, parameters);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ParameterChanged");
            }
        }

        private AIModelAdvancedParameters GetCurrentParameters()
        {
            return new AIModelAdvancedParameters
            {
                Provider = _selectedProvider,
                Temperature = TemperatureSlider.Value,
                MaxTokens = int.TryParse(MaxTokensTextBox.Text, out var maxTokens) ? maxTokens : 2000,
                TopP = TopPSlider.Value,
                TopK = int.TryParse(TopKTextBox.Text, out var topK) ? topK : 40,
                FrequencyPenalty = FrequencyPenaltySlider.Value,
                PresencePenalty = PresencePenaltySlider.Value,
                ContextWindow = int.TryParse(ContextWindowTextBox.Text, out var contextWindow) ? contextWindow : 4096,
                TimeoutSeconds = int.TryParse(TimeoutTextBox.Text, out var timeout) ? timeout : 30
            };
        }

        #endregion

        #region Backup and Restore

        private async Task LoadBackupsAsync()
        {
            try
            {
                var backups = await _configurationService.GetBackupsAsync();
                BackupsDataGrid.ItemsSource = backups;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.LoadBackupsAsync");
            }
        }

        private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backupName = $"Manual backup {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                await _configurationService.CreateBackupAsync(backupName);
                await LoadBackupsAsync();
                MessageBox.Show("Backup created successfully.", "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.CreateBackupButton_Click");
                MessageBox.Show($"Error creating backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedBackup = BackupsDataGrid.SelectedItem as ConfigurationBackup;
                if (selectedBackup == null)
                {
                    MessageBox.Show("Please select a backup to restore.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to restore the backup '{selectedBackup.Name}'?\nThis will overwrite your current configuration.", 
                    "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.RestoreFromBackupAsync(selectedBackup.Id);
                    await LoadDataAsync();
                    MessageBox.Show("Backup restored successfully.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.RestoreBackupButton_Click");
                MessageBox.Show($"Error restoring backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedBackup = BackupsDataGrid.SelectedItem as ConfigurationBackup;
                if (selectedBackup == null)
                {
                    MessageBox.Show("Please select a backup to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete the backup '{selectedBackup.Name}'?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.DeleteBackupAsync(selectedBackup.Id);
                    await LoadBackupsAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.DeleteBackupButton_Click");
                MessageBox.Show($"Error deleting backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AutoBackupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _configurationService.SetAutoBackupAsync(true);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.AutoBackupCheckBox_Checked");
            }
        }

        private async void AutoBackupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _configurationService.SetAutoBackupAsync(false);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.AutoBackupCheckBox_Unchecked");
            }
        }

        private async void RetentionChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (int.TryParse(MaxBackupsTextBox.Text, out var maxBackups) && 
                    int.TryParse(RetentionDaysTextBox.Text, out var retentionDays))
                {
                    await _configurationService.SetBackupRetentionAsync(maxBackups, retentionDays);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.RetentionChanged");
            }
        }

        #endregion

        #region Health Check

        private async void RunHealthCheckButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var healthCheck = await _configurationService.GetHealthCheckAsync();
                
                // TODO: HealthStatusText control missing - HealthStatusText.Text = $"Status: {healthCheck.Status}";
                HealthScoreText.Text = $"Score: {healthCheck.Score}/100";
                
                var details = new System.Text.StringBuilder();
                details.AppendLine($"Health Check Results ({healthCheck.CheckedAt:yyyy-MM-dd HH:mm:ss})");
                details.AppendLine($"Overall Status: {healthCheck.Status}");
                details.AppendLine($"Score: {healthCheck.Score}/100");
                details.AppendLine();
                
                if (healthCheck.Issues.Any())
                {
                    details.AppendLine("Issues:");
                    foreach (var issue in healthCheck.Issues)
                    {
                        details.AppendLine($"  [{issue.Severity}] {issue.Title}");
                        details.AppendLine($"    {issue.Description}");
                        if (!string.IsNullOrEmpty(issue.Resolution))
                            details.AppendLine($"    Resolution: {issue.Resolution}");
                        details.AppendLine();
                    }
                }
                else
                {
                    details.AppendLine("No issues found.");
                }
                
                HealthDetailsText.Text = details.ToString();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.RunHealthCheckButton_Click");
                MessageBox.Show($"Error running health check: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void GetRecommendationsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var recommendations = await _configurationService.GetRecommendationsAsync();
                
                var details = new System.Text.StringBuilder();
                details.AppendLine("Configuration Recommendations:");
                details.AppendLine();
                
                if (recommendations.Any())
                {
                    foreach (var recommendation in recommendations)
                    {
                        details.AppendLine($"[{recommendation.Priority}] {recommendation.Title}");
                        details.AppendLine($"  {recommendation.Description}");
                        details.AppendLine($"  Expected Impact: {recommendation.ExpectedImpact}");
                        details.AppendLine($"  Reason: {recommendation.Reason}");
                        details.AppendLine();
                    }
                }
                else
                {
                    details.AppendLine("No recommendations available.");
                }
                
                HealthDetailsText.Text = details.ToString();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.GetRecommendationsButton_Click");
                MessageBox.Show($"Error getting recommendations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Settings

        private async Task LoadSettingsAsync()
        {
            try
            {
                var autoBackup = _configurationService.GetValue("AutoBackupEnabled", true);
                AutoBackupCheckBox.IsChecked = autoBackup;
                
                var maxBackups = _configurationService.GetValue("MaxBackups", 10);
                MaxBackupsTextBox.Text = maxBackups.ToString();
                
                var retentionDays = _configurationService.GetValue("RetentionDays", 30);
                RetentionDaysTextBox.Text = retentionDays.ToString();
                
                // Set default provider
                ProviderComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.LoadSettingsAsync");
            }
        }

        #endregion

        #region Action Buttons

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Configuration is automatically saved as changes are made
                MessageBox.Show("Configuration saved successfully.", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.SaveButton_Click");
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Are you sure you want to reset all settings to their default values?", 
                    "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.ResetToDefaultsAsync();
                    await LoadDataAsync();
                    MessageBox.Show("Settings reset to defaults.", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ResetButton_Click");
                MessageBox.Show($"Error resetting settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var parent = Parent as Window;
            parent?.Close();
        }

        #endregion
    }
    
    // Placeholder dialog classes - these would be implemented separately
    public partial class ProfileEditorDialog : Window
    {
        public ProfileEditorDialog(IAdvancedConfigurationService configurationService, IErrorHandler errorHandler, ConfigurationProfile profile = null)
        {
            // Implementation would go here
            Title = profile == null ? "Create Profile" : "Edit Profile";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }
    
    public partial class TemplateEditorDialog : Window
    {
        public TemplateEditorDialog(IAdvancedConfigurationService configurationService, IErrorHandler errorHandler, ConfigurationTemplate template = null)
        {
            // Implementation would go here
            Title = template == null ? "Create Template" : "Edit Template";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }
}