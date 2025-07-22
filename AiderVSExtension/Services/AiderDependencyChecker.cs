using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Security;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for checking Aider and Python dependencies
    /// </summary>
    public class AiderDependencyChecker : IAiderDependencyChecker
    {
        private readonly IErrorHandler _errorHandler;

        public AiderDependencyChecker(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Checks the status of all Aider dependencies
        /// </summary>
        public async Task<AiderDependencyStatus> CheckDependenciesAsync()
        {
            var status = new AiderDependencyStatus();

            try
            {
                // Check Python installation
                await CheckPythonAsync(status);

                // Check Aider installation (only if Python is available)
                if (status.IsPythonInstalled)
                {
                    await CheckAiderAsync(status);
                }

                // Check AgentAPI installation
                await CheckAgentApiAsync(status);

                return status;
            }
            catch (Exception ex)
            {
                status.ErrorMessage = $"Error checking dependencies: {ex.Message}";
                await _errorHandler.LogErrorAsync("Failed to check Aider dependencies", ex, "AiderDependencyChecker.CheckDependenciesAsync");
                return status;
            }
        }

        /// <summary>
        /// Installs Aider using pip (with enhanced security)
        /// </summary>
        public async Task<bool> InstallAiderAsync()
        {
            try
            {
                await _errorHandler.LogInfoAsync("Starting Aider installation", "AiderDependencyChecker.InstallAiderAsync");

                // Security validation - only allow pip command
                const string command = "pip";
                if (!ProcessSecurity.IsAllowedCommand(command))
                {
                    throw new SecurityException("Attempted to execute disallowed command");
                }

                // Use argument array instead of string concatenation to prevent injection
                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                // Add arguments for security
                startInfo.Arguments = "install aider-chat";

                Process process = null;
                try
                {
                    process = Process.Start(startInfo);
                    if (process == null)
                    {
                        await _errorHandler.LogErrorAsync("Failed to start pip process", null, "AiderDependencyChecker.InstallAiderAsync");
                        return false;
                    }

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        await _errorHandler.LogInfoAsync("Aider installation completed successfully", "AiderDependencyChecker.InstallAiderAsync");
                        return true;
                    }
                    else
                    {
                        await _errorHandler.LogErrorAsync($"Aider installation failed. Output: {output}, Error: {error}", null, "AiderDependencyChecker.InstallAiderAsync");
                        return false;
                    }
                }
                finally
                {
                    process?.Dispose();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error during Aider installation", ex, "AiderDependencyChecker.InstallAiderAsync");
                return false;
            }
        }

        /// <summary>
        /// Upgrades Aider to the latest version
        /// </summary>
        public async Task<bool> UpgradeAiderAsync()
        {
            try
            {
                await _errorHandler.LogInfoAsync("Starting Aider upgrade", "AiderDependencyChecker.UpgradeAiderAsync");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "pip",
                    Arguments = "install --upgrade aider-chat",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return false;
                }

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error during Aider upgrade", ex, "AiderDependencyChecker.UpgradeAiderAsync");
                return false;
            }
        }

        private async Task CheckPythonAsync(AiderDependencyStatus status)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    // Try python3 as fallback
                    await CheckPython3Async(status);
                    return;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && output.Contains("Python"))
                {
                    status.IsPythonInstalled = true;
                    status.PythonVersion = output.Trim();
                    status.PythonPath = "python";
                }
                else
                {
                    // Try python3 as fallback
                    await CheckPython3Async(status);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Error checking Python: {ex.Message}", "AiderDependencyChecker.CheckPythonAsync");
                // Try python3 as fallback
                await CheckPython3Async(status);
            }
        }

        private async Task CheckPython3Async(AiderDependencyStatus status)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    status.MissingDependencies.Add("Python 3.x is not installed or not in PATH");
                    return;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && output.Contains("Python"))
                {
                    status.IsPythonInstalled = true;
                    status.PythonVersion = output.Trim();
                    status.PythonPath = "python3";
                }
                else
                {
                    status.MissingDependencies.Add("Python 3.x is not installed or not in PATH");
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Error checking Python3: {ex.Message}", "AiderDependencyChecker.CheckPython3Async");
                status.MissingDependencies.Add("Python 3.x is not installed or not accessible");
            }
        }

        private async Task CheckAiderAsync(AiderDependencyStatus status)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = status.PythonPath,
                    Arguments = "-c \"import aider; print(aider.__version__)\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    status.MissingDependencies.Add("Unable to check Aider installation");
                    return;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    status.IsAiderInstalled = true;
                    status.AiderVersion = output.Trim();
                    
                    // Try to find aider executable path
                    await FindAiderExecutableAsync(status);
                }
                else
                {
                    status.MissingDependencies.Add("Aider is not installed (run: pip install aider-chat)");
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Error checking Aider: {ex.Message}", "AiderDependencyChecker.CheckAiderAsync");
                status.MissingDependencies.Add("Unable to verify Aider installation");
            }
        }

        private async Task FindAiderExecutableAsync(AiderDependencyStatus status)
        {
            try
            {
                // Try common aider executable names
                var aiderCommands = new[] { "aider", "aider.exe", status.PythonPath + " -m aider" };

                foreach (var command in aiderCommands)
                {
                    try
                    {
                        var parts = command.Split(' ');
                        var fileName = parts[0];
                        var args = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) + " --version" : "--version";

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = fileName,
                            Arguments = args,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(startInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            if (process.ExitCode == 0)
                            {
                                status.AiderPath = command;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Continue trying other commands
                    }
                }

                if (string.IsNullOrEmpty(status.AiderPath))
                {
                    // Fallback to python module invocation
                    status.AiderPath = $"{status.PythonPath} -m aider";
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Error finding Aider executable: {ex.Message}", "AiderDependencyChecker.FindAiderExecutableAsync");
                status.AiderPath = $"{status.PythonPath} -m aider";
            }
        }

        private async Task CheckAgentApiAsync(AiderDependencyStatus status)
        {
            try
            {
                // Get the appropriate executable name for the platform
                var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "agentapi.exe" : "agentapi";

                // Try to find AgentAPI in PATH
                var startInfo = new ProcessStartInfo
                {
                    FileName = executableName,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                try
                {
                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (process.ExitCode == 0)
                        {
                            // AgentAPI found
                            await _errorHandler.LogInfoAsync($"AgentAPI found: {output.Trim()}", "AiderDependencyChecker.CheckAgentApiAsync");
                            return;
                        }
                    }
                }
                catch
                {
                    // AgentAPI not found in PATH
                }

                // Check common installation locations
                var commonPaths = GetCommonAgentApiPaths();
                foreach (var path in commonPaths)
                {
                    var fullPath = Path.Combine(path, executableName);
                    if (File.Exists(fullPath))
                    {
                        await _errorHandler.LogInfoAsync($"AgentAPI found at: {fullPath}", "AiderDependencyChecker.CheckAgentApiAsync");
                        return;
                    }
                }

                // AgentAPI not found
                status.MissingDependencies.Add("AgentAPI is not installed. Download from: https://github.com/coder/agentapi/releases");
                await _errorHandler.LogWarningAsync("AgentAPI not found. Install from: https://github.com/coder/agentapi/releases", "AiderDependencyChecker.CheckAgentApiAsync");
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Error checking AgentAPI: {ex.Message}", "AiderDependencyChecker.CheckAgentApiAsync");
                status.MissingDependencies.Add("Unable to verify AgentAPI installation");
            }
        }

        private string[] GetCommonAgentApiPaths()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "agentapi"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "agentapi"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin")
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new[]
                {
                    "/usr/local/bin",
                    "/opt/homebrew/bin",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin")
                };
            }
            else
            {
                // Linux
                return new[]
                {
                    "/usr/local/bin",
                    "/usr/bin",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin")
                };
            }
        }
    }
}