using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Shell.TableControl;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Constants;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for integrating with Visual Studio's Error List
    /// </summary>
    public class ErrorListIntegrationService : IErrorListIntegrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IQuickFixProvider _quickFixProvider;
        private readonly IErrorHandler _errorHandler;
        private readonly ErrorMessageParser _errorParser;
        private IVsErrorList _errorList;
        private IMenuCommandService _menuCommandService;

        public ErrorListIntegrationService(
            IServiceProvider serviceProvider,
            IQuickFixProvider quickFixProvider,
            IErrorHandler errorHandler)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _quickFixProvider = quickFixProvider ?? throw new ArgumentNullException(nameof(quickFixProvider));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _errorParser = new ErrorMessageParser();
        }

        /// <summary>
        /// Initializes the error list integration
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Get the error list service
                _errorList = await _serviceProvider.GetServiceAsync(typeof(SVsErrorList)) as IVsErrorList;
                
                // Get the menu command service
                _menuCommandService = await _serviceProvider.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;

                // Register context menu commands
                await RegisterContextMenuCommandsAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error initializing error list integration");
            }
        }

        /// <summary>
        /// Adds "Fix with Aider" context menu items to the error list
        /// </summary>
        public async Task AddAiderContextMenuAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_menuCommandService == null)
                {
                    return;
                }

                // Create "Fix with Aider" command
                var fixWithAiderCommandId = new CommandID(PackageGuids.CommandSet, PackageIds.FixWithAiderCommand);
                var fixWithAiderCommand = new OleMenuCommand(OnFixWithAiderCommand, fixWithAiderCommandId);
                fixWithAiderCommand.BeforeQueryStatus += OnFixWithAiderBeforeQueryStatus;
                _menuCommandService.AddCommand(fixWithAiderCommand);

                // Create "Add to Aider Chat" command
                var addToChatCommandId = new CommandID(PackageGuids.CommandSet, PackageIds.AddToAiderChatCommand);
                var addToChatCommand = new OleMenuCommand(OnAddToChatCommand, addToChatCommandId);
                addToChatCommand.BeforeQueryStatus += OnAddToChatBeforeQueryStatus;
                _menuCommandService.AddCommand(addToChatCommand);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error adding Aider context menu");
            }
        }

        /// <summary>
        /// Gets the currently selected error from the error list
        /// </summary>
        public async Task<AiderVSExtension.Models.ErrorContext> GetSelectedErrorAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_errorList == null)
                {
                    return null;
                }

                // Get the selected error from the error list
                // Note: This is a simplified implementation
                // In a real implementation, you would need to access the IVsErrorList interface
                // and extract the selected error information

                // For now, return a placeholder
                return new AiderVSExtension.Models.ErrorContext
                {
                    Message = "Sample error message",
                    ErrorCode = "CS0000",
                    FilePath = "C:\\Sample\\File.cs",
                    LineNumber = 10,
                    ColumnNumber = 5,
                    ErrorType = AiderVSExtension.Models.ErrorType.CompilationError,
                    ProjectName = "SampleProject"
                };
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error getting selected error");
                return null;
            }
        }

        /// <summary>
        /// Gets all errors from the error list
        /// </summary>
        public async Task<IEnumerable<AiderVSExtension.Models.ErrorContext>> GetAllErrorsAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var errors = new List<AiderVSExtension.Models.ErrorContext>();

                if (_errorList == null)
                {
                    return errors;
                }

                // Enumerate errors from Visual Studio's error list
                try
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    
                    // Get the error list service
                    var vsErrorList = _serviceProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsErrorList)) as Microsoft.VisualStudio.Shell.Interop.IVsErrorList;
                    if (vsErrorList != null)
                    {
                        // Get the task list
                        var vsTaskList = _serviceProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsTaskList)) as Microsoft.VisualStudio.Shell.Interop.IVsTaskList;
                        if (vsTaskList != null)
                        {
                            // Enumerate tasks
                            vsTaskList.EnumTaskItems(out var taskEnum);
                            if (taskEnum != null)
                            {
                                var taskItems = new Microsoft.VisualStudio.Shell.Interop.IVsTaskItem[1];
                                uint fetched = 0;
                                
                                while (taskEnum.Next(1, taskItems, out fetched) == Microsoft.VisualStudio.VSConstants.S_OK && fetched > 0)
                                {
                                    var taskItem = taskItems[0];
                                    if (taskItem != null)
                                    {
                                        // Extract error information
                                        var error = ExtractErrorFromTaskItem(taskItem);
                                        if (error != null)
                                        {
                                            errors.Add(error);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await _errorHandler.LogWarningAsync($"Error enumerating error list: {ex.Message}", "ErrorListIntegrationService.GetCurrentErrorsAsync");
                }

                return errors;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error getting all errors");
                return Enumerable.Empty<AiderVSExtension.Models.ErrorContext>();
            }
        }

        /// <summary>
        /// Navigates to the error location in the editor
        /// </summary>
        public async Task NavigateToErrorAsync(AiderVSExtension.Models.ErrorContext errorContext)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (errorContext == null || string.IsNullOrEmpty(errorContext.FilePath))
                {
                    return;
                }

                // Get the DTE service
                var dte = await _serviceProvider.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte == null)
                {
                    return;
                }

                // Open the file
                var window = dte.ItemOperations.OpenFile(errorContext.FilePath);
                if (window?.Document?.Selection is EnvDTE.TextSelection selection)
                {
                    // Navigate to the error location
                    selection.GotoLine(errorContext.LineNumber, false);
                    if (errorContext.ColumnNumber > 0)
                    {
                        selection.MoveToColumnIndex(errorContext.LineNumber, errorContext.ColumnNumber, false);
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error navigating to error location");
            }
        }

        /// <summary>
        /// Refreshes the error list to show updated errors
        /// </summary>
        public async Task RefreshErrorListAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_errorList != null)
                {
                    // Force refresh of the error list
                    // The exact implementation depends on the VS version and error list provider
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error refreshing error list");
            }
        }

        /// <summary>
        /// Processes errors and adds Aider integration
        /// </summary>
        public async Task ProcessErrorsAsync(IEnumerable<AiderVSExtension.Models.ErrorContext> errors)
        {
            try
            {
                foreach (var error in errors)
                {
                    // Enrich error context
                    await EnrichErrorContextAsync(error);

                    // Log error processing
                    await _errorHandler.LogAsync($"Processing error: {error.Message}");
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error processing errors");
            }
        }

        #region Private Methods

        private async Task RegisterContextMenuCommandsAsync()
        {
            try
            {
                await AddAiderContextMenuAsync();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error registering context menu commands");
            }
        }

        private async void OnFixWithAiderCommand(object sender, EventArgs e)
        {
            try
            {
                var selectedError = await GetSelectedErrorAsync();
                if (selectedError != null)
                {
                    var quickFixes = await _quickFixProvider.GetQuickFixesAsync(selectedError);
                    var fixWithAIAction = quickFixes.FirstOrDefault(q => q.ActionType == QuickFixActionType.FixWithAI);

                    if (fixWithAIAction != null)
                    {
                        await _quickFixProvider.ExecuteQuickFixAsync(fixWithAIAction);
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error executing Fix with Aider command");
            }
        }

        private async void OnAddToChatCommand(object sender, EventArgs e)
        {
            try
            {
                var selectedError = await GetSelectedErrorAsync();
                if (selectedError != null)
                {
                    var quickFixes = await _quickFixProvider.GetQuickFixesAsync(selectedError);
                    var addToChatAction = quickFixes.FirstOrDefault(q => q.ActionType == QuickFixActionType.AddToChat);

                    if (addToChatAction != null)
                    {
                        await _quickFixProvider.ExecuteQuickFixAsync(addToChatAction);
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error executing Add to Chat command");
            }
        }

        private async void OnFixWithAiderBeforeQueryStatus(object sender, EventArgs e)
        {
            try
            {
                if (sender is OleMenuCommand command)
                {
                    var selectedError = await GetSelectedErrorAsync();
                    command.Visible = selectedError != null && _quickFixProvider.CanProvideQuickFix(selectedError.ErrorType);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error checking Fix with Aider command status");
            }
        }

        private async void OnAddToChatBeforeQueryStatus(object sender, EventArgs e)
        {
            try
            {
                if (sender is OleMenuCommand command)
                {
                    var selectedError = await GetSelectedErrorAsync();
                    command.Visible = selectedError != null;
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error checking Add to Chat command status");
            }
        }

        private async Task EnrichErrorContextAsync(AiderVSExtension.Models.ErrorContext errorContext)
        {
            try
            {
                if (errorContext == null)
                {
                    return;
                }

                // Add timestamp
                errorContext.AdditionalContext["ProcessedAt"] = DateTime.UtcNow.ToString("O");

                // Add VS session info
                errorContext.AdditionalContext["VSSession"] = Environment.MachineName;

                // Categorize error if not already done
                if (errorContext.ErrorType == AiderVSExtension.Models.ErrorType.Unknown)
                {
                    errorContext.ErrorType = _errorParser.CategorizeError(errorContext.Message, errorContext.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error enriching error context");
            }
        }

        private AiderVSExtension.Models.ErrorContext ExtractErrorFromTaskItem(Microsoft.VisualStudio.Shell.Interop.IVsTaskItem taskItem)
        {
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                
                // Get task properties
                taskItem.get_Text(out var description);
                taskItem.get_Document(out var document);
                taskItem.get_Line(out var line);
                taskItem.get_Column(out var column);
                taskItem.get_Category(out var category);
                taskItem.get_Priority(out var priority);

                if (string.IsNullOrEmpty(description))
                    return null;

                // Determine error type based on category
                var errorType = AiderVSExtension.Models.ErrorType.CompilationError;
                switch (category)
                {
                    case Microsoft.VisualStudio.Shell.Interop.VSTASKCATEGORY.CAT_BUILDCOMPILE:
                        errorType = AiderVSExtension.Models.ErrorType.BuildError;
                        break;
                    case Microsoft.VisualStudio.Shell.Interop.VSTASKCATEGORY.CAT_CODESENSE:
                        errorType = AiderVSExtension.Models.ErrorType.SyntaxError;
                        break;
                    case Microsoft.VisualStudio.Shell.Interop.VSTASKCATEGORY.CAT_USER:
                        errorType = AiderVSExtension.Models.ErrorType.Warning;
                        break;
                }

                return new AiderVSExtension.Models.ErrorContext
                {
                    Message = description,
                    FilePath = document,
                    LineNumber = line + 1, // VS uses 0-based line numbers
                    ColumnNumber = column + 1, // VS uses 0-based column numbers
                    ErrorType = errorType,
                    Severity = priority == Microsoft.VisualStudio.Shell.Interop.VSTASKPRIORITY.TP_HIGH ? "Error" : "Warning",
                    ErrorCode = "", // Task items don't typically have error codes
                    ErrorSpan = description // Use description as span for now
                };
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error extracting task item: {ex.Message}", "ErrorListIntegrationService.ExtractErrorFromTaskItem");
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// Interface for error list integration service
    /// </summary>
    public interface IErrorListIntegrationService
    {
        Task InitializeAsync();
        Task AddAiderContextMenuAsync();
        Task<AiderVSExtension.Models.ErrorContext> GetSelectedErrorAsync();
        Task<IEnumerable<AiderVSExtension.Models.ErrorContext>> GetAllErrorsAsync();
        Task NavigateToErrorAsync(AiderVSExtension.Models.ErrorContext errorContext);
        Task RefreshErrorListAsync();
        Task ProcessErrorsAsync(IEnumerable<AiderVSExtension.Models.ErrorContext> errors);
    }
}