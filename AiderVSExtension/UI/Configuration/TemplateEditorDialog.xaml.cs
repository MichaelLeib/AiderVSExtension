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
    /// Interaction logic for TemplateEditorDialog.xaml
    /// </summary>
    public partial class TemplateEditorDialog : Window
    {
        private readonly IAdvancedConfigurationService _configurationService;
        private readonly IErrorHandler _errorHandler;
        private ConfigurationTemplate _template;
        private bool _isLoading = false;

        // Stub controls for non-Windows compilation
#if !WINDOWS
        private class StubControl
        {
            public string Text { get; set; } = "";
            public bool IsChecked { get; set; }
            public object ItemsSource { get; set; }
            public Visibility Visibility { get; set; } = Visibility.Visible;
            public bool IsEnabled { get; set; } = true;
            public event EventHandler<RoutedEventArgs> Click;
        }
        
        private StubControl NameTextBox = new StubControl();
        private StubControl DescriptionTextBox = new StubControl();
        private StubControl CategoryComboBox = new StubControl();
        private StubControl AuthorTextBox = new StubControl();
        private StubControl VersionTextBox = new StubControl();
        private StubControl TagsTextBox = new StubControl();
        private StubControl ContentTextBox = new StubControl();
        private StubControl UsageTextBox = new StubControl();
        private StubControl IsDefaultCheckBox = new StubControl();
        private StubControl IsBuiltInCheckBox = new StubControl();
        private StubControl AllowParametersCheckBox = new StubControl();
        private StubControl ConfigurationTemplateRadio = new StubControl();
        private StubControl PromptTemplateRadio = new StubControl();
        private StubControl ParametersPanel = new StubControl();
        private StubControl VariablesPanel = new StubControl();
        private StubControl SaveButton = new StubControl();
        private StubControl CancelButton = new StubControl();
#endif

        public TemplateEditorDialog(IAdvancedConfigurationService configurationService, IErrorHandler errorHandler, ConfigurationTemplate template = null)
        {
#if WINDOWS
            InitializeComponent();
#endif
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _template = template ?? new ConfigurationTemplate();

#if WINDOWS
            Loaded += TemplateEditorDialog_Loaded;
#endif
        }

        private async void TemplateEditorDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isLoading = true;
                LoadTemplate();
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "TemplateEditorDialog.TemplateEditorDialog_Loaded");
#if WINDOWS
                MessageBox.Show($"Error loading template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadTemplate()
        {
            if (_template == null) return;

            // Basic template loading - simplified for compilation
            NameTextBox.Text = _template.Name ?? "";
            DescriptionTextBox.Text = _template.Description ?? "";
            CategoryComboBox.Text = _template.Category.ToString() ?? "";
            AuthorTextBox.Text = _template.Author ?? "";
            VersionTextBox.Text = _template.Version ?? "1.0.0";
            TagsTextBox.Text = _template.Tags != null ? string.Join(", ", _template.Tags) : "";
            ContentTextBox.Text = "";
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                // var categories = await _configurationService.GetTemplateCategoriesAsync();
                // CategoryComboBox.ItemsSource = categories;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "TemplateEditorDialog.LoadCategoriesAsync");
            }
        }

        private void LoadParameters()
        {
            // ConfigurationTemplate doesn't have Parameters or Variables properties
            // This method is stubbed for now
        }

        private void AddParameterRow(string name, string value)
        {
            // Add parameter row logic would go here
        }

        private void AddVariableRow(string name, string value)
        {
            // Add variable row logic would go here
        }

        private void RemoveParameterRow(object sender, RoutedEventArgs e)
        {
            // Remove parameter row logic would go here
        }

        private void RemoveVariableRow(object sender, RoutedEventArgs e)
        {
            // Remove variable row logic would go here
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
#if WINDOWS
                    MessageBox.Show("Template name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
#endif
                    return;
                }

                // Update template properties
                _template.Name = NameTextBox.Text.Trim();
                _template.Description = DescriptionTextBox.Text?.Trim();
                _template.Author = AuthorTextBox.Text?.Trim();
                _template.Version = VersionTextBox.Text?.Trim() ?? "1.0.0";
                
                // Parse tags
                var tagsText = TagsTextBox.Text?.Trim();
                if (!string.IsNullOrEmpty(tagsText))
                {
                    _template.Tags = tagsText.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                }

                // Save template
                if (_template.Id == null)
                {
                    await _configurationService.CreateTemplateAsync(_template);
                }
                else
                {
                    await _configurationService.UpdateTemplateAsync(_template);
                }

#if WINDOWS
                DialogResult = true;
#endif
                Close();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "TemplateEditorDialog.SaveButton_Click");
#if WINDOWS
                MessageBox.Show($"Error saving template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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