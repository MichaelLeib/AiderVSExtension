using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// UI context information
    /// </summary>
    public class UIContext
    {
        /// <summary>
        /// Context name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Context type
        /// </summary>
        public ContextType Type { get; set; }

        /// <summary>
        /// Context properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Last update time
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Context priority
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether the context is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Context metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Context types
    /// </summary>
    public enum ContextType
    {
        /// <summary>
        /// Default context
        /// </summary>
        Default,

        /// <summary>
        /// Coding context
        /// </summary>
        Coding,

        /// <summary>
        /// Debugging context
        /// </summary>
        Debugging,

        /// <summary>
        /// Git context
        /// </summary>
        Git,

        /// <summary>
        /// Project context
        /// </summary>
        Project,

        /// <summary>
        /// Testing context
        /// </summary>
        Testing,

        /// <summary>
        /// Configuration context
        /// </summary>
        Configuration,

        /// <summary>
        /// Custom context
        /// </summary>
        Custom
    }

    /// <summary>
    /// Context information from IDE
    /// </summary>
    public class ContextInfo
    {
        /// <summary>
        /// Currently active file
        /// </summary>
        public string ActiveFile { get; set; }

        /// <summary>
        /// Selected text
        /// </summary>
        public string SelectedText { get; set; }

        /// <summary>
        /// Caret position
        /// </summary>
        public int CaretPosition { get; set; }

        /// <summary>
        /// Line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Whether file has errors
        /// </summary>
        public bool HasErrors { get; set; }

        /// <summary>
        /// Whether file has warnings
        /// </summary>
        public bool HasWarnings { get; set; }

        /// <summary>
        /// Whether file is modified
        /// </summary>
        public bool IsModified { get; set; }

        /// <summary>
        /// Whether debugging is active
        /// </summary>
        public bool IsDebugging { get; set; }

        /// <summary>
        /// Whether build is in progress
        /// </summary>
        public bool IsBuilding { get; set; }

        /// <summary>
        /// Current project path
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// Current solution path
        /// </summary>
        public string SolutionPath { get; set; }

        /// <summary>
        /// Active configuration
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Active platform
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Additional context properties
        /// </summary>
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Configuration for context-aware UI elements
    /// </summary>
    public class ContextAwareConfiguration
    {
        /// <summary>
        /// Visibility rules
        /// </summary>
        public List<ContextRule> VisibilityRules { get; set; } = new List<ContextRule>();

        /// <summary>
        /// Property updates
        /// </summary>
        public List<PropertyUpdate> PropertyUpdates { get; set; } = new List<PropertyUpdate>();

        /// <summary>
        /// Styling changes
        /// </summary>
        public List<ContextStyling> Styling { get; set; } = new List<ContextStyling>();

        /// <summary>
        /// Custom behaviors
        /// </summary>
        public List<ContextBehavior> Behaviors { get; set; } = new List<ContextBehavior>();

        /// <summary>
        /// Update frequency
        /// </summary>
        public TimeSpan UpdateFrequency { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Whether to animate changes
        /// </summary>
        public bool AnimateChanges { get; set; } = true;

        /// <summary>
        /// Animation duration
        /// </summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(300);
    }

    /// <summary>
    /// Context rule for conditional UI updates
    /// </summary>
    public class ContextRule
    {
        /// <summary>
        /// Rule conditions
        /// </summary>
        public List<ContextCondition> Conditions { get; set; } = new List<ContextCondition>();

        /// <summary>
        /// Logical operator between conditions
        /// </summary>
        public LogicalOperator Operator { get; set; } = LogicalOperator.And;

        /// <summary>
        /// Rule name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Rule description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Context condition
    /// </summary>
    public class ContextCondition
    {
        /// <summary>
        /// Property name to check
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Comparison operator
        /// </summary>
        public ConditionOperator Operator { get; set; }

        /// <summary>
        /// Value to compare against
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Whether to negate the condition
        /// </summary>
        public bool Negate { get; set; } = false;
    }

    /// <summary>
    /// Property update for context-aware elements
    /// </summary>
    public class PropertyUpdate
    {
        /// <summary>
        /// Property name to update
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// New property value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Condition for the update
        /// </summary>
        public ContextRule Condition { get; set; }

        /// <summary>
        /// Whether to animate the property change
        /// </summary>
        public bool AnimateChange { get; set; } = true;

        /// <summary>
        /// Animation duration
        /// </summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(300);
    }

    /// <summary>
    /// Context styling configuration
    /// </summary>
    public class ContextStyling
    {
        /// <summary>
        /// Condition for applying this styling
        /// </summary>
        public ContextRule Condition { get; set; }

        /// <summary>
        /// Style to apply
        /// </summary>
        public Style Style { get; set; }

        /// <summary>
        /// Resources to add
        /// </summary>
        public Dictionary<object, object> Resources { get; set; } = new Dictionary<object, object>();

        /// <summary>
        /// CSS-like properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Priority for conflicting styles
        /// </summary>
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Context behavior configuration
    /// </summary>
    public class ContextBehavior
    {
        /// <summary>
        /// Behavior name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Condition for triggering the behavior
        /// </summary>
        public ContextRule Condition { get; set; }

        /// <summary>
        /// Action to execute
        /// </summary>
        public Action<FrameworkElement, UIContext> Action { get; set; }

        /// <summary>
        /// Whether the behavior is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Execution priority
        /// </summary>
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Context-aware element wrapper
    /// </summary>
    public class ContextAwareElement
    {
        /// <summary>
        /// UI element
        /// </summary>
        public FrameworkElement Element { get; set; }

        /// <summary>
        /// Context configuration
        /// </summary>
        public ContextAwareConfiguration Configuration { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// Whether the element is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Element metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Context snapshot for debugging
    /// </summary>
    public class ContextSnapshot
    {
        /// <summary>
        /// Current context
        /// </summary>
        public UIContext Context { get; set; }

        /// <summary>
        /// Registered elements
        /// </summary>
        public List<ContextAwareElement> RegisteredElements { get; set; } = new List<ContextAwareElement>();

        /// <summary>
        /// Snapshot timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Active context providers
        /// </summary>
        public List<string> ContextProviders { get; set; } = new List<string>();

        /// <summary>
        /// Active features
        /// </summary>
        public List<string> ActiveFeatures { get; set; } = new List<string>();

        /// <summary>
        /// Performance metrics
        /// </summary>
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Context changed event arguments
    /// </summary>
    public class ContextChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Previous context
        /// </summary>
        public UIContext PreviousContext { get; set; }

        /// <summary>
        /// New context
        /// </summary>
        public UIContext NewContext { get; set; }

        /// <summary>
        /// Context information that triggered the change
        /// </summary>
        public ContextInfo ContextInfo { get; set; }

        /// <summary>
        /// Change timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether the change was automatic
        /// </summary>
        public bool IsAutomatic { get; set; } = true;
    }

    /// <summary>
    /// UI update event arguments
    /// </summary>
    public class UIUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// Current context
        /// </summary>
        public UIContext Context { get; set; }

        /// <summary>
        /// Elements that were updated
        /// </summary>
        public List<FrameworkElement> UpdatedElements { get; set; } = new List<FrameworkElement>();

        /// <summary>
        /// Update timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Update duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Whether the update was successful
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Any errors that occurred
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Logical operators
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>
        /// All conditions must be true
        /// </summary>
        And,

        /// <summary>
        /// At least one condition must be true
        /// </summary>
        Or,

        /// <summary>
        /// Exactly one condition must be true
        /// </summary>
        Xor
    }

    /// <summary>
    /// Condition operators
    /// </summary>
    public enum ConditionOperator
    {
        /// <summary>
        /// Equals
        /// </summary>
        Equals,

        /// <summary>
        /// Not equals
        /// </summary>
        NotEquals,

        /// <summary>
        /// Greater than
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Less than
        /// </summary>
        LessThan,

        /// <summary>
        /// Greater than or equal
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Less than or equal
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Contains
        /// </summary>
        Contains,

        /// <summary>
        /// Starts with
        /// </summary>
        StartsWith,

        /// <summary>
        /// Ends with
        /// </summary>
        EndsWith,

        /// <summary>
        /// Is true
        /// </summary>
        IsTrue,

        /// <summary>
        /// Is false
        /// </summary>
        IsFalse,

        /// <summary>
        /// Is null
        /// </summary>
        IsNull,

        /// <summary>
        /// Is not null
        /// </summary>
        IsNotNull,

        /// <summary>
        /// Matches regex
        /// </summary>
        MatchesRegex
    }
}