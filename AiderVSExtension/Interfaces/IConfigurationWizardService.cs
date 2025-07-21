using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for configuration wizard service
    /// </summary>
    public interface IConfigurationWizardService
    {
        /// <summary>
        /// Event fired when a wizard step is completed
        /// </summary>
        event EventHandler<WizardStepCompletedEventArgs> StepCompleted;

        /// <summary>
        /// Event fired when a wizard is completed
        /// </summary>
        event EventHandler<WizardCompletedEventArgs> WizardCompleted;

        /// <summary>
        /// Event fired when a wizard is cancelled
        /// </summary>
        event EventHandler<WizardCancelledEventArgs> WizardCancelled;

        /// <summary>
        /// Starts a new configuration wizard session
        /// </summary>
        /// <param name="wizardType">Type of wizard to start</param>
        /// <param name="options">Wizard options</param>
        /// <returns>Wizard session</returns>
        Task<WizardSession> StartWizardAsync(WizardType wizardType, WizardOptions options = null);

        /// <summary>
        /// Processes wizard step input and advances to next step
        /// </summary>
        /// <param name="sessionId">Wizard session ID</param>
        /// <param name="stepData">Step input data</param>
        /// <returns>Next step or null if wizard is complete</returns>
        Task<WizardStep> ProcessStepAsync(string sessionId, Dictionary<string, object> stepData);

        /// <summary>
        /// Goes back to the previous step
        /// </summary>
        /// <param name="sessionId">Wizard session ID</param>
        /// <returns>Previous step or null if at first step</returns>
        Task<WizardStep> GoBackAsync(string sessionId);

        /// <summary>
        /// Cancels a wizard session
        /// </summary>
        /// <param name="sessionId">Wizard session ID</param>
        /// <returns>True if cancelled successfully</returns>
        Task<bool> CancelWizardAsync(string sessionId);

        /// <summary>
        /// Gets the current wizard session
        /// </summary>
        /// <param name="sessionId">Wizard session ID</param>
        /// <returns>Wizard session or null if not found</returns>
        WizardSession GetSession(string sessionId);

        /// <summary>
        /// Gets all active wizard sessions
        /// </summary>
        /// <returns>List of active sessions</returns>
        IEnumerable<WizardSession> GetActiveSessions();

        /// <summary>
        /// Gets wizard templates for quick setup
        /// </summary>
        /// <returns>List of wizard templates</returns>
        Task<IEnumerable<WizardTemplate>> GetWizardTemplatesAsync();

        /// <summary>
        /// Creates a configuration from wizard template
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="customizations">Custom settings</param>
        /// <returns>Configuration profile</returns>
        Task<ConfigurationProfile> CreateFromTemplateAsync(string templateId, Dictionary<string, object> customizations = null);
    }
}