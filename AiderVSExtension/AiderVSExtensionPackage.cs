using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using AiderVSExtension.Services;
using AiderVSExtension.Options;
using AiderVSExtension.UI.Chat;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using Task = System.Threading.Tasks.Task;

namespace AiderVSExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AiderVSExtensionPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(ConfigurationPageOptions), "Aider VS Extension", "General", 0, 0, true)]
    [ProvideToolWindow(typeof(ChatToolWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class AiderVSExtensionPackage : AsyncPackage
    {
        /// <summary>
        /// AiderVSExtensionPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a7c02a2b-8b4e-4f5d-9b3c-1e2f3a4b5c6d";

        // Command IDs from the menu file
        public const int OpenChatToolWindowCommandId = 0x0100;
        public const int AddToAiderChatCommandId = 0x0101;
        public const int FixWithAiderCommandId = 0x0102;
        public const int OpenSettingsCommandId = 0x0106;
        public const int ToggleAICompletionCommandId = 0x0104;
        public const int TestAIConnectionCommandId = 0x0111;

        // Command set GUID from the menu file
        public static readonly Guid CommandSet = new Guid("{12345678-1234-1234-1234-123456789ABE}");

        private Services.ServiceContainer _serviceContainer;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Report progress
            progress?.Report(new ServiceProgressData("Initializing Aider VS Extension services...", "Starting service container", 0, 4));

            // Initialize the service container on background thread
            _serviceContainer = new ServiceContainer();
            await _serviceContainer.InitializeAsync(this, cancellationToken);
            
            progress?.Report(new ServiceProgressData("Initializing Aider VS Extension services...", "Service container initialized", 1, 4));

            // Initialize session management on background thread
            await InitializeSessionManagementAsync(progress, cancellationToken);
            
            progress?.Report(new ServiceProgressData("Initializing Aider VS Extension services...", "Session management initialized", 2, 4));

            // Initialize Aider services and dependencies on background thread
            await InitializeAiderServicesAsync(progress, cancellationToken);
            
            progress?.Report(new ServiceProgressData("Initializing Aider VS Extension services...", "Aider services initialized", 3, 5));

            // Switch to UI thread only for VS service registration
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            
            // Register services with Visual Studio's service container (UI thread required)
            RegisterServices();
            
            // Initialize command handlers (UI thread required)
            await InitializeCommandsAsync();
            
            progress?.Report(new ServiceProgressData("Initializing Aider VS Extension services...", "Commands initialized", 4, 5));
            
            // Final progress report
            progress?.Report(new ServiceProgressData("Aider VS Extension initialized", "Extension ready", 5, 5));
        }

        /// <summary>
        /// Registers services with Visual Studio's service container
        /// </summary>
        private void RegisterServices()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Register our service container as a service
            var serviceContainer = GetService(typeof(IServiceContainer)) as IServiceContainer;
            serviceContainer?.AddService(typeof(ServiceContainer), _serviceContainer, true);
        }

        /// <summary>
        /// Initializes session management services
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        private async Task InitializeSessionManagementAsync(IProgress<ServiceProgressData> progress, CancellationToken cancellationToken)
        {
            try
            {
                // Register session manager with dependencies
                _serviceContainer.RegisterSessionManager();

                // Get the session manager and initialize it
                var sessionManager = _serviceContainer.GetService<SessionManager>();
                if (sessionManager != null)
                {
                    // The session manager will automatically start when the application state service
                    // fires the ExtensionInitialized event, so we don't need to manually start it here
                    
                    // Initialize the application state service
                    var applicationStateService = _serviceContainer.GetService<Interfaces.IApplicationStateService>();
                    if (applicationStateService != null)
                    {
                        await applicationStateService.InitializeAsync();
                    }
                    
                    progress?.Report(new ServiceProgressData("Initializing Aider VS Extension services...", "Session management configured", 2, 4));
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - extension should still work without session management
                System.Diagnostics.Debug.WriteLine($"Failed to initialize session management: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes Aider services and validates dependencies
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        private async Task InitializeAiderServicesAsync(IProgress<ServiceProgressData> progress, CancellationToken cancellationToken)
        {
            try
            {
                // Register Aider service with dependencies
                _serviceContainer.RegisterAiderService();

                // Get the setup manager and perform initial dependency check
                var setupManager = _serviceContainer.GetService<Interfaces.IAiderSetupManager>();
                if (setupManager != null)
                {
                    // Check dependencies in background - don't block initialization
                    var dependenciesSatisfied = await setupManager.AreDependenciesSatisfiedAsync();
                    
                    if (!dependenciesSatisfied)
                    {
                        System.Diagnostics.Debug.WriteLine("Aider dependencies not satisfied - setup will be required before first use");
                    }
                    else
                    {
                        // Dependencies satisfied - validate AgentAPI in background
                        _ = ValidateAgentApiAsync(setupManager);
                    }
                }
                
                progress?.Report(new ServiceProgressData("Initializing Aider VS Extension services...", "Aider services configured", 3, 5));
            }
            catch (Exception ex)
            {
                // Log error but don't throw - extension should still work with degraded functionality
                System.Diagnostics.Debug.WriteLine($"Failed to initialize Aider services: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates AgentAPI in the background
        /// </summary>
        /// <param name="setupManager">The setup manager to use for validation</param>
        /// <returns>Task representing the async operation</returns>
        private async Task ValidateAgentApiAsync(Interfaces.IAiderSetupManager setupManager)
        {
            try
            {
                await setupManager.ValidateAgentApiAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AgentAPI validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the service container for dependency injection
        /// </summary>
        /// <returns>The service container instance</returns>
        public Services.ServiceContainer GetServiceContainer()
        {
            return _serviceContainer;
        }

        /// <summary>
        /// Gets the chat tool window instance
        /// </summary>
        /// <returns>The chat tool window instance</returns>
        public ChatToolWindow GetChatToolWindow()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return FindToolWindow(typeof(ChatToolWindow), 0, true) as ChatToolWindow;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting chat tool window: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Shows the chat tool window
        /// </summary>
        public void ShowChatToolWindow()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var window = GetChatToolWindow();
                if (window?.Frame is IVsWindowFrame windowFrame)
                {
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing chat tool window: {ex}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Shutdown session management gracefully
                    var sessionManager = _serviceContainer?.GetService<SessionManager>();
                    sessionManager?.Dispose();

                    // Shutdown application state service
                    var applicationStateService = _serviceContainer?.GetService<Interfaces.IApplicationStateService>();
                    if (applicationStateService != null)
                    {
                        // Fire shutdown in background to avoid blocking disposal
                        _ = ShutdownApplicationStateAsync(applicationStateService);
                    }

                    _serviceContainer?.Dispose();
                }
                catch (Exception ex)
                {
                    // Log error but don't throw during disposal
                    System.Diagnostics.Debug.WriteLine($"Error during extension disposal: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Shuts down the application state service in the background
        /// </summary>
        /// <param name="applicationStateService">The application state service to shutdown</param>
        /// <returns>Task representing the async operation</returns>
        private async Task ShutdownApplicationStateAsync(Interfaces.IApplicationStateService applicationStateService)
        {
            try
            {
                await applicationStateService.ShutdownAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors during shutdown
            }
        }

        /// <summary>
        /// Initializes command handlers
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        private async Task InitializeCommandsAsync()
        {
            await Task.Run(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                
                var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    // Register command handlers
                    RegisterCommand(commandService, OpenChatToolWindowCommandId, ExecuteOpenChatToolWindowCommand, null);
                    RegisterCommand(commandService, AddToAiderChatCommandId, ExecuteAddToAiderChatCommand, null);
                    RegisterCommand(commandService, FixWithAiderCommandId, ExecuteFixWithAiderCommand, null);
                    RegisterCommand(commandService, OpenSettingsCommandId, ExecuteOpenSettingsCommand, null);
                    RegisterCommand(commandService, ToggleAICompletionCommandId, ExecuteToggleAICompletionCommand, null);
                    RegisterCommand(commandService, TestAIConnectionCommandId, ExecuteTestAIConnectionCommand, null);
                }
            });
        }

        /// <summary>
        /// Registers a command handler
        /// </summary>
        /// <param name="commandService">The command service</param>
        /// <param name="commandId">The command ID</param>
        /// <param name="executeHandler">The execute handler</param>
        /// <param name="queryStatusHandler">The query status handler</param>
        private void RegisterCommand(OleMenuCommandService commandService, int commandId, EventHandler executeHandler, EventHandler<OleMenuCommand> queryStatusHandler)
        {
            var menuCommandID = new CommandID(CommandSet, commandId);
            var menuCommand = new OleMenuCommand(executeHandler, menuCommandID);
            
            if (queryStatusHandler != null)
            {
                menuCommand.BeforeQueryStatus += queryStatusHandler;
            }
            
            commandService.AddCommand(menuCommand);
        }

        #region Command Handlers

        private void ExecuteOpenChatToolWindowCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                ShowChatToolWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening chat tool window: {ex}");
            }
        }

        private void ExecuteAddToAiderChatCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // Get selected text from editor and add to chat
                var selection = GetCurrentEditorSelection();
                if (selection != null)
                {
                    var chatWindow = GetChatToolWindow();
                    if (chatWindow != null)
                    {
                        ShowChatToolWindow();
                        // Add selected text to chat context
                        chatWindow.AddFileReferenceToChat(selection);
                    }
                }
                else
                {
                    // No selection - show chat anyway
                    ShowChatToolWindow();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding to Aider chat: {ex}");
            }
        }

        private void ExecuteFixWithAiderCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // Get error context and start Aider fix
                var errorContext = GetCurrentErrorContext();
                var chatWindow = GetChatToolWindow();
                
                if (chatWindow != null)
                {
                    ShowChatToolWindow();
                    
                    if (errorContext != null)
                    {
                        // Add error context to chat
                        chatWindow.AddFileReferenceToChat(errorContext);
                        
                        // Pre-populate chat with fix request
                        var fixMessage = $"Please help fix this error:\n\n{errorContext.Content}\n\nFile: {errorContext.FilePath}";
                        chatWindow.ChatControl.SetChatInput(fixMessage);
                    }
                    else
                    {
                        // No specific error context - general help request
                        var selection = GetCurrentEditorSelection();
                        if (selection != null)
                        {
                            chatWindow.AddFileReferenceToChat(selection);
                            chatWindow.ChatControl.SetChatInput("Please review this code and help fix any issues:");
                        }
                        else
                        {
                            chatWindow.ChatControl.SetChatInput("Please help me fix the issues in my code.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fixing with Aider: {ex}");
            }
        }

        private void ExecuteOpenSettingsCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // Open the options page
                ShowOptionPage(typeof(ConfigurationPageOptions));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening settings: {ex}");
            }
        }

        private async void ExecuteToggleAICompletionCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // Toggle AI completion feature
                var configService = _serviceContainer?.GetService<IConfigurationService>();
                if (configService != null)
                {
                    var wasEnabled = configService.IsAICompletionEnabled;
                    await configService.ToggleAICompletionAsync();
                    var isEnabled = configService.IsAICompletionEnabled;
                    
                    System.Diagnostics.Debug.WriteLine($"AI Completion toggled from {wasEnabled} to {isEnabled}");
                    
                    // Show status message
                    var statusMessage = isEnabled ? "AI Completion Enabled" : "AI Completion Disabled";
                    var statusBar = GetService(typeof(IVsStatusbar)) as IVsStatusbar;
                    statusBar?.SetText(statusMessage);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Configuration service not available for toggle command");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling AI completion: {ex}");
            }
        }

        private void ExecuteTestAIConnectionCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // Test AI connection
                var setupManager = _serviceContainer?.GetService<IAiderSetupManager>();
                if (setupManager != null)
                {
                    // Run connection test in background
                    _ = TestAIConnectionAsync(setupManager);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error testing AI connection: {ex}");
            }
        }

        private async Task TestAIConnectionAsync(IAiderSetupManager setupManager)
        {
            try
            {
                // TODO: Implement actual connection test
                await setupManager.AreDependenciesSatisfiedAsync();
                System.Diagnostics.Debug.WriteLine("AI connection test completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI connection test failed: {ex}");
            }
        }

        #endregion

        #region Editor Integration

        /// <summary>
        /// Gets the current editor selection as a file reference
        /// </summary>
        /// <returns>File reference with selected text, or null if no selection</returns>
        private FileReference GetCurrentEditorSelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                // Get the DTE service
                var dte = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                if (dte?.ActiveDocument?.Selection is EnvDTE.TextSelection selection)
                {
                    var selectedText = selection.Text;
                    if (!string.IsNullOrWhiteSpace(selectedText))
                    {
                        return new FileReference
                        {
                            FilePath = dte.ActiveDocument.FullName,
                            StartLine = selection.TopPoint.Line,
                            EndLine = selection.BottomPoint.Line,
                            Content = selectedText,
                            Type = ReferenceType.Selection
                        };
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting editor selection: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Gets the current error context from the active document
        /// </summary>
        /// <returns>File reference with error context, or null if no error found</returns>
        private FileReference GetCurrentErrorContext()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                // Get the DTE service
                var dte = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                if (dte?.ActiveDocument == null) return null;
                
                var filePath = dte.ActiveDocument.FullName;
                var textSelection = dte.ActiveDocument.Selection as EnvDTE.TextSelection;
                int currentLine = textSelection?.CurrentLine ?? 1;
                
                // Try to get error list service to find errors for current file
                var errorList = GetService(typeof(SVsErrorList)) as IVsErrorList;
                if (errorList != null)
                {
                    // For now, create a simple error context based on current line
                    // In a full implementation, we'd enumerate actual errors from the error list
                    var errorMessage = $"Error at line {currentLine} in {System.IO.Path.GetFileName(filePath)}";
                    
                    return new FileReference
                    {
                        FilePath = filePath,
                        StartLine = currentLine,
                        EndLine = currentLine,
                        Content = errorMessage,
                        Type = ReferenceType.Error
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting error context: {ex}");
                return null;
            }
        }

        #endregion

        #endregion
    }
}