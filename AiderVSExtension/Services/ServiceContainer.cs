using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service container for dependency injection and service management
    /// </summary>
    public class ServiceContainer : IDisposable
    {
        private readonly Dictionary<Type, object> _services;
        private readonly Dictionary<Type, Func<object>> _serviceFactories;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public ServiceContainer()
        {
            _services = new Dictionary<Type, object>();
            _serviceFactories = new Dictionary<Type, Func<object>>();
        }

        /// <summary>
        /// Initializes the service container with default services
        /// </summary>
        /// <param name="package">The Visual Studio package</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        public async Task InitializeAsync(AsyncPackage package, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                // Register default service factories
                RegisterServiceFactories();
            }, cancellationToken);
        }

        /// <summary>
        /// Registers a service instance
        /// </summary>
        /// <typeparam name="TInterface">The service interface type</typeparam>
        /// <param name="instance">The service instance</param>
        public void RegisterService<TInterface>(TInterface instance) where TInterface : class
        {
            lock (_lock)
            {
                _services[typeof(TInterface)] = instance;
            }
        }

        /// <summary>
        /// Registers a service factory
        /// </summary>
        /// <typeparam name="TInterface">The service interface type</typeparam>
        /// <param name="factory">The factory function</param>
        public void RegisterService<TInterface>(Func<TInterface> factory) where TInterface : class
        {
            lock (_lock)
            {
                _serviceFactories[typeof(TInterface)] = () => factory();
            }
        }

        /// <summary>
        /// Registers a service factory with dependencies
        /// </summary>
        /// <typeparam name="TInterface">The service interface type</typeparam>
        /// <typeparam name="TImplementation">The service implementation type</typeparam>
        public void RegisterService<TInterface, TImplementation>() 
            where TInterface : class 
            where TImplementation : class, TInterface, new()
        {
            lock (_lock)
            {
                _serviceFactories[typeof(TInterface)] = () => new TImplementation();
            }
        }

        /// <summary>
        /// Gets a service instance
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>The service instance</returns>
        public T GetService<T>() where T : class
        {
            return GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Gets a service instance by type
        /// </summary>
        /// <param name="serviceType">The service type</param>
        /// <returns>The service instance</returns>
        public object GetService(Type serviceType)
        {
            lock (_lock)
            {
                // Check if we have a cached instance
                if (_services.TryGetValue(serviceType, out object cachedService))
                {
                    return cachedService;
                }

                // Check if we have a factory
                if (_serviceFactories.TryGetValue(serviceType, out Func<object> factory))
                {
                    var service = factory();
                    _services[serviceType] = service;
                    return service;
                }

                return null;
            }
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>True if the service is registered</returns>
        public bool IsServiceRegistered<T>()
        {
            return IsServiceRegistered(typeof(T));
        }

        /// <summary>
        /// Checks if a service is registered by type
        /// </summary>
        /// <param name="serviceType">The service type</param>
        /// <returns>True if the service is registered</returns>
        public bool IsServiceRegistered(Type serviceType)
        {
            lock (_lock)
            {
                return _services.ContainsKey(serviceType) || _serviceFactories.ContainsKey(serviceType);
            }
        }

        /// <summary>
        /// Unregisters a service
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        public void UnregisterService<T>()
        {
            UnregisterService(typeof(T));
        }

        /// <summary>
        /// Unregisters a service by type
        /// </summary>
        /// <param name="serviceType">The service type</param>
        public void UnregisterService(Type serviceType)
        {
            lock (_lock)
            {
                if (_services.TryGetValue(serviceType, out object service))
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _services.Remove(serviceType);
                }

                _serviceFactories.Remove(serviceType);
            }
        }

        /// <summary>
        /// Gets all registered service types
        /// </summary>
        /// <returns>Collection of registered service types</returns>
        public IEnumerable<Type> GetRegisteredServiceTypes()
        {
            lock (_lock)
            {
                var types = new HashSet<Type>();
                foreach (var type in _services.Keys)
                {
                    types.Add(type);
                }
                foreach (var type in _serviceFactories.Keys)
                {
                    types.Add(type);
                }
                return types;
            }
        }

        /// <summary>
        /// Registers default service factories for core interfaces
        /// </summary>
        private void RegisterServiceFactories()
        {
            // Register services without dependencies first
            RegisterService<IErrorHandler, ErrorHandler>();
            RegisterService<IMessageRenderer, MessageRendererService>();
            
            // Configuration service (required by many other services)
            RegisterService<IConfigurationService, ConfigurationService>();
            
            // VS Theming service
            RegisterService<IVSThemingService>(() =>
            {
                // Create a basic implementation for now
                return new VSThemingService();
            });
            
            // Session persistence and state management services (no dependencies)
            RegisterService<IConversationPersistenceService, ConversationPersistenceService>();
            RegisterService<IApplicationStateService, ApplicationStateService>();
            
            // Secure credential service
            RegisterService<ISecureCredentialService>(() =>
            {
                var errorHandler = GetService<IErrorHandler>();
                return new SecureCredentialService(errorHandler);
            });
            
            // Services with dependencies will be registered using factory patterns
            RegisterServiceWithDependencies();
        }

        /// <summary>
        /// Registers services that have dependencies using factory pattern
        /// </summary>
        private void RegisterServiceWithDependencies()
        {
            // Register MessageQueue service (no dependencies)
            RegisterService<IMessageQueue, MessageQueueService>();

            // Core infrastructure services
            RegisterService<ITelemetryService, TelemetryService>();
            RegisterService<ICorrelationService, CorrelationService>();
            RegisterService<ICircuitBreakerService, CircuitBreakerService>();

            // AIModelManager depends on IConfigurationService and IErrorHandler
            RegisterService<IAIModelManager>(() =>
            {
                var configService = GetService<IConfigurationService>();
                var errorHandler = GetService<IErrorHandler>();
                return new AIModelManager(configService, errorHandler);
            });

            // CompletionProvider depends on AIModelManager, ConfigurationService, and ErrorHandler
            RegisterService<ICompletionProvider>(() =>
            {
                var aiModelManager = GetService<IAIModelManager>();
                var configService = GetService<IConfigurationService>();
                var errorHandler = GetService<IErrorHandler>();
                return new CompletionProviderService(aiModelManager, configService, errorHandler);
            });

            // Git services
            RegisterService<IGitService>(() =>
            {
                var errorHandler = GetService<IErrorHandler>();
                return new GitService(errorHandler);
            });

            RegisterService<IGitChatContextProvider>(() =>
            {
                var gitService = GetService<IGitService>();
                var errorHandler = GetService<IErrorHandler>();
                return new GitChatContextProvider(gitService, errorHandler);
            });

            // Retry service
            RegisterService<IRetryService>(() =>
            {
                var errorHandler = GetService<IErrorHandler>();
                return new RetryService(errorHandler);
            });

            // AgentAPI services
            RegisterService<IAiderDependencyChecker>(() =>
            {
                var errorHandler = GetService<IErrorHandler>();
                return new AiderDependencyChecker(errorHandler);
            });

            RegisterService<IAgentApiService>(() =>
            {
                var errorHandler = GetService<IErrorHandler>();
                var configService = GetService<IConfigurationService>();
                var dependencyChecker = GetService<IAiderDependencyChecker>();
                var telemetryService = GetService<ITelemetryService>();
                var circuitBreaker = GetService<ICircuitBreakerService>();
                return new AgentApiService(errorHandler, configService, dependencyChecker, telemetryService, circuitBreaker);
            });

            RegisterService<IAiderSetupManager>(() =>
            {
                var dependencyChecker = GetService<IAiderDependencyChecker>();
                var agentApiService = GetService<IAgentApiService>();
                var errorHandler = GetService<IErrorHandler>();
                var telemetryService = GetService<ITelemetryService>();
                return new AiderSetupManager(dependencyChecker, agentApiService, errorHandler, telemetryService);
            });

            // QuickFixProvider depends on multiple services
            RegisterService<IQuickFixProvider>(() =>
            {
                var serviceProvider = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider;
                var aiModelManager = GetService<IAIModelManager>();
                var errorHandler = GetService<IErrorHandler>();
                var aiderService = GetService<IAiderService>();
                return new QuickFixProvider(serviceProvider, aiModelManager, errorHandler, aiderService);
            });

            // ThemingService depends on VSThemingService and ConfigurationService
            RegisterService<IThemingService>(() =>
            {
                var vsThemingService = GetService<IVSThemingService>();
                var configurationService = GetService<IConfigurationService>();
                return new ThemingService(vsThemingService, configurationService);
            });

            // Additional services that require complex initialization will be registered
            // manually in the package initialization with proper dependency injection
        }

        /// <summary>
        /// Registers the FileContextService with required dependencies
        /// </summary>
        /// <param name="dte">The DTE2 instance</param>
        /// <param name="outputPane">The output window pane</param>
        public void RegisterFileContextService(EnvDTE80.DTE2 dte, Microsoft.VisualStudio.Shell.Interop.IVsOutputWindowPane outputPane)
        {
            var service = new FileContextService(dte, outputPane);
            RegisterService<IFileContextService>(service);
        }

        /// <summary>
        /// Registers the AiderService with required dependencies
        /// </summary>
        public void RegisterAiderService()
        {
            RegisterService<IAiderService>(() =>
            {
                var agentApiService = GetService<IAgentApiService>();
                var dependencyChecker = GetService<IAiderDependencyChecker>();
                var errorHandler = GetService<IErrorHandler>();
                var telemetryService = GetService<ITelemetryService>();
                var correlationService = GetService<ICorrelationService>();
                return new AiderService(agentApiService, dependencyChecker, errorHandler, telemetryService, correlationService);
            });
        }

        /// <summary>
        /// Registers the DiffVisualizationService with required dependencies
        /// </summary>
        /// <param name="errorHandler">The error handler service</param>
        public void RegisterDiffVisualizationService(IErrorHandler errorHandler)
        {
            var service = new DiffVisualizationService(errorHandler);
            RegisterService<IDiffVisualizationService>(service);
        }

        /// <summary>
        /// Registers the SessionManager with required dependencies
        /// </summary>
        public void RegisterSessionManager()
        {
            var applicationStateService = GetService<IApplicationStateService>();
            var conversationPersistenceService = GetService<IConversationPersistenceService>();
            
            if (applicationStateService != null && conversationPersistenceService != null)
            {
                var sessionManager = new SessionManager(applicationStateService, conversationPersistenceService);
                RegisterService<SessionManager>(sessionManager);
            }
        }

        /// <summary>
        /// Registers the OutputWindowService with required Visual Studio dependencies
        /// </summary>
        /// <param name="serviceProvider">The Visual Studio service provider</param>
        public void RegisterOutputWindowService(IServiceProvider serviceProvider)
        {
            RegisterService<IOutputWindowService>(() =>
            {
                var errorHandler = GetService<IErrorHandler>();
                var quickFixProvider = GetService<IQuickFixProvider>();
                return new OutputWindowService(serviceProvider, errorHandler, quickFixProvider);
            });
        }

        /// <summary>
        /// Disposes all services and clears the container
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    foreach (var service in _services.Values)
                    {
                        if (service is IDisposable disposable)
                        {
                            try
                            {
                                disposable.Dispose();
                            }
                            catch (Exception)
                            {
                                // Log error but continue disposing other services
                            }
                        }
                    }

                    _services.Clear();
                    _serviceFactories.Clear();
                }

                _disposed = true;
            }
        }
    }
}