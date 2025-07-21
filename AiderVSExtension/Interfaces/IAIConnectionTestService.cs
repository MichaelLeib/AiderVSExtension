using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for testing AI provider connections
    /// </summary>
    public interface IAIConnectionTestService
    {
        /// <summary>
        /// Tests connection to the specified AI provider
        /// </summary>
        /// <param name="config">AI model configuration to test</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Connection test result</returns>
        Task<ConnectionTestResult> TestConnectionAsync(AIModelConfiguration config, CancellationToken cancellationToken = default);
    }
}