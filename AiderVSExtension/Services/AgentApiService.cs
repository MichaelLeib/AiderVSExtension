using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Security;
using System.Text.Json;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for managing AgentAPI server and communication with Aider
    /// </summary>
    public class AgentApiService : IAgentApiService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IConfigurationService _configurationService;
        private readonly IAiderDependencyChecker _dependencyChecker;
        private readonly ITelemetryService _telemetryService;
        private readonly ICircuitBreakerService _circuitBreaker;

        private Process _agentApiProcess;
        private HttpClient _httpClient;
        private AgentApiConfig _config;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private readonly SemaphoreSlim _startupSemaphore = new SemaphoreSlim(1, 1);

        public event EventHandler<AgentApiEventArgs> StatusChanged;
        public event EventHandler<AgentApiEventArgs> MessageReceived;

        public bool IsRunning => _agentApiProcess != null && !_agentApiProcess.HasExited;
        public string ServerUrl => SecureUrlBuilder.BuildSecureUrl(_config?.Host ?? "localhost", _config?.Port ?? 3284);

        public AgentApiService(
            IErrorHandler errorHandler,
            IConfigurationService configurationService,
            IAiderDependencyChecker dependencyChecker,
            ITelemetryService telemetryService,
            ICircuitBreakerService circuitBreaker)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _dependencyChecker = dependencyChecker ?? throw new ArgumentNullException(nameof(dependencyChecker));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));

            InitializeHttpClient();
            LoadConfiguration();
        }

        /// <summary>
        /// Starts the AgentAPI server
        /// </summary>
        public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            await _startupSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (IsRunning)
                {
                    return true;
                }

                await _errorHandler.LogInfoAsync("Starting AgentAPI server", "AgentApiService.StartAsync");

                // Check dependencies first
                var dependencyStatus = await _dependencyChecker.CheckDependenciesAsync();
                if (!dependencyStatus.IsAiderInstalled)
                {
                    OnStatusChanged("dependency_missing", "Aider is not installed. Please install Aider first.");
                    return false;
                }

                // Find AgentAPI executable
                var agentApiPath = GetAgentApiExecutablePath();
                if (string.IsNullOrEmpty(agentApiPath) || !File.Exists(agentApiPath))
                {
                    OnStatusChanged("agentapi_missing", "AgentAPI executable not found. Please install AgentAPI from https://github.com/coder/agentapi/releases");
                    await _errorHandler.LogErrorAsync(
                        "AgentAPI executable not found. Install from: https://github.com/coder/agentapi/releases",
                        new FileNotFoundException("AgentAPI not found"),
                        "AgentApiService.StartAsync");
                    return false;
                }

                // Build command arguments
                var args = BuildAgentApiArguments(dependencyStatus);

                // Start AgentAPI process
                var startInfo = new ProcessStartInfo
                {
                    FileName = agentApiPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(agentApiPath)
                };

                // Add environment variables
                foreach (var envVar in _config.Environment)
                {
                    startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }

                _agentApiProcess = Process.Start(startInfo);
                if (_agentApiProcess == null)
                {
                    OnStatusChanged("start_failed", "Failed to start AgentAPI process");
                    return false;
                }

                // Monitor process output
                _ = MonitorProcessOutput();

                // Wait for server to be ready
                var isReady = await WaitForServerReady(cancellationToken);
                if (isReady)
                {
                    OnStatusChanged("running", "AgentAPI server is running");
                    _telemetryService?.TrackEvent("AgentApi.Started", new Dictionary<string, string>
                    {
                        ["Model"] = _config.Model,
                        ["Port"] = _config.Port.ToString()
                    });
                }
                else
                {
                    OnStatusChanged("start_timeout", "AgentAPI server failed to start within timeout");
                    await StopAsync();
                }

                return isReady;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error starting AgentAPI server", ex, "AgentApiService.StartAsync");
                OnStatusChanged("start_error", $"Error starting server: {ex.Message}");
                return false;
            }
            finally
            {
                _startupSemaphore.Release();
            }
        }

        /// <summary>
        /// Stops the AgentAPI server
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                if (_agentApiProcess != null && !_agentApiProcess.HasExited)
                {
                    await _errorHandler.LogInfoAsync("Stopping AgentAPI server", "AgentApiService.StopAsync");

                    // Try graceful shutdown first
                    try
                    {
                        // AgentAPI doesn't have a shutdown endpoint, use process termination
                    // await _httpClient.PostAsync($"{ServerUrl}/shutdown", null);
                        await Task.Delay(2000); // Give it time to shutdown gracefully
                    }
                    catch
                    {
                        // Ignore errors during graceful shutdown
                    }

                    // Force kill if still running
                    if (!_agentApiProcess.HasExited)
                    {
                        _agentApiProcess.Kill();
                        await _agentApiProcess.WaitForExitAsync();
                    }

                    _agentApiProcess.Dispose();
                    _agentApiProcess = null;

                    OnStatusChanged("stopped", "AgentAPI server stopped");
                    _telemetryService?.TrackEvent("AgentApi.Stopped");
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error stopping AgentAPI server", ex, "AgentApiService.StopAsync");
            }
        }

        /// <summary>
        /// Sends a message to Aider via AgentAPI
        /// </summary>
        public async Task<AgentApiResponse> SendMessageAsync(AgentApiRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsRunning)
                {
                    throw new InvalidOperationException("AgentAPI server is not running");
                }

                return await _circuitBreaker.ExecuteAsync(async (ct) =>
                {
                    var startTime = DateTime.UtcNow;

                    var json = SecureJsonSerializer.Serialize(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(_config.RequestTimeout);

                    // AgentAPI uses /message endpoint (correct)
                    var response = await _httpClient.PostAsync($"{ServerUrl}/message", content, timeoutCts.Token);
                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var agentResponse = SecureJsonSerializer.Deserialize<AgentApiResponse>(responseJson, strict: true);

                    var duration = DateTime.UtcNow - startTime;
                    _telemetryService?.TrackPerformance("AgentApi.SendMessage", duration, true, new Dictionary<string, string>
                    {
                        ["MessageLength"] = request.Content?.Length.ToString() ?? "0"
                    });

                    OnMessageReceived("response", agentResponse);
                    return agentResponse;

                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error sending message to AgentAPI", ex, "AgentApiService.SendMessageAsync");
                _telemetryService?.TrackException(ex, new Dictionary<string, string>
                {
                    ["Operation"] = "SendMessage"
                });
                throw;
            }
        }

        /// <summary>
        /// Gets the current status of the AgentAPI server
        /// </summary>
        public async Task<AgentApiStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsRunning)
                {
                    return new AgentApiStatus { Status = "stopped" };
                }

                var response = await _httpClient.GetAsync($"{ServerUrl}/status", cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return SecureJsonSerializer.Deserialize<AgentApiStatus>(responseJson, strict: true);
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Error getting AgentAPI status: {ex.Message}", "AgentApiService.GetStatusAsync");
                return new AgentApiStatus { Status = "error" };
            }
        }

        /// <summary>
        /// Restarts the AgentAPI server
        /// </summary>
        public async Task<bool> RestartAsync(CancellationToken cancellationToken = default)
        {
            await StopAsync();
            await Task.Delay(1000, cancellationToken); // Brief pause
            return await StartAsync(cancellationToken);
        }

        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10) // Long timeout for AI responses
            };
        }

        private void LoadConfiguration()
        {
            _config = new AgentApiConfig();

            // Load from configuration service if available
            try
            {
                var aiConfig = _configurationService.GetAIModelConfiguration();
                if (aiConfig != null)
                {
                    _config.Model = aiConfig.ModelName ?? "sonnet";
                    if (aiConfig.Parameters?.ContainsKey("agentapi_port") == true)
                    {
                        if (int.TryParse(aiConfig.Parameters["agentapi_port"].ToString(), out var port))
                        {
                            _config.Port = port;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error loading AgentAPI configuration: {ex.Message}", "AgentApiService.LoadConfiguration");
            }
        }

        private string GetAgentApiExecutablePath()
        {
            try
            {
                // First check if explicitly configured
                if (!string.IsNullOrEmpty(_config.AgentApiExecutablePath) && File.Exists(_config.AgentApiExecutablePath))
                {
                    return _config.AgentApiExecutablePath;
                }

                var executableName = GetPlatformExecutableName();

                // Try to find in PATH first (preferred method for user-installed AgentAPI)
                try
                {
                    var pathEnv = Environment.GetEnvironmentVariable("PATH");
                    if (!string.IsNullOrEmpty(pathEnv))
                    {
                        foreach (var path in pathEnv.Split(Path.PathSeparator))
                        {
                            var candidate = Path.Combine(path, executableName);
                            if (File.Exists(candidate))
                            {
                                return candidate;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore PATH search errors
                }

                // Check common installation locations
                var commonPaths = new List<string>();
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows: Check common installation paths
                    commonPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "agentapi"));
                    commonPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "agentapi"));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS: Check Homebrew and common paths
                    commonPaths.Add("/usr/local/bin");
                    commonPaths.Add("/opt/homebrew/bin");
                    commonPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"));
                }
                else
                {
                    // Linux: Check common installation paths
                    commonPaths.Add("/usr/local/bin");
                    commonPaths.Add("/usr/bin");
                    commonPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"));
                }

                foreach (var path in commonPaths)
                {
                    var candidate = Path.Combine(path, executableName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error finding AgentAPI executable: {ex.Message}", "AgentApiService.GetAgentApiExecutablePath");
                return null;
            }
        }

        private string GetPlatformExecutableName()
        {
            // AgentAPI uses standard naming: "agentapi" with .exe extension on Windows
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "agentapi.exe" : "agentapi";
        }

        private string BuildAgentApiArguments(AiderDependencyStatus dependencyStatus)
        {
            var args = new List<string>
            {
                "server"
            };

            // Validate and sanitize host
            var host = ProcessSecurity.SanitizeArgument(_config.Host ?? "localhost");
            if (host != "localhost")
            {
                // Additional validation for host format
                if (!IsValidHostname(host))
                {
                    throw new SecurityException("Invalid hostname format detected");
                }
                args.Add("--host");
                args.Add(host);
            }
            
            // Validate port range
            if (_config.Port != 3284)
            {
                if (_config.Port < 1024 || _config.Port > 65535)
                {
                    throw new SecurityException("Port number outside valid range");
                }
                args.Add("--port");
                args.Add(_config.Port.ToString());
            }

            // Add the separator for the wrapped command
            args.Add("--");

            // Add aider command
            args.Add("aider");

            // Add model configuration with validation
            if (!string.IsNullOrEmpty(_config.Model))
            {
                if (!ProcessSecurity.IsValidModelName(_config.Model))
                {
                    throw new SecurityException("Invalid model name format detected");
                }
                args.Add("--model");
                args.Add(ProcessSecurity.SanitizeArgument(_config.Model));
            }

            // Add API key configuration with enhanced security
            var aiConfig = _configurationService.GetAIModelConfiguration();
            if (aiConfig != null && !string.IsNullOrEmpty(aiConfig.ApiKey))
            {
                // Validate provider and API key combination
                var provider = aiConfig.Provider switch
                {
                    AIProvider.ChatGPT => "openai",
                    AIProvider.Claude => "anthropic",
                    _ => null
                };

                if (!string.IsNullOrEmpty(provider))
                {
                    // Enhanced security validation
                    if (!ProcessSecurity.IsValidProviderApiKeyCombination(provider, aiConfig.ApiKey))
                    {
                        throw new SecurityException("Invalid provider-API key combination detected");
                    }
                    
                    // Use separate arguments instead of concatenation to prevent injection
                    args.Add("--api-key");
                    args.Add($"{ProcessSecurity.SanitizeArgument(provider)}={ProcessSecurity.SanitizeArgument(aiConfig.ApiKey)}");
                }
            }

            // Add additional aider arguments
            args.Add("--yes-always"); // Auto-confirm changes
            
            // Note: Removed --no-git to allow Aider's git integration
            // The extension will handle git operations through LibGit2Sharp

            return string.Join(" ", args.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg));
        }
        
        /// <summary>
        /// Validates hostname format for security
        /// </summary>
        private bool IsValidHostname(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
                return false;
            
            // Allow localhost and IP addresses (basic validation)
            if (hostname == "localhost" || hostname == "127.0.0.1" || hostname == "::1")
                return true;
            
            // Basic hostname validation (letters, numbers, dots, hyphens)
            return System.Text.RegularExpressions.Regex.IsMatch(hostname, @"^[a-zA-Z0-9.-]+$") && hostname.Length <= 253;
        }

        private async Task<bool> WaitForServerReady(CancellationToken cancellationToken)
        {
            var timeout = DateTime.UtcNow.Add(_config.StartupTimeout);

            while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // AgentAPI uses /status endpoint for health checks
                    var response = await _httpClient.GetAsync($"{ServerUrl}/status", cancellationToken);
                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Server not ready yet, continue waiting
                }

                await Task.Delay(1000, cancellationToken);
            }

            return false;
        }

        private async Task MonitorProcessOutput()
        {
            try
            {
                if (_agentApiProcess == null) return;

                var outputTask = MonitorOutputStreamAsync(_agentApiProcess.StandardOutput, "Output");
                var errorTask = MonitorOutputStreamAsync(_agentApiProcess.StandardError, "Error");

                await Task.WhenAll(outputTask, errorTask);
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Error monitoring AgentAPI process output", ex, "AgentApiService.MonitorProcessOutput");
            }
        }

        private async Task MonitorOutputStreamAsync(StreamReader reader, string streamType)
        {
            try
            {
                string line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (streamType == "Output")
                    {
                        await _errorHandler.LogInfoAsync($"AgentAPI Output: {line}", "AgentApiService.MonitorProcessOutput").ConfigureAwait(false);
                    }
                    else
                    {
                        await _errorHandler.LogWarningAsync($"AgentAPI Error: {line}", "AgentApiService.MonitorProcessOutput").ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Error monitoring AgentAPI {streamType.ToLower()} stream", ex, "AgentApiService.MonitorOutputStreamAsync").ConfigureAwait(false);
            }
        }

        private void OnStatusChanged(string eventType, string message)
        {
            StatusChanged?.Invoke(this, new AgentApiEventArgs
            {
                EventType = eventType,
                Message = message
            });
        }

        private void OnMessageReceived(string eventType, object data)
        {
            MessageReceived?.Invoke(this, new AgentApiEventArgs
            {
                EventType = eventType,
                Data = data
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_lockObject)
            {
                if (!_disposed && disposing)
                {
                    // Stop the AgentAPI process first
                    try
                    {
                        var stopTask = StopAsync();
                        if (!stopTask.Wait(TimeSpan.FromSeconds(5)))
                        {
                            // Force kill if graceful shutdown takes too long
                            try
                            {
                                if (_agentApiProcess != null && !_agentApiProcess.HasExited)
                                {
                                    _agentApiProcess.Kill();
                                    _agentApiProcess.WaitForExit(2000);
                                }
                            }
                            catch
                            {
                                // Ignore errors during force kill
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors during disposal
                    }

                    // Dispose managed resources
                    _httpClient?.Dispose();
                    _startupSemaphore?.Dispose();
                    
                    // Dispose process if still exists
                    _agentApiProcess?.Dispose();

                    // Clear event handlers to prevent memory leaks
                    StatusChanged = null;
                    MessageReceived = null;

                    _disposed = true;
                }
            }
        }

        ~AgentApiService()
        {
            Dispose(false);
        }
    }
}