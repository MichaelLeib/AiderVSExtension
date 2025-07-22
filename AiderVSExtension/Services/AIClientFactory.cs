using System;
using System.Net.Http;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Security;
// using OpenAI;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Factory for creating AI client instances
    /// </summary>
    public class AIClientFactory : IAIClientFactory, IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public AIClientFactory()
        {
            _httpClient = CertificatePinning.CreateSecureHttpClient("https://api.openai.com");
        }

        /// <summary>
        /// Creates an OpenAI client for the specified configuration
        /// </summary>
        // public OpenAIClient CreateOpenAIClient(AIModelConfiguration config)
        // {
        //     if (config == null)
        //         throw new ArgumentNullException(nameof(config));

        //     if (config.Provider != AIProvider.ChatGPT)
        //         throw new ArgumentException("Configuration must be for ChatGPT provider", nameof(config));

        //     if (string.IsNullOrEmpty(config.ApiKey))
        //         throw new ArgumentException("API key is required for OpenAI client", nameof(config));

        //     var client = new OpenAIClient(config.ApiKey);
        //     return client;
        // }

        /// <summary>
        /// Creates an HTTP client for Anthropic Claude API
        /// </summary>
        public HttpClient CreateClaudeClient(AIModelConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (config.Provider != AIProvider.Claude)
                throw new ArgumentException("Configuration must be for Claude provider", nameof(config));

            if (string.IsNullOrEmpty(config.ApiKey))
                throw new ArgumentException("API key is required for Claude client", nameof(config));

            var secureEndpoint = SecureUrlBuilder.EnforceHttps(config.Endpoint ?? "https://api.anthropic.com/");
            var client = CertificatePinning.CreateSecureHttpClient(secureEndpoint);
            client.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            return client;
        }

        /// <summary>
        /// Creates an HTTP client for Ollama API
        /// </summary>
        public HttpClient CreateOllamaClient(AIModelConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (config.Provider != AIProvider.Ollama)
                throw new ArgumentException("Configuration must be for Ollama provider", nameof(config));

            var endpoint = SecureUrlBuilder.EnforceHttps(config.Endpoint ?? "http://localhost:11434/");
            // For Ollama, disable certificate pinning since it's typically local
            var client = CertificatePinning.CreateSecureHttpClient(endpoint, enablePinning: false);
            client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

            return client;
        }

        /// <summary>
        /// Creates an appropriate client for the given configuration
        /// </summary>
        public object CreateClient(AIModelConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return config.Provider switch
            {
                // AIProvider.ChatGPT => CreateOpenAIClient(config),
                AIProvider.Claude => CreateClaudeClient(config),
                AIProvider.Ollama => CreateOllamaClient(config),
                _ => throw new NotSupportedException($"Provider {config.Provider} is not supported")
            };
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources
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
                    _httpClient?.Dispose();
                }

                _disposed = true;
            }
        }

        #endregion
    }
}