using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a template parameter for configuration templates
    /// </summary>
    public class TemplateParameter
    {
        /// <summary>
        /// Parameter name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameter display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Parameter description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Parameter type
        /// </summary>
        public Type ParameterType { get; set; }

        /// <summary>
        /// Default value for the parameter
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Whether the parameter is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Validation rules for the parameter
        /// </summary>
        public List<string> ValidationRules { get; set; } = new List<string>();

        /// <summary>
        /// Possible values for the parameter (for enum-like parameters)
        /// </summary>
        public List<object> PossibleValues { get; set; } = new List<object>();

        /// <summary>
        /// Category for grouping parameters
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Order for display purposes
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Whether the parameter should be displayed in the UI
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Help text or documentation URL
        /// </summary>
        public string HelpText { get; set; }
    }
}