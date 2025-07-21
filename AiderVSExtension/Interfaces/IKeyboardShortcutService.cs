using System;
using System.Collections.Generic;
using System.Windows.Input;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for keyboard shortcut management and accessibility
    /// </summary>
    public interface IKeyboardShortcutService
    {
        /// <summary>
        /// Event fired when a shortcut is executed
        /// </summary>
        event EventHandler<ShortcutExecutedEventArgs> ShortcutExecuted;

        /// <summary>
        /// Registers a keyboard shortcut
        /// </summary>
        /// <param name="shortcut">Shortcut definition</param>
        /// <param name="action">Action to execute</param>
        void RegisterShortcut(KeyboardShortcut shortcut, Action action);

        /// <summary>
        /// Unregisters a keyboard shortcut
        /// </summary>
        /// <param name="shortcut">Shortcut to unregister</param>
        void UnregisterShortcut(KeyboardShortcut shortcut);

        /// <summary>
        /// Handles key down events
        /// </summary>
        /// <param name="key">Key pressed</param>
        /// <param name="modifiers">Modifier keys</param>
        /// <returns>True if handled</returns>
        bool HandleKeyDown(Key key, ModifierKeys modifiers);

        /// <summary>
        /// Gets all registered shortcuts
        /// </summary>
        /// <returns>List of shortcuts</returns>
        IEnumerable<KeyboardShortcut> GetAllShortcuts();

        /// <summary>
        /// Gets shortcuts by category
        /// </summary>
        /// <param name="category">Shortcut category</param>
        /// <returns>List of shortcuts</returns>
        IEnumerable<KeyboardShortcut> GetShortcutsByCategory(string category);

        /// <summary>
        /// Checks if a shortcut conflicts with existing shortcuts
        /// </summary>
        /// <param name="shortcut">Shortcut to check</param>
        /// <returns>True if conflicts</returns>
        bool HasConflict(KeyboardShortcut shortcut);

        /// <summary>
        /// Gets accessibility features status
        /// </summary>
        /// <returns>Accessibility status</returns>
        AccessibilityStatus GetAccessibilityStatus();

        /// <summary>
        /// Applies accessibility settings to an element
        /// </summary>
        /// <param name="element">Element to apply settings to</param>
        void ApplyAccessibilitySettings(System.Windows.FrameworkElement element);

        /// <summary>
        /// Creates help text for shortcuts
        /// </summary>
        /// <returns>Help text</returns>
        string GetShortcutHelpText();
    }
}