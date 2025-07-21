using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for checking and managing Aider dependencies
    /// </summary>
    public interface IAiderDependencyChecker
    {
        /// <summary>
        /// Checks the status of all Aider dependencies
        /// </summary>
        /// <returns>Dependency status information</returns>
        Task<AiderDependencyStatus> CheckDependenciesAsync();

        /// <summary>
        /// Installs Aider using pip
        /// </summary>
        /// <returns>True if installation succeeded</returns>
        Task<bool> InstallAiderAsync();

        /// <summary>
        /// Upgrades Aider to the latest version
        /// </summary>
        /// <returns>True if upgrade succeeded</returns>
        Task<bool> UpgradeAiderAsync();
    }
}