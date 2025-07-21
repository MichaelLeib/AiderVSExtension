using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Exceptions;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using System.Linq;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// HTTP client for communicating with Aider backend when WebSocket is not available
    /// </summary>
    public class AiderHttpClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IErrorHandler _errorHandler;
        private readonly string _baseUrl;
        private readonly RetryPolicy _retryPolicy;
        private bool _isDisposed = false;

        public AiderHttpClient(string baseUrl, IErrorHandler errorHandler, RetryPolicy retryPolicy = null)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _retryPolicy = retryPolicy ?? new RetryPolicy();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.Network.DefaultUserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        /// <summary>
        /// Sends a chat message to Aider via HTTP and returns the response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The response from Aider</returns>
        public async Task<ChatMessage> SendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AiderHttpClient));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var requestPayload = new
                    {
                        message = message.Content,
                        type = message.Type.ToString(),
                        references = message.References,
                        timestamp = message.Timestamp.ToString("O")
                    };

                    var jsonContent = JsonSerializer.Serialize(requestPayload);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    await _errorHandler.LogInfoAsync($"Sending HTTP request to {_baseUrl}/chat", "AiderHttpClient.SendMessageAsync").ConfigureAwait(false);

                    var response = await _httpClient.PostAsync("/chat", httpContent, cancellationToken).ConfigureAwait(false);

                    if (!((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new AiderCommunicationException($"HTTP {response.StatusCode}: {errorContent}");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    
                    // Validate response content before deserialization
                    var validationErrors = ValidationHelper.ValidateAIResponse(responseContent, "AI Response");
                    if (validationErrors.Any())
                    {
                        throw new AiderCommunicationException($"Invalid AI response: {string.Join("; ", validationErrors)}");
                    }
                    
                    ChatMessage responseMessage;
                    try
                    {
                        responseMessage = JsonSerializer.Deserialize<ChatMessage>(responseContent);
                    }
                    catch (JsonException ex)
                    {
                        throw new AiderCommunicationException($"Failed to deserialize AI response: {ex.Message}", ex);
                    }

                    if (responseMessage == null)
                    {
                        throw new AiderCommunicationException("Received null response from Aider backend");
                    }
                    
                    // Validate the deserialized message
                    var messageValidationErrors = ValidationHelper.GetValidationErrors(responseMessage);
                    if (messageValidationErrors.Any())
                    {
                        throw new AiderCommunicationException($"Invalid AI response structure: {string.Join("; ", messageValidationErrors)}");
                    }

                    await _errorHandler.LogInfoAsync("Received response from Aider backend", "AiderHttpClient.SendMessageAsync").ConfigureAwait(false);
                    return responseMessage;
                }
                catch (HttpRequestException ex)
                {
                    await _errorHandler.LogWarningAsync($"HTTP request failed: {ex.Message}", "AiderHttpClient.SendMessageAsync").ConfigureAwait(false);
                    throw new AiderCommunicationException("HTTP request to Aider backend failed", ex);
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
                {
                    await _errorHandler.LogWarningAsync("HTTP request was cancelled", "AiderHttpClient.SendMessageAsync").ConfigureAwait(false);
                    throw new AiderCommunicationException("Request was cancelled", ex);
                }
                catch (TaskCanceledException ex)
                {
                    await _errorHandler.LogWarningAsync("HTTP request timed out", "AiderHttpClient.SendMessageAsync").ConfigureAwait(false);
                    throw new AiderCommunicationException("Request timed out", ex);
                }
                catch (JsonException ex)
                {
                    await _errorHandler.LogErrorAsync($"JSON serialization error: {ex.Message}", "AiderHttpClient.SendMessageAsync").ConfigureAwait(false);
                    throw new AiderCommunicationException("Failed to serialize/deserialize message", ex);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks if the Aider backend is available via HTTP
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if available, false otherwise</returns>
        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                return false;

            try
            {
                var response = await _httpClient.GetAsync("/health", cancellationToken).ConfigureAwait(false);
                return ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300);
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Health check failed: {ex.Message}", "AiderHttpClient.IsAvailableAsync").ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>
        /// Gets the server status and information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Server status information</returns>
        public async Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AiderHttpClient));

            try
            {
                var response = await _httpClient.GetAsync("/status", cancellationToken).ConfigureAwait(false);
                
                if (!((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                {
                    return new ServerStatus
                    {
                        IsOnline = false,
                        ErrorMessage = $"HTTP {response.StatusCode}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                
                // Validate response content
                var validationErrors = ValidationHelper.ValidateAIResponse(content, "Server Status Response");
                if (validationErrors.Any())
                {
                    return new ServerStatus 
                    { 
                        IsOnline = false, 
                        ErrorMessage = $"Invalid server response: {string.Join("; ", validationErrors)}" 
                    };
                }
                
                ServerStatus status;
                try
                {
                    status = JsonSerializer.Deserialize<ServerStatus>(content);
                }
                catch (JsonException ex)
                {
                    return new ServerStatus 
                    { 
                        IsOnline = false, 
                        ErrorMessage = $"Failed to parse server response: {ex.Message}" 
                    };
                }
                
                return status ?? new ServerStatus { IsOnline = false };
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Failed to get server status: {ex.Message}", "AiderHttpClient.GetServerStatusAsync").ConfigureAwait(false);
                return new ServerStatus
                {
                    IsOnline = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Uploads file context to the Aider backend
        /// </summary>
        /// <param name="fileReferences">File references to upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload result</returns>
        public async Task<UploadResult> UploadFileContextAsync(IEnumerable<FileReference> fileReferences, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AiderHttpClient));

            if (fileReferences == null)
                throw new ArgumentNullException(nameof(fileReferences));

            try
            {
                var payload = new
                {
                    files = fileReferences
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/upload-context", httpContent, cancellationToken).ConfigureAwait(false);

                if (!((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return new UploadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                
                // Validate response content
                var validationErrors = ValidationHelper.ValidateAIResponse(responseContent, "Upload Response");
                if (validationErrors.Any())
                {
                    return new UploadResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = $"Invalid upload response: {string.Join("; ", validationErrors)}" 
                    };
                }
                
                UploadResult result;
                try
                {
                    result = JsonSerializer.Deserialize<UploadResult>(responseContent);
                }
                catch (JsonException ex)
                {
                    return new UploadResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = $"Failed to parse upload response: {ex.Message}" 
                    };
                }

                return result ?? new UploadResult { IsSuccess = false, ErrorMessage = "Invalid response" };
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"File context upload failed: {ex.Message}", "AiderHttpClient.UploadFileContextAsync").ConfigureAwait(false);
                return new UploadResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _httpClient?.Dispose();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Represents server status information
    /// </summary>
    public class ServerStatus
    {
        public bool IsOnline { get; set; }
        public string Version { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents file upload result
    /// </summary>
    public class UploadResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int ProcessedFiles { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}