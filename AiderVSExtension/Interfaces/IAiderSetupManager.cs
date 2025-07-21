using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing Aider setup and dependencies
    /// </summary>
    public interface IAiderSetupManager
    {
        /// <summary>
        /// Checks if Aider dependencies are satisfied
        /// </summary>
        /// <returns>True if all dependencies are satisfied</returns>
        Task<bool> AreDependenciesSatisfiedAsync();

        /// <summary>
        /// Shows the setup dialog if dependencies are not satisfied
        /// </summary>
        /// <returns>True if setup was completed successfully</returns>
        Task<bool> EnsureDependenciesAsync();

        /// <summary>
        /// Shows the setup dialog regardless of current status
        /// </summary>
        /// <returns>True if setup was completed successfully</returns>
        Task<bool> ShowSetupDialogAsync();

        /// <summary>
        /// Validates that AgentAPI can be started
        /// </summary>
        /// <returns>True if AgentAPI is ready to use</returns>
        Task<bool> ValidateAgentApiAsync();
    }
}