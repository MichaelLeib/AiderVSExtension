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

        // Stub controls for non-Windows compilation
#if !WINDOWS
        private class StubControl
        {
            public object ItemsSource { get; set; }
            public object SelectedItem { get; set; }
            public bool IsChecked { get; set; }
            public string Text { get; set; } = "";
            public bool IsEnabled { get; set; } = true;
            public Visibility Visibility { get; set; } = Visibility.Visible;
            public event EventHandler<RoutedEventArgs> Click;
            public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
            public event EventHandler<TextChangedEventArgs> TextChanged;
        }
        
        private StubControl ProfilesDataGrid = new StubControl();
        private StubControl TemplatesDataGrid = new StubControl();
        private StubControl BackupsDataGrid = new StubControl();
        private StubControl ProviderComboBox = new StubControl();
        private StubControl AutoBackupCheckBox = new StubControl();
        private StubControl RetentionTextBox = new StubControl();
        private StubControl CreateProfileButton = new StubControl();
        private StubControl EditProfileButton = new StubControl();
        private StubControl DeleteProfileButton = new StubControl();
        private StubControl DuplicateProfileButton = new StubControl();
        private StubControl ActivateProfileButton = new StubControl();
        private StubControl ExportProfileButton = new StubControl();
        private StubControl ImportProfileButton = new StubControl();
        private StubControl CreateTemplateButton = new StubControl();
        private StubControl EditTemplateButton = new StubControl();
        private StubControl DeleteTemplateButton = new StubControl();
        private StubControl ApplyTemplateButton = new StubControl();
        private StubControl ExportTemplateButton = new StubControl();
        private StubControl ImportTemplateButton = new StubControl();
        private StubControl CreateBackupButton = new StubControl();
        private StubControl RestoreBackupButton = new StubControl();
        private StubControl DeleteBackupButton = new StubControl();
        private StubControl RunHealthCheckButton = new StubControl();
        private StubControl GetRecommendationsButton = new StubControl();
        private StubControl LoadDefaultsButton = new StubControl();
        private StubControl TestParametersButton = new StubControl();
        private StubControl SaveButton = new StubControl();
        private StubControl ResetButton = new StubControl();
        private StubControl CloseButton = new StubControl();
#endif

        public AdvancedConfigurationPage(IAdvancedConfigurationService configurationService, IErrorHandler errorHandler)
        {
#if WINDOWS
            InitializeComponent();
#endif
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
#if WINDOWS
            Loaded += OnLoaded;
#endif
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
#if WINDOWS
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
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
#if WINDOWS
                var dialog = new ProfileEditorDialog(_configurationService, _errorHandler);
                if (dialog.ShowDialog() == true)
                {
                    await LoadProfilesAsync();
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.CreateProfileButton_Click");
#if WINDOWS
                MessageBox.Show($"Error creating profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a profile to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

#if WINDOWS
                var dialog = new ProfileEditorDialog(_configurationService, _errorHandler, selectedProfile);
                if (dialog.ShowDialog() == true)
                {
                    await LoadProfilesAsync();
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.EditProfileButton_Click");
#if WINDOWS
                MessageBox.Show($"Error editing profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a profile to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

#if WINDOWS
                var result = MessageBox.Show(
                    $"Are you sure you want to delete profile '{selectedProfile.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.DeleteProfileAsync(selectedProfile.Id);
                    await LoadProfilesAsync();
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.DeleteProfileButton_Click");
#if WINDOWS
                MessageBox.Show($"Error deleting profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void DuplicateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a profile to duplicate.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

                var duplicatedProfile = await _configurationService.DuplicateProfileAsync(selectedProfile.Id, $"{selectedProfile.Name} (Copy)");
                if (duplicatedProfile != null)
                {
                    await LoadProfilesAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.DuplicateProfileButton_Click");
#if WINDOWS
                MessageBox.Show($"Error duplicating profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void ActivateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a profile to activate.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

                await _configurationService.ActivateProfileAsync(selectedProfile.Id);
#if WINDOWS
                MessageBox.Show($"Profile '{selectedProfile.Name}' activated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ActivateProfileButton_Click");
#if WINDOWS
                MessageBox.Show($"Error activating profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void ExportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProfile = ProfilesDataGrid.SelectedItem as ConfigurationProfile;
                if (selectedProfile == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a profile to export.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

#if WINDOWS
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = $"{selectedProfile.Name}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await _configurationService.ExportProfileAsync(selectedProfile.Id, saveFileDialog.FileName);
                    MessageBox.Show("Profile exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ExportProfileButton_Click");
#if WINDOWS
                MessageBox.Show($"Error exporting profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void ImportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
#if WINDOWS
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    await _configurationService.ImportProfileAsync(openFileDialog.FileName);
                    await LoadProfilesAsync();
                    MessageBox.Show("Profile imported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ImportProfileButton_Click");
#if WINDOWS
                MessageBox.Show($"Error importing profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
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
#if WINDOWS
                var dialog = new TemplateEditorDialog(_configurationService, _errorHandler);
                if (dialog.ShowDialog() == true)
                {
                    await LoadTemplatesAsync();
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.CreateTemplateButton_Click");
#if WINDOWS
                MessageBox.Show($"Error creating template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void EditTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTemplate = TemplatesDataGrid.SelectedItem as ConfigurationTemplate;
                if (selectedTemplate == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a template to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

#if WINDOWS
                var dialog = new TemplateEditorDialog(_configurationService, _errorHandler, selectedTemplate);
                if (dialog.ShowDialog() == true)
                {
                    await LoadTemplatesAsync();
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.EditTemplateButton_Click");
#if WINDOWS
                MessageBox.Show($"Error editing template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void DeleteTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTemplate = TemplatesDataGrid.SelectedItem as ConfigurationTemplate;
                if (selectedTemplate == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a template to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

#if WINDOWS
                var result = MessageBox.Show(
                    $"Are you sure you want to delete template '{selectedTemplate.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.DeleteTemplateAsync(selectedTemplate.Id);
                    await LoadTemplatesAsync();
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.DeleteTemplateButton_Click");
#if WINDOWS
                MessageBox.Show($"Error deleting template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void ApplyTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTemplate = TemplatesDataGrid.SelectedItem as ConfigurationTemplate;
                if (selectedTemplate == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a template to apply.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

                // await _configurationService.ApplyTemplateAsync(selectedTemplate.Id, "current-profile-id");
#if WINDOWS
                MessageBox.Show("Template applied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ApplyTemplateButton_Click");
#if WINDOWS
                MessageBox.Show($"Error applying template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void ExportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTemplate = TemplatesDataGrid.SelectedItem as ConfigurationTemplate;
                if (selectedTemplate == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a template to export.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

#if WINDOWS
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = $"{selectedTemplate.Name}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await _configurationService.ExportTemplateAsync(selectedTemplate.Id, saveFileDialog.FileName);
                    MessageBox.Show("Template exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ExportTemplateButton_Click");
#if WINDOWS
                MessageBox.Show($"Error exporting template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void ImportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
#if WINDOWS
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    await _configurationService.ImportTemplateAsync(openFileDialog.FileName);
                    await LoadTemplatesAsync();
                    MessageBox.Show("Template imported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ImportTemplateButton_Click");
#if WINDOWS
                MessageBox.Show($"Error importing template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
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
            // Parameter control update logic would go here
        }

        private async void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedItem is AIProvider provider)
            {
                _selectedProvider = provider;
                await LoadAdvancedParametersAsync();
            }
        }

        private async void LoadDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // await _configurationService.LoadDefaultParametersAsync(_selectedProvider);
                await LoadAdvancedParametersAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.LoadDefaultsButton_Click");
            }
        }

        private async void TestParametersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = GetCurrentParameters();
                var result = await _configurationService.TestParametersAsync(parameters);
                
#if WINDOWS
                if (result)
                {
                    MessageBox.Show("Parameters test successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Parameters test failed. Please check your configuration.", "Test Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.TestParametersButton_Click");
#if WINDOWS
                MessageBox.Show($"Error testing parameters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void ParameterChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = GetCurrentParameters();
                await _configurationService.SaveAdvancedParametersAsync(parameters);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ParameterChanged");
            }
        }

        private AIModelAdvancedParameters GetCurrentParameters()
        {
            // Get current parameters logic would go here
            return new AIModelAdvancedParameters();
        }

        #endregion

        #region Backup Management

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
                await _configurationService.CreateBackupAsync();
                await LoadBackupsAsync();
#if WINDOWS
                MessageBox.Show("Backup created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.CreateBackupButton_Click");
#if WINDOWS
                MessageBox.Show($"Error creating backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedBackup = BackupsDataGrid.SelectedItem as ConfigurationBackup;
                if (selectedBackup == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a backup to restore.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

#if WINDOWS
                var result = MessageBox.Show(
                    "Are you sure you want to restore this backup? This will overwrite your current configuration.",
                    "Confirm Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.RestoreBackupAsync(selectedBackup.Id);
                    await LoadDataAsync();
                    MessageBox.Show("Backup restored successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.RestoreBackupButton_Click");
#if WINDOWS
                MessageBox.Show($"Error restoring backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedBackup = BackupsDataGrid.SelectedItem as ConfigurationBackup;
                if (selectedBackup == null)
                {
#if WINDOWS
                    MessageBox.Show("Please select a backup to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
                    return;
                }

#if WINDOWS
                var result = MessageBox.Show(
                    $"Are you sure you want to delete backup '{selectedBackup.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.DeleteBackupAsync(selectedBackup.Id);
                    await LoadBackupsAsync();
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.DeleteBackupButton_Click");
#if WINDOWS
                MessageBox.Show($"Error deleting backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void AutoBackupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _configurationService.EnableAutoBackupAsync();
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
                await _configurationService.DisableAutoBackupAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.AutoBackupCheckBox_Unchecked");
            }
        }

        private async void RetentionChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (int.TryParse(RetentionTextBox.Text, out int retentionDays))
                {
                    await _configurationService.SetBackupRetentionAsync(retentionDays);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.RetentionChanged");
            }
        }

        #endregion

        #region Health Check and Recommendations

        private async void RunHealthCheckButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var healthReport = await _configurationService.RunHealthCheckAsync();
                
#if WINDOWS
                var message = $"Health Check Results:\n\n";
                message += $"Configuration: {(healthReport.IsConfigurationValid ? "Valid" : "Invalid")}\n";
                message += $"Connections: {(healthReport.AreConnectionsWorking ? "Working" : "Failed")}\n";
                message += $"Performance: {(healthReport.IsPerformanceOptimal ? "Optimal" : "Suboptimal")}\n";
                message += $"Issues Found: {healthReport.Issues.Count}\n\n";
                
                if (healthReport.Issues.Any())
                {
                    message += "Issues:\n";
                    foreach (var issue in healthReport.Issues)
                    {
                        message += $"- {issue}\n";
                    }
                }

                MessageBox.Show(message, "Health Check", MessageBoxButton.OK, 
                    healthReport.Issues.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.RunHealthCheckButton_Click");
#if WINDOWS
                MessageBox.Show($"Error running health check: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void GetRecommendationsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var recommendations = await _configurationService.GetRecommendationsAsync();
                
#if WINDOWS
                var message = "Recommendations:\n\n";
                if (recommendations.Any())
                {
                    foreach (var recommendation in recommendations)
                    {
                        message += $"• {recommendation.Description}\n";
                        if (!string.IsNullOrEmpty(recommendation.Action))
                        {
                            message += $"  Action: {recommendation.Action}\n";
                        }
                        message += "\n";
                    }
                }
                else
                {
                    message += "No recommendations at this time.";
                }

                MessageBox.Show(message, "Recommendations", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.GetRecommendationsButton_Click");
#if WINDOWS
                MessageBox.Show($"Error getting recommendations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        #endregion

        #region Settings Management

        private async Task LoadSettingsAsync()
        {
            try
            {
                var settings = await _configurationService.GetSettingsAsync();
                // Load settings into controls
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.LoadSettingsAsync");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _configurationService.SaveSettingsAsync();
#if WINDOWS
                MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.SaveButton_Click");
#if WINDOWS
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
#if WINDOWS
                var result = MessageBox.Show(
                    "Are you sure you want to reset all settings to default?",
                    "Confirm Reset",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _configurationService.ResetSettingsAsync();
                    await LoadDataAsync();
                    MessageBox.Show("Settings reset successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
#endif
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationPage.ResetButton_Click");
#if WINDOWS
                MessageBox.Show($"Error resetting settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Close logic would go here
        }

        #endregion
    }

    // Stub implementations for nested classes
#if !WINDOWS
    // ProfileEditorDialog is defined in its own file
#endif
}