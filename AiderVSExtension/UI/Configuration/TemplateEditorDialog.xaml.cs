using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AiderVSExtension.Models;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.UI.Configuration
{
    /// <summary>
    /// Template editor dialog for creating and editing configuration templates
    /// </summary>
    public partial class TemplateEditorDialog : Window
    {
        private readonly IConfigurationService _configurationService;
        private ConfigurationTemplate _template;
        private bool _isNewTemplate;

        public ConfigurationTemplate Template
        {
            get => _template;
            private set => _template = value;
        }

        public bool IsNewTemplate => _isNewTemplate;

        public TemplateEditorDialog(
            IConfigurationService configurationService,
            ConfigurationTemplate template = null)
        {
            InitializeComponent();
            
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            
            _template = template ?? new ConfigurationTemplate();
            _isNewTemplate = template == null;

            InitializeDialog();
            LoadTemplate();
        }

        private void InitializeDialog()
        {
            Title = _isNewTemplate ? "Create New Template" : "Edit Template";
            
            // Set default values for new templates
            if (_isNewTemplate)
            {
                VersionTextBox.Text = "1.0.0";
                CategoryComboBox.SelectedIndex = 0; // General
                PromptTemplateRadio.IsChecked = true;
            }
        }

        private void LoadTemplate()
        {
            if (_template == null) return;

            NameTextBox.Text = _template.Name ?? "";
            DescriptionTextBox.Text = _template.Description ?? "";
            CategoryComboBox.Text = _template.Category ?? "";
            AuthorTextBox.Text = _template.Author ?? "";
            VersionTextBox.Text = _template.Version ?? "1.0.0";
            TagsTextBox.Text = _template.Tags != null ? string.Join(", ", _template.Tags) : "";
            ContentTextBox.Text = _template.Content ?? "";
            UsageTextBox.Text = _template.Usage ?? "";

            // Set template type
            if (_template.TemplateType == TemplateType.Configuration)
            {
                ConfigurationTemplateRadio.IsChecked = true;
            }
            else
            {
                PromptTemplateRadio.IsChecked = true;
            }

            // Set options
            IsDefaultCheckBox.IsChecked = _template.IsDefault;
            IsBuiltInCheckBox.IsChecked = _template.IsBuiltIn;
            AllowParametersCheckBox.IsChecked = _template.Parameters?.Any() == true || 
                                                _template.Variables?.Any() == true;

            // Load parameters and variables
            LoadParameters();
        }

        private void LoadParameters()
        {
            ParametersPanel.Children.Clear();

            // Load parameters
            if (_template.Parameters != null)
            {
                foreach (var parameter in _template.Parameters)
                {
                    AddParameterRow(parameter.Key, parameter.Value);
                }
            }

            // Load variables
            if (_template.Variables != null)
            {
                foreach (var variable in _template.Variables)
                {
                    AddVariableRow(variable.Key, variable.Value?.ToString());
                }
            }
        }

        private void AddParameterRow(string name = "", TemplateParameter parameter = null)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };

            var nameTextBox = new TextBox
            {
                Width = 120,
                Margin = new Thickness(0, 0, 5, 0),
                Text = name,
                ToolTip = "Parameter name"
            };

            var descriptionTextBox = new TextBox
            {
                Width = 150,
                Margin = new Thickness(0, 0, 5, 0),
                Text = parameter?.Description ?? "",
                ToolTip = "Parameter description"
            };

            var typeComboBox = new ComboBox
            {
                Width = 80,
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "Parameter type"
            };

            typeComboBox.Items.Add("String");
            typeComboBox.Items.Add("Number");
            typeComboBox.Items.Add("Boolean");
            typeComboBox.Items.Add("List");
            typeComboBox.SelectedValue = parameter?.Type.ToString() ?? "String";

            var defaultValueTextBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(0, 0, 5, 0),
                Text = parameter?.DefaultValue?.ToString() ?? "",
                ToolTip = "Default value"
            };

            var requiredCheckBox = new CheckBox
            {
                Content = "Required",
                Margin = new Thickness(0, 0, 10, 0),
                IsChecked = parameter?.IsRequired ?? false,
                VerticalAlignment = VerticalAlignment.Center
            };

            var removeButton = new Button
            {
                Content = "Remove",
                Width = 60,
                Padding = new Thickness(5, 2, 5, 2)
            };

            removeButton.Click += (sender, e) =>
            {
                ParametersPanel.Children.Remove(panel);
            };

            panel.Children.Add(new Label { Content = "Name:", Width = 50, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(nameTextBox);
            panel.Children.Add(new Label { Content = "Desc:", Width = 40, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(descriptionTextBox);
            panel.Children.Add(new Label { Content = "Type:", Width = 40, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(typeComboBox);
            panel.Children.Add(new Label { Content = "Default:", Width = 50, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(defaultValueTextBox);
            panel.Children.Add(requiredCheckBox);
            panel.Children.Add(removeButton);

            ParametersPanel.Children.Add(panel);
        }

        private void AddVariableRow(string name = "", string value = "")
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };

            var nameTextBox = new TextBox
            {
                Width = 150,
                Margin = new Thickness(0, 0, 5, 0),
                Text = name,
                ToolTip = "Variable name"
            };

            var valueTextBox = new TextBox
            {
                Width = 200,
                Margin = new Thickness(0, 0, 5, 0),
                Text = value,
                ToolTip = "Variable value or expression"
            };

            var removeButton = new Button
            {
                Content = "Remove",
                Width = 60,
                Padding = new Thickness(5, 2, 5, 2)
            };

            removeButton.Click += (sender, e) =>
            {
                ParametersPanel.Children.Remove(panel);
            };

            panel.Children.Add(new Label { Content = "Variable:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(nameTextBox);
            panel.Children.Add(new Label { Content = "Value:", Width = 50, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(valueTextBox);
            panel.Children.Add(removeButton);

            ParametersPanel.Children.Add(panel);
        }

        private bool ValidateInput()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                errors.Add("Template name is required");

            if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
                errors.Add("Template content is required");

            if (string.IsNullOrWhiteSpace(CategoryComboBox.Text))
                errors.Add("Category is required");

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

        private ConfigurationTemplate SaveTemplate()
        {
            if (!ValidateInput())
                return null;

            _template.Name = NameTextBox.Text.Trim();
            _template.Description = DescriptionTextBox.Text.Trim();
            _template.Category = CategoryComboBox.Text.Trim();
            _template.Author = AuthorTextBox.Text.Trim();
            _template.Version = VersionTextBox.Text.Trim();
            _template.Content = ContentTextBox.Text;
            _template.Usage = UsageTextBox.Text.Trim();
            _template.LastModified = DateTime.UtcNow;

            // Set template type
            _template.TemplateType = ConfigurationTemplateRadio.IsChecked == true 
                ? TemplateType.Configuration 
                : TemplateType.Prompt;

            // Set options
            _template.IsDefault = IsDefaultCheckBox.IsChecked ?? false;
            _template.IsBuiltIn = IsBuiltInCheckBox.IsChecked ?? false;

            // Parse tags
            var tagsText = TagsTextBox.Text.Trim();
            _template.Tags = string.IsNullOrEmpty(tagsText) 
                ? new List<string>() 
                : tagsText.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

            // Save parameters and variables
            SaveParametersAndVariables();

            return _template;
        }

        private void SaveParametersAndVariables()
        {
            _template.Parameters = new Dictionary<string, TemplateParameter>();
            _template.Variables = new Dictionary<string, object>();

            foreach (StackPanel panel in ParametersPanel.Children.OfType<StackPanel>())
            {
                var controls = panel.Children.OfType<Control>().ToList();
                
                // Check if this is a parameter row (has ComboBox) or variable row
                var hasComboBox = controls.OfType<ComboBox>().Any();
                
                if (hasComboBox)
                {
                    // Parameter row
                    var nameTextBox = controls.OfType<TextBox>().FirstOrDefault();
                    var descriptionTextBox = controls.OfType<TextBox>().Skip(1).FirstOrDefault();
                    var typeComboBox = controls.OfType<ComboBox>().FirstOrDefault();
                    var defaultValueTextBox = controls.OfType<TextBox>().Skip(2).FirstOrDefault();
                    var requiredCheckBox = controls.OfType<CheckBox>().FirstOrDefault();

                    if (nameTextBox != null && !string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        var parameter = new TemplateParameter
                        {
                            Description = descriptionTextBox?.Text ?? "",
                            Type = Enum.TryParse<ParameterType>(typeComboBox?.SelectedValue?.ToString(), out var type) 
                                ? type : ParameterType.String,
                            DefaultValue = defaultValueTextBox?.Text,
                            IsRequired = requiredCheckBox?.IsChecked ?? false
                        };

                        _template.Parameters[nameTextBox.Text.Trim()] = parameter;
                    }
                }
                else
                {
                    // Variable row
                    var nameTextBox = controls.OfType<TextBox>().FirstOrDefault();
                    var valueTextBox = controls.OfType<TextBox>().Skip(1).FirstOrDefault();

                    if (nameTextBox != null && valueTextBox != null &&
                        !string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        _template.Variables[nameTextBox.Text.Trim()] = valueTextBox.Text ?? "";
                    }
                }
            }
        }

        private void TemplateType_Changed(object sender, RoutedEventArgs e)
        {
            // Could add template type specific logic here
        }

        private void AllowParameters_Changed(object sender, RoutedEventArgs e)
        {
            ParametersTab.IsEnabled = AllowParametersCheckBox.IsChecked ?? false;
        }

        private void AddParameterButton_Click(object sender, RoutedEventArgs e)
        {
            AddParameterRow();
        }

        private void AddVariableButton_Click(object sender, RoutedEventArgs e)
        {
            AddVariableRow();
        }

        private void GeneratePreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tempTemplate = SaveTemplate();
                if (tempTemplate == null)
                    return;

                // Generate a preview by applying sample parameter values
                var preview = tempTemplate.Content;
                
                if (tempTemplate.Parameters?.Any() == true)
                {
                    foreach (var param in tempTemplate.Parameters)
                    {
                        var placeholder = $"{{{param.Key}}}";
                        var sampleValue = param.Value.DefaultValue?.ToString() ?? $"[{param.Key}]";
                        preview = preview.Replace(placeholder, sampleValue);
                    }
                }

                if (tempTemplate.Variables?.Any() == true)
                {
                    foreach (var variable in tempTemplate.Variables)
                    {
                        var placeholder = $"${{{variable.Key}}}";
                        var value = variable.Value?.ToString() ?? "";
                        preview = preview.Replace(placeholder, value);
                    }
                }

                PreviewTextBox.Text = preview;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error generating preview:\n{ex.Message}",
                    "Preview Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var template = SaveTemplate();
                if (template == null)
                    return;

                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"{template.Name}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(saveDialog.FileName, json);
                    
                    MessageBox.Show("Template exported successfully!", "Export Complete", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting template:\n{ex.Message}", "Export Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var importedTemplate = JsonSerializer.Deserialize<ConfigurationTemplate>(json);
                    
                    if (importedTemplate != null)
                    {
                        _template = importedTemplate;
                        LoadTemplate();
                        
                        MessageBox.Show("Template imported successfully!", "Import Complete", 
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing template:\n{ex.Message}", "Import Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var savedTemplate = SaveTemplate();
            if (savedTemplate != null)
            {
                _template = savedTemplate;
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