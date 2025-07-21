using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Base class for data models that provides validation and property change notification
    /// </summary>
    public abstract class ValidatableModelBase : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets validation errors for the entire object
        /// </summary>
        public string Error
        {
            get
            {
                var errors = GetValidationErrors();
                return errors.Count > 0 ? string.Join(Environment.NewLine, errors) : string.Empty;
            }
        }

        /// <summary>
        /// Gets validation error for a specific property
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>Error message, or empty string if valid</returns>
        public string this[string propertyName]
        {
            get
            {
                return _errors.TryGetValue(propertyName, out var error) ? error : string.Empty;
            }
        }

        /// <summary>
        /// Gets whether the model is currently valid
        /// </summary>
        public bool IsValid => GetValidationErrors().Count == 0;

        /// <summary>
        /// Gets all validation errors for this model
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public virtual List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            
            // Get Data Annotations validation errors
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this);
            
            if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    errors.Add(validationResult.ErrorMessage ?? "Unknown validation error");
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates a property value and updates the error state
        /// </summary>
        /// <param name="value">Property value to validate</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>True if valid, false otherwise</returns>
        protected bool ValidateProperty(object value, [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null) return true;

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this) { MemberName = propertyName };

            var isValid = Validator.TryValidateProperty(value, validationContext, validationResults);

            if (isValid)
            {
                _errors.Remove(propertyName);
            }
            else
            {
                var error = validationResults.Count > 0 ? validationResults[0].ErrorMessage ?? "Invalid value" : "Invalid value";
                _errors[propertyName] = error;
            }

            return isValid;
        }

        /// <summary>
        /// Sets a property value and raises change notification
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Backing field for the property</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>True if the value was changed, false otherwise</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            ValidateProperty(value, propertyName);
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Validates all properties and updates error state
        /// </summary>
        /// <returns>True if all properties are valid, false otherwise</returns>
        protected bool ValidateAllProperties()
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this);

            var isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);

            _errors.Clear();
            
            foreach (var validationResult in validationResults)
            {
                var propertyName = validationResult.MemberNames.FirstOrDefault() ?? "Unknown";
                _errors[propertyName] = validationResult.ErrorMessage ?? "Invalid value";
            }

            return isValid;
        }

        /// <summary>
        /// Clears all validation errors
        /// </summary>
        protected void ClearErrors()
        {
            _errors.Clear();
            OnPropertyChanged(nameof(Error));
        }

        /// <summary>
        /// Adds a validation error for a specific property
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="errorMessage">Error message</param>
        protected void AddError(string propertyName, string errorMessage)
        {
            _errors[propertyName] = errorMessage;
            OnPropertyChanged(nameof(Error));
        }

        /// <summary>
        /// Removes a validation error for a specific property
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        protected void RemoveError(string propertyName)
        {
            _errors.Remove(propertyName);
            OnPropertyChanged(nameof(Error));
        }

        /// <summary>
        /// Gets all property names that have validation errors
        /// </summary>
        /// <returns>Collection of property names with errors</returns>
        public IEnumerable<string> GetPropertiesWithErrors()
        {
            return _errors.Keys;
        }

        /// <summary>
        /// Gets the error message for a specific property
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>Error message, or null if no error</returns>
        public string GetPropertyError(string propertyName)
        {
            return _errors.TryGetValue(propertyName, out var error) ? error : null;
        }

        /// <summary>
        /// Creates a validation summary for logging or display
        /// </summary>
        /// <returns>Validation summary</returns>
        public virtual string GetValidationSummary()
        {
            var errors = GetValidationErrors();
            if (errors.Count == 0)
            {
                return "No validation errors";
            }

            return $"Validation errors ({errors.Count}):\n" + string.Join("\n", errors.Select((e, i) => $"{i + 1}. {e}"));
        }
    }
}