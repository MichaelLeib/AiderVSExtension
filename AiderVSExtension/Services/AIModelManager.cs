using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Security;
using Newtonsoft.Json;

namespace AiderVSExtension.Services
{
    public class AIModelManager : IAIModelManager, IDisposable
    {
        public event EventHandler<ModelChangedEventArgs> ActiveModelChanged;

        private AIModelConfiguration _activeModel;
        private readonly IConfigurationService _configService;
        private readonly IErrorHandler _errorHandler;
        private readonly List<AIModelConfiguration> _availableModels;
        private readonly HttpClient _httpClient;

        public AIModelManager(IConfigurationService configService, IErrorHandler errorHandler)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _availableModels = new List<AIModelConfiguration>();
            _httpClient = new HttpClient();
        }

        public AIModelConfiguration ActiveModel
        {
            get => _activeModel;
            private set
            {
                if (_activeModel != value)
                {
                    var previousModel = _activeModel;
                    _activeModel = value;
                    OnActiveModelChanged(previousModel, value);
                }
            }
        }

        public async Task InitializeAsync()
        {
            // Load available models from configuration
            var modelConfigs = await _configService.GetAllModelConfigurationsAsync();
            _availableModels.Clear();
            _availableModels.AddRange(modelConfigs);

            // Set the active model based on configuration
            var activeModelId = await _configService.GetActiveModelIdAsync();
            if (!string.IsNullOrEmpty(activeModelId))
            {
                var activeModel = _availableModels.FirstOrDefault(m => m.Id == activeModelId);
                if (activeModel != null)
                {
                    _activeModel = activeModel;
                }
            }
        }

        public Task<IEnumerable<AIModelConfiguration>> GetAvailableModelsAsync()
        {
            return Task.FromResult<IEnumerable<AIModelConfiguration>>(_availableModels.AsEnumerable());
        }

        public Task<IEnumerable<AIModelConfiguration>> GetAvailableModelsAsync(AIProvider provider)
        {
            var filteredModels = _availableModels.Where(m => m.Provider == provider);
            return Task.FromResult<IEnumerable<AIModelConfiguration>>(filteredModels);
        }

        public async Task<bool> SetActiveModelAsync(AIProvider provider, string modelName)
        {
            var targetModel = _availableModels.FirstOrDefault(m => 
                m.Provider == provider && 
                string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase));

            if (targetModel == null)
            {
                return false;
            }

            // Test connection before setting as active
            var connectionResult = await TestConnectionAsync(targetModel);
            if (!connectionResult.IsSuccessful)
            {
                return false;
            }

            ActiveModel = targetModel;
            
            // Save the active model selection to configuration
            await _configService.SetActiveModelIdAsync(targetModel.Id);
            
            return true;
        }

        public async Task<CompletionResponse> GetCompletionAsync(CompletionRequest request)
        {
            if (ActiveModel == null)
            {
                throw new InvalidOperationException("No active AI model configured.");
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                switch (ActiveModel.Provider)
                {
                    case AIProvider.ChatGPT:
                        return await GetOpenAICompletionAsync(request);
                    case AIProvider.Claude:
                        return await GetClaudeCompletionAsync(request);
                    case AIProvider.Ollama:
                        return await GetOllamaCompletionAsync(request);
                    default:
                        throw new NotSupportedException($"Provider {ActiveModel.Provider} is not supported.");
                }
            }
            catch (Exception ex)
            {
                return new CompletionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ChatResponse> SendChatMessageAsync(AiderVSExtension.Models.ChatMessage message)
        {
            if (ActiveModel == null)
            {
                throw new InvalidOperationException("No active AI model configured.");
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                switch (ActiveModel.Provider)
                {
                    case AIProvider.ChatGPT:
                        return await SendOpenAIChatMessageAsync(message);
                    case AIProvider.Claude:
                        return await SendClaudeChatMessageAsync(message);
                    case AIProvider.Ollama:
                        return await SendOllamaChatMessageAsync(message);
                    default:
                        throw new NotSupportedException($"Provider {ActiveModel.Provider} is not supported.");
                }
            }
            catch (Exception ex)
            {
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private void OnActiveModelChanged(AIModelConfiguration previousModel, AIModelConfiguration newModel)
        {
            var args = new ModelChangedEventArgs
            {
                PreviousModel = previousModel,
                NewModel = newModel
            };
            ActiveModelChanged?.Invoke(this, args);
        }

        public async Task<ChatResponse> SendChatAsync(IEnumerable<AiderVSExtension.Models.ChatMessage> messages)
        {
            try
            {
                if (_activeModel == null)
                {
                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active AI model configured"
                    };
                }

                switch (_activeModel.Provider)
                {
                    case AIProvider.ChatGPT:
                        return await SendOpenAIChat(messages);
                    case AIProvider.Claude:
                        return await SendClaudeChat(messages);
                    case AIProvider.Ollama:
                        return await SendOllamaChat(messages);
                    default:
                        return new ChatResponse
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Unsupported provider: {_activeModel.Provider}"
                        };
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Error sending chat: {ex.Message}", ex, "AIModelManager.SendChatAsync");
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #region Provider-specific implementation stubs

        private async Task<bool> TestOpenAIConnectionAsync(AIModelConfiguration modelConfig)
        {
            try
            {
                if (string.IsNullOrEmpty(modelConfig.ApiKey))
                {
                    return false;
                }

                // Test connection by making a simple models list request
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {modelConfig.ApiKey}");
                    var response = await httpClient.GetAsync("https://api.openai.com/v1/models");
                    return ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenAI connection test failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestClaudeConnectionAsync(AIModelConfiguration modelConfig)
        {
            try
            {
                if (string.IsNullOrEmpty(modelConfig.ApiKey))
                {
                    return false;
                }

                // Test connection with a simple messages API call
                var request = new
                {
                    model = modelConfig.ModelName ?? "claude-3-sonnet-20240229",
                    max_tokens = 10,
                    messages = new[]
                    {
                        new { role = "user", content = "Hello" }
                    }
                };

                var json = SecureJsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"))
                {
                    httpRequest.Headers.Add("x-api-key", modelConfig.ApiKey);
                    httpRequest.Headers.Add("anthropic-version", "2023-06-01");
                    httpRequest.Content = content;

                    var response = await _httpClient.SendAsync(httpRequest);
                    return ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Claude connection test failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestOllamaConnectionAsync(AIModelConfiguration modelConfig)
        {
            try
            {
                if (string.IsNullOrEmpty(modelConfig.EndpointUrl))
                {
                    return false;
                }

                // Test connection by hitting the tags endpoint
                var tagsUrl = $"{modelConfig.EndpointUrl.TrimEnd('/')}/api/tags";
                using (var request = new HttpRequestMessage(HttpMethod.Get, tagsUrl))
                {
                    var response = await _httpClient.SendAsync(request);
                    return ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ollama connection test failed: {ex.Message}");
                return false;
            }
        }

        private async Task<CompletionResponse> GetOpenAICompletionAsync(CompletionRequest request)
        {
            try
            {
                if (_activeModel == null || string.IsNullOrEmpty(_activeModel.ApiKey))
                {
                    return new CompletionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active model or API key configured"
                    };
                }

                var startTime = DateTime.UtcNow;

                // Build the chat completion request using HTTP
                var requestData = new
                {
                    model = _activeModel.ModelName ?? "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful coding assistant that provides accurate, concise code completions and explanations." },
                        new { role = "user", content = request.Prompt }
                    },
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature
                };

                var json = SecureJsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
                {
                    httpRequest.Headers.Add("Authorization", $"Bearer {_activeModel.ApiKey}");
                    httpRequest.Content = content;

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseTime = DateTime.UtcNow - startTime;

                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var openaiResponse = SecureJsonSerializer.Deserialize<OpenAIResponse>(responseContent, strict: true);
                        
                        if (openaiResponse?.Choices?.Any() == true)
                        {
                            var choice = openaiResponse.Choices.First();
                            return new CompletionResponse
                            {
                                IsSuccess = true,
                                Content = choice.Message?.Content ?? "",
                                ModelUsed = _activeModel.ModelName,
                                TokensUsed = openaiResponse.Usage?.TotalTokens ?? 0,
                                ResponseTime = responseTime
                            };
                        }
                    }

                    return new CompletionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"OpenAI API error: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenAI completion failed: {ex.Message}");
                return new CompletionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<CompletionResponse> GetClaudeCompletionAsync(CompletionRequest request)
        {
            try
            {
                if (_activeModel == null || string.IsNullOrEmpty(_activeModel.ApiKey))
                {
                    return new CompletionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active model or API key configured"
                    };
                }

                var startTime = DateTime.UtcNow;

                var requestData = new
                {
                    model = _activeModel.ModelName ?? "claude-3-sonnet-20240229",
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature,
                    messages = new[]
                    {
                        new { role = "user", content = request.Prompt }
                    }
                };

                var json = SecureJsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"))
                {
                    httpRequest.Headers.Add("x-api-key", _activeModel.ApiKey);
                    httpRequest.Headers.Add("anthropic-version", "2023-06-01");
                    httpRequest.Content = content;

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseTime = DateTime.UtcNow - startTime;

                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var claudeResponse = SecureJsonSerializer.Deserialize<ClaudeResponse>(responseContent, strict: true);
                        
                        return new CompletionResponse
                        {
                            IsSuccess = true,
                            Content = claudeResponse?.Content?.FirstOrDefault()?.Text ?? "",
                            ModelUsed = _activeModel.ModelName,
                            TokensUsed = claudeResponse?.Usage?.OutputTokens ?? 0,
                            ResponseTime = responseTime
                        };
                    }

                    return new CompletionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Claude API error: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Claude completion failed: {ex.Message}");
                return new CompletionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<CompletionResponse> GetOllamaCompletionAsync(CompletionRequest request)
        {
            try
            {
                if (_activeModel == null || string.IsNullOrEmpty(_activeModel.EndpointUrl))
                {
                    return new CompletionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active model or endpoint configured"
                    };
                }

                var startTime = DateTime.UtcNow;

                var requestData = new
                {
                    model = _activeModel.ModelName ?? "llama2",
                    prompt = request.Prompt,
                    options = new
                    {
                        temperature = request.Temperature,
                        num_predict = request.MaxTokens
                    },
                    stream = false
                };

                var json = SecureJsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var generateUrl = $"{_activeModel.EndpointUrl.TrimEnd('/')}/api/generate";
                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, generateUrl))
                {
                    httpRequest.Content = content;

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseTime = DateTime.UtcNow - startTime;

                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var ollamaResponse = SecureJsonSerializer.Deserialize<OllamaResponse>(responseContent, strict: true);
                        
                        return new CompletionResponse
                        {
                            IsSuccess = true,
                            Content = ollamaResponse?.Response ?? "",
                            ModelUsed = _activeModel.ModelName,
                            TokensUsed = 0, // Ollama doesn't return token count in basic API
                            ResponseTime = responseTime
                        };
                    }

                    return new CompletionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Ollama API error: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ollama completion failed: {ex.Message}");
                return new CompletionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<ChatResponse> SendOpenAIChatMessageAsync(AiderVSExtension.Models.ChatMessage message)
        {
            try
            {
                if (_activeModel == null || string.IsNullOrEmpty(_activeModel.ApiKey))
                {
                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active model or API key configured"
                    };
                }

                var startTime = DateTime.UtcNow;

                // Convert our ChatMessage to OpenAI format
                var messages = new List<object>
                {
                    new { role = "system", content = "You are a helpful coding assistant integrated into Visual Studio." },
                    new { role = "user", content = message.Content }
                };

                // Add file context if available
                if (message.References?.Any() == true)
                {
                    var contextMessage = "Here are the referenced files:\n\n";
                    foreach (var fileRef in message.References)
                    {
                        contextMessage += $"File: {fileRef.FilePath}\n";
                        if (!string.IsNullOrEmpty(fileRef.Content))
                        {
                            contextMessage += $"Content:\n{fileRef.Content}\n\n";
                        }
                    }
                    messages.Insert(1, new { role = "user", content = contextMessage });
                }

                var requestData = new
                {
                    model = _activeModel.ModelName ?? "gpt-3.5-turbo",
                    messages = messages,
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var json = SecureJsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
                {
                    httpRequest.Headers.Add("Authorization", $"Bearer {_activeModel.ApiKey}");
                    httpRequest.Content = content;

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseTime = DateTime.UtcNow - startTime;

                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var openaiResponse = SecureJsonSerializer.Deserialize<OpenAIResponse>(responseContent, strict: true);
                        
                        if (openaiResponse?.Choices?.Any() == true)
                        {
                            var choice = openaiResponse.Choices.First();
                            return new ChatResponse
                            {
                                IsSuccess = true,
                                Content = choice.Message?.Content ?? "",
                                ModelUsed = _activeModel.ModelName,
                                TokensUsed = openaiResponse.Usage?.TotalTokens ?? 0,
                                ResponseTime = responseTime,
                                OriginalRequest = message.Content,
                                ResponseType = ChatResponseType.Text
                            };
                        }
                    }

                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"OpenAI API error: {response.StatusCode} - {responseContent}",
                        ResponseType = ChatResponseType.Error
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenAI chat failed: {ex.Message}");
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseType = ChatResponseType.Error
                };
            }
        }

        private async Task<ChatResponse> SendClaudeChatMessageAsync(AiderVSExtension.Models.ChatMessage message)
        {
            try
            {
                if (_activeModel == null || string.IsNullOrEmpty(_activeModel.ApiKey))
                {
                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active model or API key configured"
                    };
                }

                var startTime = DateTime.UtcNow;

                // Build messages array with file context
                var messages = new List<object>
                {
                    new { role = "user", content = message.Content }
                };

                // Add file context if available
                if (message.References?.Any() == true)
                {
                    var contextContent = "Here are the referenced files:\n\n";
                    foreach (var fileRef in message.References)
                    {
                        contextContent += $"File: {fileRef.FilePath}\n";
                        if (!string.IsNullOrEmpty(fileRef.Content))
                        {
                            contextContent += $"Content:\n{fileRef.Content}\n\n";
                        }
                    }
                    messages.Insert(0, new { role = "user", content = contextContent });
                }

                var requestData = new
                {
                    model = _activeModel.ModelName ?? "claude-3-sonnet-20240229",
                    max_tokens = 1000,
                    temperature = 0.7,
                    messages = messages
                };

                var json = SecureJsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"))
                {
                    httpRequest.Headers.Add("x-api-key", _activeModel.ApiKey);
                    httpRequest.Headers.Add("anthropic-version", "2023-06-01");
                    httpRequest.Content = content;

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseTime = DateTime.UtcNow - startTime;

                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var claudeResponse = SecureJsonSerializer.Deserialize<ClaudeResponse>(responseContent, strict: true);
                        
                        return new ChatResponse
                        {
                            IsSuccess = true,
                            Content = claudeResponse?.Content?.FirstOrDefault()?.Text ?? "",
                            ModelUsed = _activeModel.ModelName,
                            TokensUsed = claudeResponse?.Usage?.OutputTokens ?? 0,
                            ResponseTime = responseTime,
                            OriginalRequest = message.Content,
                            ResponseType = ChatResponseType.Text
                        };
                    }

                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Claude API error: {response.StatusCode} - {responseContent}",
                        ResponseType = ChatResponseType.Error
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Claude chat failed: {ex.Message}");
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseType = ChatResponseType.Error
                };
            }
        }

        private async Task<ChatResponse> SendOllamaChatMessageAsync(AiderVSExtension.Models.ChatMessage message)
        {
            try
            {
                if (_activeModel == null || string.IsNullOrEmpty(_activeModel.EndpointUrl))
                {
                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active model or endpoint configured"
                    };
                }

                var startTime = DateTime.UtcNow;

                // Build prompt with file context
                var fullPrompt = message.Content;
                if (message.References?.Any() == true)
                {
                    var contextPrompt = "Here are the referenced files:\n\n";
                    foreach (var fileRef in message.References)
                    {
                        contextPrompt += $"File: {fileRef.FilePath}\n";
                        if (!string.IsNullOrEmpty(fileRef.Content))
                        {
                            contextPrompt += $"Content:\n{fileRef.Content}\n\n";
                        }
                    }
                    fullPrompt = contextPrompt + "\n\nUser question: " + message.Content;
                }

                var requestData = new
                {
                    model = _activeModel.ModelName ?? "llama2",
                    prompt = fullPrompt,
                    options = new
                    {
                        temperature = 0.7,
                        num_predict = 1000
                    },
                    stream = false
                };

                var json = SecureJsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var generateUrl = $"{_activeModel.EndpointUrl.TrimEnd('/')}/api/generate";
                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, generateUrl))
                {
                    httpRequest.Content = content;

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseTime = DateTime.UtcNow - startTime;

                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var ollamaResponse = SecureJsonSerializer.Deserialize<OllamaResponse>(responseContent, strict: true);
                        
                        return new ChatResponse
                        {
                            IsSuccess = true,
                            Content = ollamaResponse?.Response ?? "",
                            ModelUsed = _activeModel.ModelName,
                            TokensUsed = 0, // Ollama doesn't return token count in basic API
                            ResponseTime = responseTime,
                            OriginalRequest = message.Content,
                            ResponseType = ChatResponseType.Text
                        };
                    }

                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Ollama API error: {response.StatusCode} - {responseContent}",
                        ResponseType = ChatResponseType.Error
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ollama chat failed: {ex.Message}");
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseType = ChatResponseType.Error
                };
            }
        }

        private async Task<ChatResponse> SendOpenAIChat(IEnumerable<AiderVSExtension.Models.ChatMessage> messages)
        {
            try
            {
                if (_activeModel == null || string.IsNullOrEmpty(_activeModel.ApiKey))
                {
                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active OpenAI model or API key configured"
                    };
                }

                var startTime = DateTime.UtcNow;

                var chatMessages = new List<object>();
                foreach (var message in messages)
                {
                    var role = message.Type == MessageType.User ? "user" : "assistant";
                    chatMessages.Add(new { role = role, content = message.Content });
                }

                var requestData = new
                {
                    model = _activeModel.ModelName ?? "gpt-3.5-turbo",
                    messages = chatMessages
                };

                var json = SecureJsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
                {
                    httpRequest.Headers.Add("Authorization", $"Bearer {_activeModel.ApiKey}");
                    httpRequest.Content = content;

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseTime = DateTime.UtcNow - startTime;

                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var openaiResponse = SecureJsonSerializer.Deserialize<OpenAIResponse>(responseContent, strict: true);
                        
                        if (openaiResponse?.Choices?.Any() == true)
                        {
                            var choice = openaiResponse.Choices.First();
                            return new ChatResponse
                            {
                                IsSuccess = true,
                                Content = choice.Message?.Content ?? "",
                                ResponseTime = responseTime,
                                ModelUsed = _activeModel.ModelName,
                                ResponseType = ChatResponseType.Text
                            };
                        }
                    }

                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "No response from OpenAI"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseType = ChatResponseType.Error
                };
            }
        }

        private async Task<ChatResponse> SendClaudeChat(IEnumerable<AiderVSExtension.Models.ChatMessage> messages)
        {
            // For now, just use the first message - full conversation support would require more work
            var firstMessage = messages.FirstOrDefault();
            if (firstMessage == null)
            {
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "No messages provided"
                };
            }
            
            return await SendClaudeChatMessageAsync(firstMessage);
        }

        private async Task<ChatResponse> SendOllamaChat(IEnumerable<AiderVSExtension.Models.ChatMessage> messages)
        {
            // For now, just use the first message - full conversation support would require more work  
            var firstMessage = messages.FirstOrDefault();
            if (firstMessage == null)
            {
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "No messages provided"
                };
            }
            
            return await SendOllamaChatMessageAsync(firstMessage);
        }

        #endregion

        /// <summary>
        /// Gets the current model configuration
        /// </summary>
        /// <returns>Current model configuration</returns>
        public AIModelConfiguration GetCurrentModel()
        {
            return _activeModel;
        }

        /// <summary>
        /// Generates a completion using the current AI model
        /// </summary>
        /// <param name="prompt">The prompt for completion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated completion text</returns>
        public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (_activeModel == null)
            {
                throw new InvalidOperationException("No active AI model configured.");
            }

            if (string.IsNullOrEmpty(prompt))
            {
                return string.Empty;
            }

            try
            {
                var request = new CompletionRequest
                {
                    Prompt = prompt,
                    MaxTokens = 100,
                    Temperature = 0.7,
                    Language = "csharp" // Default to C# for now
                };

                var response = await GetCompletionAsync(request);
                return response?.Content ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating completion: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets available models for a specific provider
        /// </summary>
        /// <param name="provider">The AI provider</param>
        /// <returns>List of available model names</returns>
        public async Task<IEnumerable<string>> GetAvailableModelsForProviderAsync(AIProvider provider)
        {
            try
            {
                switch (provider)
                {
                    case AIProvider.ChatGPT:
                        return await GetOpenAIModelsAsync();
                    case AIProvider.Claude:
                        return await GetClaudeModelsAsync();
                    case AIProvider.Ollama:
                        return await GetOllamaModelsAsync();
                    default:
                        return Enumerable.Empty<string>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting models for provider {provider}: {ex.Message}");
                // Return default models on error
                switch (provider)
                {
                    case AIProvider.ChatGPT:
                        return new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" };
                    case AIProvider.Claude:
                        return new[] { "claude-3-opus", "claude-3-sonnet", "claude-3-haiku" };
                    case AIProvider.Ollama:
                        return new[] { "llama2", "codellama", "mistral" };
                    default:
                        return Enumerable.Empty<string>();
                }
            }
        }

        private async Task<IEnumerable<string>> GetOpenAIModelsAsync()
        {
            try
            {
                var config = _availableModels.FirstOrDefault(m => m.Provider == AIProvider.ChatGPT);
                if (config == null || string.IsNullOrEmpty(config.ApiKey))
                {
                    return new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" };
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
                    var response = await httpClient.GetAsync("https://api.openai.com/v1/models");
                    
                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var modelsResponse = SecureJsonSerializer.Deserialize<OpenAIModelsResponse>(content, strict: true);
                        
                        // Filter to only chat models
                        var chatModels = modelsResponse?.Data?
                            .Where(m => m.Id.Contains("gpt"))
                            .OrderByDescending(m => m.Id)
                            .Select(m => m.Id)
                            .ToList();

                        if (chatModels?.Any() == true)
                        {
                            return chatModels;
                        }
                        return new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" };
                    }
                }

                return new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting OpenAI models: {ex.Message}");
                return new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" };
            }
        }

        private async Task<IEnumerable<string>> GetClaudeModelsAsync()
        {
            try
            {
                var config = _availableModels.FirstOrDefault(m => m.Provider == AIProvider.Claude);
                if (config == null || string.IsNullOrEmpty(config.ApiKey))
                {
                    return new[] { "claude-3-5-sonnet-20241022", "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307" };
                }

                // Use HTTP client to fetch available models from Claude API
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
                    httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                    var response = await httpClient.GetAsync("https://api.anthropic.com/v1/models");
                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var modelsResponse = SecureJsonSerializer.Deserialize<dynamic>(content, strict: true);
                        
                        if (modelsResponse?.data != null)
                        {
                            var models = new List<string>();
                            foreach (var model in modelsResponse.data)
                            {
                                if (model?.id != null)
                                {
                                    models.Add(model.id.ToString());
                                }
                            }
                            
                            if (models.Any())
                            {
                                return models;
                            }
                        }
                    }
                }

                // Fallback to known models if API call fails
                return new[] { "claude-3-5-sonnet-20241022", "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307" };
            }
            catch (Exception ex)
            {
                await _errorHandler.LogWarningAsync($"Failed to fetch Claude models: {ex.Message}", "AIModelManager.GetClaudeModelsAsync");
                // Return default models on error
                return new[] { "claude-3-5-sonnet-20241022", "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307" };
            }
        }

        private async Task<IEnumerable<string>> GetOllamaModelsAsync()
        {
            try
            {
                var config = _availableModels.FirstOrDefault(m => m.Provider == AIProvider.Ollama);
                if (config == null || string.IsNullOrEmpty(config.EndpointUrl))
                {
                    return new[] { "llama2", "codellama", "mistral" };
                }

                var tagsUrl = $"{config.EndpointUrl.TrimEnd('/')}/api/tags";
                using (var request = new HttpRequestMessage(HttpMethod.Get, tagsUrl))
                {
                    var response = await _httpClient.SendAsync(request);
                    if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var tagsResponse = SecureJsonSerializer.Deserialize<OllamaTagsResponse>(responseContent, strict: true);
                        
                        var models = tagsResponse?.Models?
                            .Select(m => m.Name)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .ToList();
                        
                        if (models?.Any() == true)
                        {
                            return models;
                        }
                        return new[] { "llama2", "codellama", "mistral" };
                    }
                }

                return new[] { "llama2", "codellama", "mistral" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting Ollama models: {ex.Message}");
                return new[] { "llama2", "codellama", "mistral" };
            }
        }

        /// <summary>
        /// Gets all available models
        /// </summary>
        /// <returns>List of available model configurations</returns>
        public IEnumerable<AIModelConfiguration> GetAvailableModels()
        {
            return _availableModels.AsEnumerable();
        }

        /// <summary>
        /// Sets the active AI model
        /// </summary>
        /// <param name="provider">The AI provider to use</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SetActiveModelAsync(AIProvider provider)
        {
            var model = _availableModels.FirstOrDefault(m => m.Provider == provider);
            if (model != null)
            {
                await SetActiveModelAsync(provider, model.ModelName);
            }
        }

        /// <summary>
        /// Tests connection to the specified AI model
        /// </summary>
        /// <param name="configuration">The model configuration to test</param>
        /// <returns>Connection test result</returns>
        public async Task<ConnectionTestResult> TestConnectionAsync(AIModelConfiguration configuration)
        {
            var startTime = DateTime.UtcNow;
            bool isSuccessful = false;
            
            switch (configuration?.Provider)
            {
                case AIProvider.ChatGPT:
                    isSuccessful = await TestOpenAIConnectionAsync(configuration);
                    break;
                case AIProvider.Claude:
                    isSuccessful = await TestClaudeConnectionAsync(configuration);
                    break;
                case AIProvider.Ollama:
                    isSuccessful = await TestOllamaConnectionAsync(configuration);
                    break;
                default:
                    isSuccessful = false;
                    break;
            }
            
            var responseTime = DateTime.UtcNow - startTime;

            return new ConnectionTestResult
            {
                IsSuccessful = isSuccessful,
                ResponseTime = responseTime,
                ErrorMessage = isSuccessful ? null : "Connection test failed",
                ModelVersion = configuration?.ModelName
            };
        }

        #region IDisposable Implementation

        private bool _disposed = false;

        /// <summary>
        /// Disposes managed and unmanaged resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _httpClient?.Dispose();
                }

                _disposed = true;
            }
        }

        #endregion
    }

    // OpenAI API response models
    public class OpenAIResponse
    {
        [JsonProperty("choices")]
        public List<OpenAIChoice> Choices { get; set; }

        [JsonProperty("usage")]
        public OpenAIUsage Usage { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonProperty("message")]
        public OpenAIMessage Message { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class OpenAIMessage
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }
    }

    public class OpenAIUsage
    {
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class OpenAIModelsResponse
    {
        [JsonProperty("data")]
        public List<OpenAIModel> Data { get; set; }
    }

    public class OpenAIModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    // Claude API response models
    public class ClaudeResponse
    {
        [JsonProperty("content")]
        public List<ClaudeContent> Content { get; set; }

        [JsonProperty("usage")]
        public ClaudeUsage Usage { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }
    }

    public class ClaudeContent
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class ClaudeUsage
    {
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }

    // Ollama API response models
    public class OllamaResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }
    }

    public class OllamaTagsResponse
    {
        [JsonProperty("models")]
        public List<OllamaModel> Models { get; set; }
    }

    public class OllamaModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("modified_at")]
        public DateTime ModifiedAt { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }
}
