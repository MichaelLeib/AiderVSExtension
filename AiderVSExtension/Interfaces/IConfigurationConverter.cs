using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for converting configuration between different formats
    /// </summary>
    public interface IConfigurationConverter
    {
        /// <summary>
        /// Gets the supported input formats
        /// </summary>
        IEnumerable<string> SupportedInputFormats { get; }

        /// <summary>
        /// Gets the supported output formats
        /// </summary>
        IEnumerable<string> SupportedOutputFormats { get; }

        /// <summary>
        /// Converts configuration from one format to another
        /// </summary>
        /// <param name="inputData">The input configuration data</param>
        /// <param name="inputFormat">The input format</param>
        /// <param name="outputFormat">The output format</param>
        /// <returns>The converted configuration data</returns>
        Task<string> ConvertAsync(string inputData, string inputFormat, string outputFormat);

        /// <summary>
        /// Validates that a format is supported for input
        /// </summary>
        /// <param name="format">The format to validate</param>
        /// <returns>True if the format is supported for input</returns>
        bool IsInputFormatSupported(string format);

        /// <summary>
        /// Validates that a format is supported for output
        /// </summary>
        /// <param name="format">The format to validate</param>
        /// <returns>True if the format is supported for output</returns>
        bool IsOutputFormatSupported(string format);

        /// <summary>
        /// Validates configuration data in a specific format
        /// </summary>
        /// <param name="data">The configuration data to validate</param>
        /// <param name="format">The format of the data</param>
        /// <returns>Validation result with any errors</returns>
        Task<ValidationResult> ValidateAsync(string data, string format);

        /// <summary>
        /// Gets the default file extension for a format
        /// </summary>
        /// <param name="format">The format</param>
        /// <returns>The default file extension</returns>
        string GetDefaultExtension(string format);
    }

    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors
        /// </summary>
        public IList<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the validation warnings
        /// </summary>
        public IList<string> Warnings { get; set; } = new List<string>();
    }
}
