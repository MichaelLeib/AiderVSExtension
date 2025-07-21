using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using System.Threading.Tasks;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for managing keyboard shortcuts and accessibility
    /// </summary>
    public class KeyboardShortcutService : IKeyboardShortcutService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IVSThemingService _themingService;
        private readonly Dictionary<string, KeyboardShortcut> _shortcuts = new Dictionary<string, KeyboardShortcut>();
        private readonly Dictionary<string, Action> _shortcutActions = new Dictionary<string, Action>();
        private bool _disposed = false;

        public event EventHandler<ShortcutExecutedEventArgs> ShortcutExecuted;

        public KeyboardShortcutService(IErrorHandler errorHandler, IVSThemingService themingService)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _themingService = themingService ?? throw new ArgumentNullException(nameof(themingService));
            
            InitializeDefaultShortcuts();
        }

        /// <summary>
        /// Registers a keyboard shortcut
        /// </summary>
        /// <param name="shortcut">Shortcut definition</param>
        /// <param name="action">Action to execute</param>
        public void RegisterShortcut(KeyboardShortcut shortcut, Action action)
        {
            try
            {
                if (shortcut == null || action == null)
                    return;

                var key = GetShortcutKey(shortcut);
                _shortcuts[key] = shortcut;
                _shortcutActions[key] = action;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "KeyboardShortcutService.RegisterShortcut");
            }
        }

        /// <summary>
        /// Unregisters a keyboard shortcut
        /// </summary>
        /// <param name="shortcut">Shortcut to unregister</param>
        public void UnregisterShortcut(KeyboardShortcut shortcut)
        {
            try
            {
                if (shortcut == null)
                    return;

                var key = GetShortcutKey(shortcut);
                _shortcuts.Remove(key);
                _shortcutActions.Remove(key);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "KeyboardShortcutService.UnregisterShortcut");
            }
        }

        /// <summary>
        /// Handles key down events
        /// </summary>
        /// <param name="key">Key pressed</param>
        /// <param name="modifiers">Modifier keys</param>
        /// <returns>True if handled</returns>
        public bool HandleKeyDown(Key key, ModifierKeys modifiers)
        {
            try
            {
                var shortcut = new KeyboardShortcut
                {
                    Key = key,
                    Modifiers = modifiers
                };

                var shortcutKey = GetShortcutKey(shortcut);
                
                if (_shortcutActions.TryGetValue(shortcutKey, out var action))
                {
                    action.Invoke();
                    
                    ShortcutExecuted?.Invoke(this, new ShortcutExecutedEventArgs
                    {
                        Shortcut = _shortcuts[shortcutKey],
                        ExecutedAt = DateTime.UtcNow
                    });
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "KeyboardShortcutService.HandleKeyDown");
            }

            return false;
        }

        /// <summary>
        /// Gets all registered shortcuts
        /// </summary>
        /// <returns>List of shortcuts</returns>
        public IEnumerable<KeyboardShortcut> GetAllShortcuts()
        {
            return _shortcuts.Values.ToList();
        }

        /// <summary>
        /// Gets shortcuts by category
        /// </summary>
        /// <param name="category">Shortcut category</param>
        /// <returns>List of shortcuts</returns>
        public IEnumerable<KeyboardShortcut> GetShortcutsByCategory(string category)
        {
            return _shortcuts.Values.Where(s => s.Category == category).ToList();
        }

        /// <summary>
        /// Checks if a shortcut conflicts with existing shortcuts
        /// </summary>
        /// <param name="shortcut">Shortcut to check</param>
        /// <returns>True if conflicts</returns>
        public bool HasConflict(KeyboardShortcut shortcut)
        {
            var key = GetShortcutKey(shortcut);
            return _shortcuts.ContainsKey(key);
        }

        /// <summary>
        /// Gets accessibility features status
        /// </summary>
        /// <returns>Accessibility status</returns>
        public AccessibilityStatus GetAccessibilityStatus()
        {
            try
            {
                var settings = _themingService.GetAccessibilitySettings();
                
                return new AccessibilityStatus
                {
                    HighContrastEnabled = settings.HighContrast,
                    KeyboardNavigationEnabled = settings.IsKeyboardNavigationEnabled,
                    ScreenReaderSupported = settings.IsScreenReaderSupported,
                    FocusVisualsEnabled = settings.ShowFocusVisuals,
                    ReducedAnimations = settings.ReduceAnimations,
                    MinimumFontSize = settings.MinimumFontSize,
                    SystemFontsEnabled = settings.UseSystemFonts,
                    FocusThickness = settings.FocusThickness
                };
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "KeyboardShortcutService.GetAccessibilityStatus");
                return new AccessibilityStatus();
            }
        }

        /// <summary>
        /// Applies accessibility settings to an element
        /// </summary>
        /// <param name="element">Element to apply settings to</param>
        public void ApplyAccessibilitySettings(System.Windows.FrameworkElement element)
        {
            try
            {
                if (element == null)
                    return;

                var settings = _themingService.GetAccessibilitySettings();
                
                // Apply focus visuals
                if (settings.ShowFocusVisuals)
                {
                    element.FocusVisualStyle = CreateFocusVisualStyle(settings.FocusThickness);
                }

                // Apply minimum font size
                if (element is System.Windows.Controls.Control control)
                {
                    if (control.FontSize < settings.MinimumFontSize)
                    {
                        control.FontSize = settings.MinimumFontSize;
                    }
                }

                // Apply high contrast colors
                if (settings.UseHighContrastColors)
                {
                    _themingService.ApplyTheme(element);
                }

                // Set keyboard navigation
                if (settings.IsKeyboardNavigationEnabled)
                {
                    element.IsTabStop = true;
                    System.Windows.Input.KeyboardNavigation.SetTabNavigation(element, System.Windows.Input.KeyboardNavigationMode.Local);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "KeyboardShortcutService.ApplyAccessibilitySettings");
            }
        }

        /// <summary>
        /// Creates help text for shortcuts
        /// </summary>
        /// <returns>Help text</returns>
        public string GetShortcutHelpText()
        {
            try
            {
                var help = new System.Text.StringBuilder();
                help.AppendLine("Keyboard Shortcuts:\n");

                var categories = _shortcuts.Values.GroupBy(s => s.Category);
                
                foreach (var category in categories.OrderBy(c => c.Key))
                {
                    help.AppendLine($"{category.Key}:");
                    
                    foreach (var shortcut in category.OrderBy(s => s.Name))
                    {
                        var keyText = GetShortcutDisplayText(shortcut);
                        help.AppendLine($"  {keyText} - {shortcut.Description}");
                    }
                    
                    help.AppendLine();
                }

                return help.ToString();
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "KeyboardShortcutService.GetShortcutHelpText");
                return "Error loading shortcuts help.";
            }
        }

        #region Private Methods

        private void InitializeDefaultShortcuts()
        {
            // Chat shortcuts
            RegisterShortcut(new KeyboardShortcut
            {
                Name = "OpenChat",
                Key = Key.T,
                Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Category = "Chat",
                Description = "Open Aider chat window"
            }, () => OpenChatWindow());

            RegisterShortcut(new KeyboardShortcut
            {
                Name = "SendMessage",
                Key = Key.Enter,
                Modifiers = ModifierKeys.Control,
                Category = "Chat",
                Description = "Send chat message"
            }, () => SendChatMessage());

            RegisterShortcut(new KeyboardShortcut
            {
                Name = "ClearChat",
                Key = Key.L,
                Modifiers = ModifierKeys.Control,
                Category = "Chat",
                Description = "Clear chat history"
            }, () => ClearChatHistory());

            // File shortcuts
            RegisterShortcut(new KeyboardShortcut
            {
                Name = "AddFileToChat",
                Key = Key.A,
                Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Category = "File",
                Description = "Add current file to chat"
            }, () => AddCurrentFileToChat());

            RegisterShortcut(new KeyboardShortcut
            {
                Name = "AddSelectionToChat",
                Key = Key.S,
                Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Category = "File",
                Description = "Add selection to chat"
            }, () => AddSelectionToChat());

            // AI shortcuts
            RegisterShortcut(new KeyboardShortcut
            {
                Name = "FixWithAider",
                Key = Key.F,
                Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Category = "AI",
                Description = "Fix error with Aider"
            }, () => FixWithAider());

            RegisterShortcut(new KeyboardShortcut
            {
                Name = "ShowCompletion",
                Key = Key.Space,
                Modifiers = ModifierKeys.Control,
                Category = "AI",
                Description = "Show AI completion"
            }, () => ShowAICompletion());

            // Navigation shortcuts
            RegisterShortcut(new KeyboardShortcut
            {
                Name = "FocusChat",
                Key = Key.F1,
                Modifiers = ModifierKeys.Alt,
                Category = "Navigation",
                Description = "Focus chat input"
            }, () => FocusChatInput());

            RegisterShortcut(new KeyboardShortcut
            {
                Name = "FocusEditor",
                Key = Key.Escape,
                Modifiers = ModifierKeys.None,
                Category = "Navigation",
                Description = "Focus editor from chat"
            }, () => FocusEditor());

            // Accessibility shortcuts
            RegisterShortcut(new KeyboardShortcut
            {
                Name = "ShowHelp",
                Key = Key.F1,
                Modifiers = ModifierKeys.None,
                Category = "Accessibility",
                Description = "Show keyboard shortcuts help"
            }, () => ShowHelp());

            RegisterShortcut(new KeyboardShortcut
            {
                Name = "ToggleHighContrast",
                Key = Key.H,
                Modifiers = ModifierKeys.Control | ModifierKeys.Alt,
                Category = "Accessibility",
                Description = "Toggle high contrast mode"
            }, () => ToggleHighContrast());
        }

        private string GetShortcutKey(KeyboardShortcut shortcut)
        {
            return $"{shortcut.Modifiers}+{shortcut.Key}";
        }

        private string GetShortcutDisplayText(KeyboardShortcut shortcut)
        {
            var parts = new List<string>();
            
            if (shortcut.Modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (shortcut.Modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (shortcut.Modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (shortcut.Modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");
            
            parts.Add(shortcut.Key.ToString());
            
            return string.Join(" + ", parts);
        }

        private System.Windows.Style CreateFocusVisualStyle(double thickness)
        {
            var style = new System.Windows.Style();
            
            var setter = new System.Windows.Setter
            {
                Property = System.Windows.Controls.Control.TemplateProperty,
                Value = CreateFocusVisualTemplate(thickness)
            };
            
            style.Setters.Add(setter);
            return style;
        }

        private System.Windows.Controls.ControlTemplate CreateFocusVisualTemplate(double thickness)
        {
            var template = new System.Windows.Controls.ControlTemplate();
            
            var factory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Shapes.Rectangle));
            factory.SetValue(System.Windows.Shapes.Rectangle.StrokeProperty, System.Windows.SystemColors.ControlTextBrush);
            factory.SetValue(System.Windows.Shapes.Rectangle.StrokeThicknessProperty, thickness);
            factory.SetValue(System.Windows.Shapes.Rectangle.StrokeDashArrayProperty, new System.Windows.Media.DoubleCollection { 1, 2 });
            factory.SetValue(System.Windows.Shapes.Rectangle.FillProperty, System.Windows.Media.Brushes.Transparent);
            
            template.VisualTree = factory;
            return template;
        }

        // Action implementations
        private void OpenChatWindow()
        {
            // Implementation would trigger chat window opening
        }

        private void SendChatMessage()
        {
            // Implementation would send current chat message
        }

        private void ClearChatHistory()
        {
            // Implementation would clear chat history
        }

        private void AddCurrentFileToChat()
        {
            // Implementation would add current file to chat
        }

        private void AddSelectionToChat()
        {
            // Implementation would add selection to chat
        }

        private void FixWithAider()
        {
            // Implementation would trigger fix with Aider
        }

        private void ShowAICompletion()
        {
            // Implementation would show AI completion
        }

        private void FocusChatInput()
        {
            // Implementation would focus chat input
        }

        private void FocusEditor()
        {
            // Implementation would focus editor
        }

        private void ShowHelp()
        {
            // Implementation would show help dialog
        }

        private void ToggleHighContrast()
        {
            // Implementation would toggle high contrast mode
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _shortcuts.Clear();
                _shortcutActions.Clear();
                _disposed = true;
            }
        }
    }
}