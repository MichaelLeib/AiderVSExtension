using System;
using System.Windows.Input;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Keyboard shortcut definition
    /// </summary>
    public class KeyboardShortcut
    {
        /// <summary>
        /// Shortcut name/identifier
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Primary key
        /// </summary>
        public Key Key { get; set; }

        /// <summary>
        /// Modifier keys
        /// </summary>
        public ModifierKeys Modifiers { get; set; }

        /// <summary>
        /// Shortcut category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Shortcut description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether the shortcut is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether the shortcut is system-defined
        /// </summary>
        public bool IsSystemDefined { get; set; } = false;

        /// <summary>
        /// Context where the shortcut is active
        /// </summary>
        public string Context { get; set; } = "Global";

        /// <summary>
        /// Priority for conflict resolution
        /// </summary>
        public int Priority { get; set; } = 0;

        public override bool Equals(object obj)
        {
            if (obj is KeyboardShortcut other)
            {
                return Key == other.Key && Modifiers == other.Modifiers;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Modifiers);
        }

        public override string ToString()
        {
            return $"{Modifiers}+{Key}";
        }
    }

    /// <summary>
    /// Shortcut executed event arguments
    /// </summary>
    public class ShortcutExecutedEventArgs : EventArgs
    {
        /// <summary>
        /// The shortcut that was executed
        /// </summary>
        public KeyboardShortcut Shortcut { get; set; }

        /// <summary>
        /// Execution timestamp
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Execution context
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Whether execution was successful
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Accessibility status information
    /// </summary>
    public class AccessibilityStatus
    {
        /// <summary>
        /// Whether high contrast mode is enabled
        /// </summary>
        public bool HighContrastEnabled { get; set; }

        /// <summary>
        /// Whether keyboard navigation is enabled
        /// </summary>
        public bool KeyboardNavigationEnabled { get; set; }

        /// <summary>
        /// Whether screen reader support is enabled
        /// </summary>
        public bool ScreenReaderSupported { get; set; }

        /// <summary>
        /// Whether focus visuals are enabled
        /// </summary>
        public bool FocusVisualsEnabled { get; set; }

        /// <summary>
        /// Whether animations are reduced
        /// </summary>
        public bool ReducedAnimations { get; set; }

        /// <summary>
        /// Minimum font size
        /// </summary>
        public double MinimumFontSize { get; set; }

        /// <summary>
        /// Whether system fonts are used
        /// </summary>
        public bool SystemFontsEnabled { get; set; }

        /// <summary>
        /// Focus indicator thickness
        /// </summary>
        public double FocusThickness { get; set; }

        /// <summary>
        /// Current accessibility score (0-100)
        /// </summary>
        public int AccessibilityScore { get; set; }

        /// <summary>
        /// Last accessibility check timestamp
        /// </summary>
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Shortcut conflict information
    /// </summary>
    public class ShortcutConflict
    {
        /// <summary>
        /// Existing shortcut
        /// </summary>
        public KeyboardShortcut ExistingShortcut { get; set; }

        /// <summary>
        /// New shortcut causing conflict
        /// </summary>
        public KeyboardShortcut NewShortcut { get; set; }

        /// <summary>
        /// Conflict severity
        /// </summary>
        public ConflictSeverity Severity { get; set; }

        /// <summary>
        /// Suggested resolution
        /// </summary>
        public string Resolution { get; set; }

        /// <summary>
        /// Whether the conflict can be automatically resolved
        /// </summary>
        public bool CanAutoResolve { get; set; }
    }

    /// <summary>
    /// Conflict severity levels
    /// </summary>
    public enum ConflictSeverity
    {
        /// <summary>
        /// Low severity - shortcuts in different contexts
        /// </summary>
        Low,

        /// <summary>
        /// Medium severity - shortcuts with different priorities
        /// </summary>
        Medium,

        /// <summary>
        /// High severity - direct conflict
        /// </summary>
        High,

        /// <summary>
        /// Critical severity - system shortcut conflict
        /// </summary>
        Critical
    }
}