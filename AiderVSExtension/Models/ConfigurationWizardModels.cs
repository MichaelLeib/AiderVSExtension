using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Wizard session
    /// </summary>
    public class WizardSession
    {
        /// <summary>
        /// Session ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Type of wizard
        /// </summary>
        public WizardType WizardType { get; set; }

        /// <summary>
        /// Wizard options
        /// </summary>
        public WizardOptions Options { get; set; }

        /// <summary>
        /// Session start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Session end time
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Whether the session is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Whether the session is completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Whether the session was cancelled
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// Current step index
        /// </summary>
        public int CurrentStepIndex { get; set; }

        /// <summary>
        /// Current step
        /// </summary>
        public WizardStep CurrentStep { get; set; }

        /// <summary>
        /// Wizard steps
        /// </summary>
        public List<WizardStep> Steps { get; set; } = new List<WizardStep>();

        /// <summary>
        /// Session data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Session progress (0-100)
        /// </summary>
        public double Progress => Steps.Count > 0 ? (double)CurrentStepIndex / Steps.Count * 100 : 0;

        /// <summary>
        /// Session duration
        /// </summary>
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
    }

    /// <summary>
    /// Wizard step
    /// </summary>
    public class WizardStep
    {
        /// <summary>
        /// Step ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Step title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Step description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Step instructions
        /// </summary>
        public string Instructions { get; set; }

        /// <summary>
        /// Step type
        /// </summary>
        public WizardStepType StepType { get; set; }

        /// <summary>
        /// Whether the step is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Whether the step is completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Step completion time
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Step fields
        /// </summary>
        public List<WizardField> Fields { get; set; } = new List<WizardField>();

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<ValidationWarning> ValidationWarnings { get; set; } = new List<ValidationWarning>();

        /// <summary>
        /// Step metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Help text for the step
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        /// Whether the step can be skipped
        /// </summary>
        public bool CanSkip { get; set; }

        /// <summary>
        /// Dependencies on other steps
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    /// <summary>
    /// Wizard field
    /// </summary>
    public class WizardField
    {
        /// <summary>
        /// Field ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Field label
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Field description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Field type
        /// </summary>
        public WizardFieldType Type { get; set; }

        /// <summary>
        /// Whether the field is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Default value
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Current value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Field options (for select fields)
        /// </summary>
        public string[] Options { get; set; }

        /// <summary>
        /// Field placeholder text
        /// </summary>
        public string Placeholder { get; set; }

        /// <summary>
        /// Field help text
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        /// Field validation rules
        /// </summary>
        public List<FieldValidationRule> ValidationRules { get; set; } = new List<FieldValidationRule>();

        /// <summary>
        /// Whether the field is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Whether the field is visible
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Field group (for organization)
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Field order within group
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Wizard template
    /// </summary>
    public class WizardTemplate
    {
        /// <summary>
        /// Template ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Template name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Template description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Template icon
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Wizard type
        /// </summary>
        public WizardType WizardType { get; set; }

        /// <summary>
        /// Template difficulty
        /// </summary>
        public WizardDifficulty Difficulty { get; set; }

        /// <summary>
        /// Estimated completion time
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }

        /// <summary>
        /// Template tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Pre-configured data
        /// </summary>
        public Dictionary<string, object> PreConfiguredData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether the template is built-in
        /// </summary>
        public bool IsBuiltIn { get; set; } = true;

        /// <summary>
        /// Template version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Template author
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Template creation date
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Template last modified date
        /// </summary>
        public DateTime? LastModified { get; set; }
    }

    /// <summary>
    /// Wizard options
    /// </summary>
    public class WizardOptions
    {
        /// <summary>
        /// Whether to skip optional steps
        /// </summary>
        public bool SkipOptionalSteps { get; set; }

        /// <summary>
        /// Whether to use defaults for missing values
        /// </summary>
        public bool UseDefaults { get; set; } = true;

        /// <summary>
        /// Whether to validate each step
        /// </summary>
        public bool ValidateSteps { get; set; } = true;

        /// <summary>
        /// Whether to show progress indicator
        /// </summary>
        public bool ShowProgress { get; set; } = true;

        /// <summary>
        /// Whether to allow going back
        /// </summary>
        public bool AllowGoBack { get; set; } = true;

        /// <summary>
        /// Whether to show help text
        /// </summary>
        public bool ShowHelp { get; set; } = true;

        /// <summary>
        /// Custom wizard theme
        /// </summary>
        public string Theme { get; set; }

        /// <summary>
        /// Custom wizard data
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Field validation rule
    /// </summary>
    public class FieldValidationRule
    {
        /// <summary>
        /// Rule type
        /// </summary>
        public ValidationRuleType Type { get; set; }

        /// <summary>
        /// Rule value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Wizard step completed event arguments
    /// </summary>
    public class WizardStepCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Session ID
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Completed step
        /// </summary>
        public WizardStep Step { get; set; }

        /// <summary>
        /// Step data
        /// </summary>
        public Dictionary<string, object> StepData { get; set; }

        /// <summary>
        /// Completion timestamp
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Wizard completed event arguments
    /// </summary>
    public class WizardCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Session ID
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Created configuration profile
        /// </summary>
        public ConfigurationProfile Profile { get; set; }

        /// <summary>
        /// Completion timestamp
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total wizard duration
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Wizard cancelled event arguments
    /// </summary>
    public class WizardCancelledEventArgs : EventArgs
    {
        /// <summary>
        /// Session ID
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Cancellation timestamp
        /// </summary>
        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Cancellation reason
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// Wizard types
    /// </summary>
    public enum WizardType
    {
        /// <summary>
        /// Quick start wizard
        /// </summary>
        QuickStart,

        /// <summary>
        /// AI model setup wizard
        /// </summary>
        AIModelSetup,

        /// <summary>
        /// Advanced configuration wizard
        /// </summary>
        AdvancedConfiguration,

        /// <summary>
        /// Profile migration wizard
        /// </summary>
        ProfileMigration,

        /// <summary>
        /// Troubleshooting wizard
        /// </summary>
        Troubleshooting,

        /// <summary>
        /// Custom wizard
        /// </summary>
        Custom
    }

    /// <summary>
    /// Wizard step types
    /// </summary>
    public enum WizardStepType
    {
        /// <summary>
        /// Information step
        /// </summary>
        Information,

        /// <summary>
        /// Input step
        /// </summary>
        Input,

        /// <summary>
        /// Selection step
        /// </summary>
        Selection,

        /// <summary>
        /// Configuration step
        /// </summary>
        Configuration,

        /// <summary>
        /// Validation step
        /// </summary>
        Validation,

        /// <summary>
        /// Summary step
        /// </summary>
        Summary,

        /// <summary>
        /// Custom step
        /// </summary>
        Custom
    }

    /// <summary>
    /// Wizard field types
    /// </summary>
    public enum WizardFieldType
    {
        /// <summary>
        /// Text field
        /// </summary>
        Text,

        /// <summary>
        /// Password field
        /// </summary>
        Password,

        /// <summary>
        /// Number field
        /// </summary>
        Number,

        /// <summary>
        /// Boolean field
        /// </summary>
        Boolean,

        /// <summary>
        /// Select field
        /// </summary>
        Select,

        /// <summary>
        /// Multi-select field
        /// </summary>
        MultiSelect,

        /// <summary>
        /// File field
        /// </summary>
        File,

        /// <summary>
        /// Textarea field
        /// </summary>
        Textarea,

        /// <summary>
        /// Date field
        /// </summary>
        Date,

        /// <summary>
        /// Time field
        /// </summary>
        Time,

        /// <summary>
        /// URL field
        /// </summary>
        Url,

        /// <summary>
        /// Email field
        /// </summary>
        Email,

        /// <summary>
        /// Range field
        /// </summary>
        Range,

        /// <summary>
        /// Color field
        /// </summary>
        Color,

        /// <summary>
        /// Custom field
        /// </summary>
        Custom
    }

    /// <summary>
    /// Wizard difficulty levels
    /// </summary>
    public enum WizardDifficulty
    {
        /// <summary>
        /// Beginner level
        /// </summary>
        Beginner,

        /// <summary>
        /// Intermediate level
        /// </summary>
        Intermediate,

        /// <summary>
        /// Advanced level
        /// </summary>
        Advanced,

        /// <summary>
        /// Expert level
        /// </summary>
        Expert
    }

    /// <summary>
    /// Validation rule types
    /// </summary>
    public enum ValidationRuleType
    {
        /// <summary>
        /// Required field
        /// </summary>
        Required,

        /// <summary>
        /// Minimum length
        /// </summary>
        MinLength,

        /// <summary>
        /// Maximum length
        /// </summary>
        MaxLength,

        /// <summary>
        /// Minimum value
        /// </summary>
        MinValue,

        /// <summary>
        /// Maximum value
        /// </summary>
        MaxValue,

        /// <summary>
        /// Regular expression pattern
        /// </summary>
        Pattern,

        /// <summary>
        /// Email format
        /// </summary>
        Email,

        /// <summary>
        /// URL format
        /// </summary>
        Url,

        /// <summary>
        /// Custom validation
        /// </summary>
        Custom
    }
}