using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Security;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for guided configuration setup and management
    /// </summary>
    public class ConfigurationWizardService : IConfigurationWizardService, IDisposable
    {
        private readonly IAdvancedConfigurationService _configurationService;
        private readonly IConfigurationValidationService _validationService;
        private readonly INotificationService _notificationService;
        private readonly IErrorHandler _errorHandler;
        private readonly Dictionary<string, WizardStep> _steps = new Dictionary<string, WizardStep>();
        private readonly Dictionary<string, WizardSession> _activeSessions = new Dictionary<string, WizardSession>();
        private bool _disposed = false;

        public event EventHandler<WizardStepCompletedEventArgs> StepCompleted;
        public event EventHandler<WizardCompletedEventArgs> WizardCompleted;
        public event EventHandler<WizardCancelledEventArgs> WizardCancelled;

        public ConfigurationWizardService(
            IAdvancedConfigurationService configurationService,
            IConfigurationValidationService validationService,
            INotificationService notificationService,
            IErrorHandler errorHandler)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
            InitializeWizardSteps();
        }

        /// <summary>
        /// Starts a new configuration wizard session
        /// </summary>
        /// <param name="wizardType">Type of wizard to start</param>
        /// <param name="options">Wizard options</param>
        /// <returns>Wizard session</returns>
        public async Task<WizardSession> StartWizardAsync(WizardType wizardType, WizardOptions options = null)
        {
            try
            {
                var session = new WizardSession
                {
                    Id = Guid.NewGuid().ToString(),
                    WizardType = wizardType,
                    Options = options ?? new WizardOptions(),
                    StartTime = DateTime.UtcNow,
                    CurrentStepIndex = 0,
                    IsActive = true,
                    Data = new Dictionary<string, object>()
                };

                // Initialize steps for this wizard type
                session.Steps = GetStepsForWizardType(wizardType);
                
                // Store active session
                _activeSessions[session.Id] = session;
                
                // Start with first step
                if (session.Steps.Any())
                {
                    session.CurrentStep = session.Steps.First();
                    await PrepareStepAsync(session, session.CurrentStep);
                }
                
                await _notificationService.ShowInfoAsync($"Started {wizardType} wizard", "Configuration Wizard");
                
                return session;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationWizardService.StartWizardAsync");
                throw;
            }
        }

        /// <summary>
        /// Processes wizard step input and advances to next step
        /// </summary>
        /// <param name="sessionId">Wizard session ID</param>
        /// <param name="stepData">Step input data</param>
        /// <returns>Next step or null if wizard is complete</returns>
        public async Task<WizardStep> ProcessStepAsync(string sessionId, Dictionary<string, object> stepData)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var session))
                {
                    throw new InvalidOperationException($"Wizard session {sessionId} not found");
                }

                var currentStep = session.CurrentStep;
                if (currentStep == null)
                {
                    throw new InvalidOperationException("No current step in wizard session");
                }

                // Validate step data
                var validationResult = await ValidateStepDataAsync(currentStep, stepData);
                if (!validationResult.IsValid)
                {
                    currentStep.ValidationErrors = validationResult.ValidationErrors;
                    currentStep.ValidationWarnings = validationResult.ValidationWarnings;
                    return currentStep;
                }

                // Process step data
                await ProcessStepDataAsync(session, currentStep, stepData);
                
                // Mark step as completed
                currentStep.IsCompleted = true;
                currentStep.CompletedAt = DateTime.UtcNow;
                
                // Fire step completed event
                StepCompleted?.Invoke(this, new WizardStepCompletedEventArgs
                {
                    SessionId = sessionId,
                    Step = currentStep,
                    StepData = stepData
                });
                
                // Move to next step
                session.CurrentStepIndex++;
                if (session.CurrentStepIndex < session.Steps.Count)
                {
                    session.CurrentStep = session.Steps[session.CurrentStepIndex];
                    await PrepareStepAsync(session, session.CurrentStep);
                    return session.CurrentStep;
                }
                else
                {
                    // Wizard completed
                    await CompleteWizardAsync(session);
                    return null;
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationWizardService.ProcessStepAsync");
                throw;
            }
        }

        /// <summary>
        /// Goes back to the previous step
        /// </summary>
        /// <param name="sessionId">Wizard session ID</param>
        /// <returns>Previous step or null if at first step</returns>
        public async Task<WizardStep> GoBackAsync(string sessionId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var session))
                {
                    throw new InvalidOperationException($"Wizard session {sessionId} not found");
                }

                if (session.CurrentStepIndex > 0)
                {
                    session.CurrentStepIndex--;
                    session.CurrentStep = session.Steps[session.CurrentStepIndex];
                    
                    // Reset step completion status
                    session.CurrentStep.IsCompleted = false;
                    session.CurrentStep.CompletedAt = null;
                    session.CurrentStep.ValidationErrors.Clear();
                    session.CurrentStep.ValidationWarnings.Clear();
                    
                    await PrepareStepAsync(session, session.CurrentStep);
                    return session.CurrentStep;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationWizardService.GoBackAsync");
                throw;
            }
        }

        /// <summary>
        /// Cancels a wizard session
        /// </summary>
        /// <param name="sessionId">Wizard session ID</param>
        /// <returns>True if cancelled successfully</returns>
        public async Task<bool> CancelWizardAsync(string sessionId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var session))
                {
                    return false;
                }

                session.IsActive = false;
                session.IsCancelled = true;
                session.EndTime = DateTime.UtcNow;
                
                _activeSessions.Remove(sessionId);
                
                // Fire cancelled event
                WizardCancelled?.Invoke(this, new WizardCancelledEventArgs
                {
                    SessionId = sessionId,
                    CancelledAt = DateTime.UtcNow
                });
                
                await _notificationService.ShowInfoAsync("Configuration wizard cancelled", "Wizard Cancelled");
                
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationWizardService.CancelWizardAsync");
                return false;
            }
        }

        /// <summary>
        /// Gets the current wizard session
        /// </summary>
        /// <param name="sessionId">Wizard session ID</param>
        /// <returns>Wizard session or null if not found</returns>
        public WizardSession GetSession(string sessionId)
        {
            return _activeSessions.GetValueOrDefault(sessionId);
        }

        /// <summary>
        /// Gets all active wizard sessions
        /// </summary>
        /// <returns>List of active sessions</returns>
        public IEnumerable<WizardSession> GetActiveSessions()
        {
            return _activeSessions.Values.Where(s => s.IsActive).ToList();
        }

        /// <summary>
        /// Gets wizard templates for quick setup
        /// </summary>
        /// <returns>List of wizard templates</returns>
        public async Task<IEnumerable<WizardTemplate>> GetWizardTemplatesAsync()
        {
            try
            {
                var templates = new List<WizardTemplate>
                {
                    new WizardTemplate
                    {
                        Id = "quick-start",
                        Name = "Quick Start",
                        Description = "Get started with Aider quickly using recommended settings",
                        WizardType = WizardType.QuickStart,
                        EstimatedDuration = TimeSpan.FromMinutes(5),
                        Difficulty = WizardDifficulty.Beginner,
                        PreConfiguredData = new Dictionary<string, object>
                        {
                            ["provider"] = AIProvider.ChatGPT,
                            ["model"] = "gpt-4",
                            ["temperature"] = 0.7
                        }
                    },
                    new WizardTemplate
                    {
                        Id = "ai-model-setup",
                        Name = "AI Model Setup",
                        Description = "Configure AI models and providers",
                        WizardType = WizardType.AIModelSetup,
                        EstimatedDuration = TimeSpan.FromMinutes(10),
                        Difficulty = WizardDifficulty.Intermediate,
                        PreConfiguredData = new Dictionary<string, object>()
                    },
                    new WizardTemplate
                    {
                        Id = "advanced-configuration",
                        Name = "Advanced Configuration",
                        Description = "Configure advanced settings and parameters",
                        WizardType = WizardType.AdvancedConfiguration,
                        EstimatedDuration = TimeSpan.FromMinutes(15),
                        Difficulty = WizardDifficulty.Advanced,
                        PreConfiguredData = new Dictionary<string, object>()
                    },
                    new WizardTemplate
                    {
                        Id = "profile-migration",
                        Name = "Profile Migration",
                        Description = "Migrate configuration from another profile or import",
                        WizardType = WizardType.ProfileMigration,
                        EstimatedDuration = TimeSpan.FromMinutes(8),
                        Difficulty = WizardDifficulty.Intermediate,
                        PreConfiguredData = new Dictionary<string, object>()
                    }
                };

                return templates;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationWizardService.GetWizardTemplatesAsync");
                return new List<WizardTemplate>();
            }
        }

        /// <summary>
        /// Creates a configuration from wizard template
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="customizations">Custom settings</param>
        /// <returns>Configuration profile</returns>
        public async Task<ConfigurationProfile> CreateFromTemplateAsync(string templateId, Dictionary<string, object> customizations = null)
        {
            try
            {
                var templates = await GetWizardTemplatesAsync();
                var template = templates.FirstOrDefault(t => t.Id == templateId);
                
                if (template == null)
                {
                    throw new ArgumentException($"Template {templateId} not found");
                }

                var profile = new ConfigurationProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Profile from {template.Name}",
                    Description = $"Created from wizard template: {template.Description}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "ConfigurationWizard",
                    Version = "1.0",
                    Settings = new Dictionary<string, object>()
                };

                // Apply template data
                foreach (var kvp in template.PreConfiguredData)
                {
                    profile.Settings[kvp.Key] = kvp.Value;
                }

                // Apply customizations
                if (customizations != null)
                {
                    foreach (var kvp in customizations)
                    {
                        profile.Settings[kvp.Key] = kvp.Value;
                    }
                }

                // Create AI model configuration if needed
                if (profile.Settings.ContainsKey("provider") && profile.Settings.ContainsKey("model"))
                {
                    profile.AIModelConfiguration = new AIModelConfiguration
                    {
                        Provider = (AIProvider)profile.Settings["provider"],
                        ModelName = profile.Settings["model"].ToString(),
                        IsEnabled = true
                    };
                }

                // Create the profile
                var createdProfile = await _configurationService.CreateProfileAsync(profile);
                
                await _notificationService.ShowSuccessAsync($"Created configuration profile from template: {template.Name}");
                
                return createdProfile;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationWizardService.CreateFromTemplateAsync");
                throw;
            }
        }

        #region Private Methods

        private void InitializeWizardSteps()
        {
            // Quick start steps
            RegisterStep("quick-start-welcome", new WizardStep
            {
                Id = "quick-start-welcome",
                Title = "Welcome to Aider",
                Description = "Let's get you started with Aider quickly",
                StepType = WizardStepType.Information,
                IsRequired = true,
                Fields = new List<WizardField>
                {
                    new WizardField { Id = "user-name", Label = "Your Name", Type = WizardFieldType.Text, IsRequired = false },
                    new WizardField { Id = "experience-level", Label = "Experience Level", Type = WizardFieldType.Select, IsRequired = false, Options = new[] { "Beginner", "Intermediate", "Advanced" } }
                }
            });

            RegisterStep("quick-start-provider", new WizardStep
            {
                Id = "quick-start-provider",
                Title = "Choose AI Provider",
                Description = "Select your preferred AI provider",
                StepType = WizardStepType.Selection,
                IsRequired = true,
                Fields = new List<WizardField>
                {
                    new WizardField { Id = "provider", Label = "AI Provider", Type = WizardFieldType.Select, IsRequired = true, Options = new[] { "OpenAI", "Claude", "Ollama" } },
                    new WizardField { Id = "api-key", Label = "API Key", Type = WizardFieldType.Password, IsRequired = true }
                }
            });

            RegisterStep("quick-start-model", new WizardStep
            {
                Id = "quick-start-model",
                Title = "Select Model",
                Description = "Choose the AI model to use",
                StepType = WizardStepType.Selection,
                IsRequired = true,
                Fields = new List<WizardField>
                {
                    new WizardField { Id = "model", Label = "Model", Type = WizardFieldType.Select, IsRequired = true },
                    new WizardField { Id = "temperature", Label = "Temperature", Type = WizardFieldType.Number, IsRequired = false, DefaultValue = 0.7 }
                }
            });

            RegisterStep("quick-start-complete", new WizardStep
            {
                Id = "quick-start-complete",
                Title = "Setup Complete",
                Description = "Your Aider configuration is ready",
                StepType = WizardStepType.Summary,
                IsRequired = false
            });

            // AI Model setup steps
            RegisterStep("ai-model-provider", new WizardStep
            {
                Id = "ai-model-provider",
                Title = "AI Provider Configuration",
                Description = "Configure your AI provider settings",
                StepType = WizardStepType.Configuration,
                IsRequired = true,
                Fields = new List<WizardField>
                {
                    new WizardField { Id = "provider", Label = "Provider", Type = WizardFieldType.Select, IsRequired = true, Options = new[] { "OpenAI", "Claude", "Ollama" } },
                    new WizardField { Id = "api-key", Label = "API Key", Type = WizardFieldType.Password, IsRequired = true },
                    new WizardField { Id = "endpoint", Label = "Custom Endpoint", Type = WizardFieldType.Text, IsRequired = false }
                }
            });

            RegisterStep("ai-model-selection", new WizardStep
            {
                Id = "ai-model-selection",
                Title = "Model Selection",
                Description = "Choose and configure your AI model",
                StepType = WizardStepType.Selection,
                IsRequired = true,
                Fields = new List<WizardField>
                {
                    new WizardField { Id = "model", Label = "Model", Type = WizardFieldType.Select, IsRequired = true },
                    new WizardField { Id = "max-tokens", Label = "Max Tokens", Type = WizardFieldType.Number, IsRequired = false, DefaultValue = 2000 },
                    new WizardField { Id = "temperature", Label = "Temperature", Type = WizardFieldType.Number, IsRequired = false, DefaultValue = 0.7 }
                }
            });

            RegisterStep("ai-model-test", new WizardStep
            {
                Id = "ai-model-test",
                Title = "Test Configuration",
                Description = "Test your AI model configuration",
                StepType = WizardStepType.Validation,
                IsRequired = true,
                Fields = new List<WizardField>
                {
                    new WizardField { Id = "test-prompt", Label = "Test Prompt", Type = WizardFieldType.Text, IsRequired = false, DefaultValue = "Hello, how are you?" }
                }
            });

            // Advanced configuration steps
            RegisterStep("advanced-parameters", new WizardStep
            {
                Id = "advanced-parameters",
                Title = "Advanced Parameters",
                Description = "Configure advanced AI model parameters",
                StepType = WizardStepType.Configuration,
                IsRequired = false,
                Fields = new List<WizardField>
                {
                    new WizardField { Id = "top-p", Label = "Top P", Type = WizardFieldType.Number, IsRequired = false, DefaultValue = 1.0 },
                    new WizardField { Id = "top-k", Label = "Top K", Type = WizardFieldType.Number, IsRequired = false, DefaultValue = 40 },
                    new WizardField { Id = "frequency-penalty", Label = "Frequency Penalty", Type = WizardFieldType.Number, IsRequired = false, DefaultValue = 0.0 },
                    new WizardField { Id = "presence-penalty", Label = "Presence Penalty", Type = WizardFieldType.Number, IsRequired = false, DefaultValue = 0.0 }
                }
            });

            RegisterStep("advanced-features", new WizardStep
            {
                Id = "advanced-features",
                Title = "Advanced Features",
                Description = "Configure advanced Aider features",
                StepType = WizardStepType.Configuration,
                IsRequired = false,
                Fields = new List<WizardField>
                {
                    new WizardField { Id = "auto-commit", Label = "Auto Commit", Type = WizardFieldType.Boolean, IsRequired = false, DefaultValue = false },
                    new WizardField { Id = "pretty-print", Label = "Pretty Print", Type = WizardFieldType.Boolean, IsRequired = false, DefaultValue = true },
                    new WizardField { Id = "stream-responses", Label = "Stream Responses", Type = WizardFieldType.Boolean, IsRequired = false, DefaultValue = true }
                }
            });
        }

        private void RegisterStep(string id, WizardStep step)
        {
            _steps[id] = step;
        }

        private List<WizardStep> GetStepsForWizardType(WizardType wizardType)
        {
            return wizardType switch
            {
                WizardType.QuickStart => new List<WizardStep>
                {
                    _steps["quick-start-welcome"],
                    _steps["quick-start-provider"],
                    _steps["quick-start-model"],
                    _steps["quick-start-complete"]
                },
                WizardType.AIModelSetup => new List<WizardStep>
                {
                    _steps["ai-model-provider"],
                    _steps["ai-model-selection"],
                    _steps["ai-model-test"]
                },
                WizardType.AdvancedConfiguration => new List<WizardStep>
                {
                    _steps["ai-model-provider"],
                    _steps["ai-model-selection"],
                    _steps["advanced-parameters"],
                    _steps["advanced-features"],
                    _steps["ai-model-test"]
                },
                _ => new List<WizardStep>()
            };
        }

        private async Task PrepareStepAsync(WizardSession session, WizardStep step)
        {
            // Prepare step based on session data
            switch (step.Id)
            {
                case "quick-start-model":
                case "ai-model-selection":
                    await PrepareModelSelectionStepAsync(session, step);
                    break;
                case "ai-model-test":
                    await PrepareTestStepAsync(session, step);
                    break;
            }
        }

        private async Task PrepareModelSelectionStepAsync(WizardSession session, WizardStep step)
        {
            // Get available models based on selected provider
            if (session.Data.TryGetValue("provider", out var providerObj) && providerObj is AIProvider provider)
            {
                var modelField = step.Fields.FirstOrDefault(f => f.Id == "model");
                if (modelField != null)
                {
                    modelField.Options = GetModelsForProvider(provider);
                }
            }
        }

        private async Task PrepareTestStepAsync(WizardSession session, WizardStep step)
        {
            // Prepare test configuration
            step.Instructions = "Click 'Test Configuration' to verify your settings work correctly.";
        }

        private string[] GetModelsForProvider(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.ChatGPT => new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" },
                AIProvider.Claude => new[] { "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307" },
                AIProvider.Ollama => new[] { "llama2", "llama2:7b", "llama2:13b", "codellama", "mistral" },
                _ => new string[0]
            };
        }

        private async Task<ConfigurationValidationResult> ValidateStepDataAsync(WizardStep step, Dictionary<string, object> stepData)
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            foreach (var field in step.Fields.Where(f => f.IsRequired))
            {
                if (!stepData.ContainsKey(field.Id) || stepData[field.Id] == null || string.IsNullOrEmpty(stepData[field.Id].ToString()))
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = field.Id,
                        ErrorMessage = $"{field.Label} is required",
                        ErrorCode = "REQUIRED_FIELD_MISSING"
                    });
                }
            }

            return result;
        }

        private async Task ProcessStepDataAsync(WizardSession session, WizardStep step, Dictionary<string, object> stepData)
        {
            // Store step data in session
            foreach (var kvp in stepData)
            {
                session.Data[kvp.Key] = kvp.Value;
            }

            // Perform step-specific processing
            switch (step.Id)
            {
                case "ai-model-test":
                    await ProcessTestStepAsync(session, stepData);
                    break;
            }
        }

        private async Task ProcessTestStepAsync(WizardSession session, Dictionary<string, object> stepData)
        {
            // Test AI model configuration
            try
            {
                var modelConfig = BuildModelConfigFromSession(session);
                var validationResult = await _validationService.ValidateAIModelConfigurationAsync(modelConfig);
                
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", validationResult.ValidationErrors.Select(e => e.ErrorMessage))}");
                }
                
                // Test the actual model connection
                var testResult = await TestModelConnectionAsync(config);
                if (testResult.IsSuccessfulful)
                {
                    await _notificationService.ShowSuccessAsync("Configuration test passed! Model is accessible.", "Test Results");
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"Configuration test failed: {testResult.ErrorMessage}", "Test Results");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Configuration test failed: {ex.Message}", ex);
            }
        }

        private AIModelConfiguration BuildModelConfigFromSession(WizardSession session)
        {
            var config = new AIModelConfiguration
            {
                Provider = (AIProvider)session.Data.GetValueOrDefault("provider", AIProvider.ChatGPT),
                ModelName = session.Data.GetValueOrDefault("model", "gpt-4").ToString(),
                ApiKey = session.Data.GetValueOrDefault("api-key", "").ToString(),
                Endpoint = session.Data.GetValueOrDefault("endpoint", "").ToString(),
                IsEnabled = true
            };

            // Add advanced parameters if present
            if (session.Data.ContainsKey("temperature"))
            {
                config.Parameters = config.Parameters ?? new Dictionary<string, object>();
                config.Parameters["temperature"] = session.Data["temperature"];
            }

            return config;
        }

        private async Task CompleteWizardAsync(WizardSession session)
        {
            try
            {
                session.IsActive = false;
                session.IsCompleted = true;
                session.EndTime = DateTime.UtcNow;

                // Create configuration profile from wizard data
                var profile = await CreateProfileFromSessionAsync(session);
                
                // Remove from active sessions
                _activeSessions.Remove(session.Id);
                
                // Fire completed event
                WizardCompleted?.Invoke(this, new WizardCompletedEventArgs
                {
                    SessionId = session.Id,
                    Profile = profile,
                    CompletedAt = DateTime.UtcNow
                });
                
                await _notificationService.ShowSuccessAsync("Configuration wizard completed successfully!", "Wizard Complete");
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationWizardService.CompleteWizardAsync");
                throw;
            }
        }

        private async Task<ConfigurationProfile> CreateProfileFromSessionAsync(WizardSession session)
        {
            var profile = new ConfigurationProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Wizard Profile - {session.WizardType}",
                Description = $"Created by {session.WizardType} wizard on {DateTime.UtcNow:yyyy-MM-dd}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "ConfigurationWizard",
                Version = "1.0",
                Settings = new Dictionary<string, object>(session.Data)
            };

            // Build AI model configuration
            if (session.Data.ContainsKey("provider") && session.Data.ContainsKey("model"))
            {
                profile.AIModelConfiguration = BuildModelConfigFromSession(session);
            }

            // Create the profile
            return await _configurationService.CreateProfileAsync(profile);
        }

        private async Task<(bool IsSuccessful, string ErrorMessage)> TestModelConnectionAsync(AIModelConfiguration config)
        {
            try
            {
                switch (config.Provider)
                {
                    case AIProvider.ChatGPT:
                        return await TestOpenAIConnectionAsync(config);
                    case AIProvider.Claude:
                        return await TestClaudeConnectionAsync(config);
                    case AIProvider.Ollama:
                        return await TestOllamaConnectionAsync(config);
                    default:
                        return (false, "Unknown AI provider");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Connection test failed: {ex.Message}");
            }
        }

        private async Task<(bool IsSuccessful, string ErrorMessage)> TestOpenAIConnectionAsync(AIModelConfiguration config)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
                
                var response = await httpClient.GetAsync("https://api.openai.com/v1/models");
                if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                {
                    return (true, "OpenAI connection successful");
                }
                else
                {
                    return (false, $"OpenAI API returned {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"OpenAI connection failed: {ex.Message}");
            }
        }

        private async Task<(bool IsSuccessful, string ErrorMessage)> TestClaudeConnectionAsync(AIModelConfiguration config)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                
                var response = await httpClient.GetAsync("https://api.anthropic.com/v1/models");
                if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                {
                    return (true, "Claude connection successful");
                }
                else
                {
                    return (false, $"Claude API returned {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Claude connection failed: {ex.Message}");
            }
        }

        private async Task<(bool IsSuccessful, string ErrorMessage)> TestOllamaConnectionAsync(AIModelConfiguration config)
        {
            try
            {
                var endpoint = !string.IsNullOrEmpty(config.Endpoint) 
                    ? SecureUrlBuilder.EnforceHttps(config.Endpoint) 
                    : "http://localhost:11434";
                // For Ollama, disable certificate pinning since it's typically local
                using var httpClient = CertificatePinning.CreateSecureHttpClient(endpoint, enablePinning: false);
                
                var response = await httpClient.GetAsync($"{endpoint}/api/tags");
                if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                {
                    return (true, "Ollama connection successful");
                }
                else
                {
                    return (false, $"Ollama API returned {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ollama connection failed: {ex.Message}");
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _steps.Clear();
                _activeSessions.Clear();
                _disposed = true;
            }
        }
    }
}