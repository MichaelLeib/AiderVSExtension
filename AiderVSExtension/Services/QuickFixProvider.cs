using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Constants;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Provides quick fixes for errors and warnings in Visual Studio
    /// </summary>
    public class QuickFixProvider : IQuickFixProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAIModelManager _aiModelManager;
        private readonly IErrorHandler _errorHandler;
        private readonly IAiderService _aiderService;

        /// <summary>
        /// Priority of this quick fix provider
        /// </summary>
        public int Priority => 100;

        /// <summary>
        /// Determines if the provider can provide quick fixes for the specified error type
        /// </summary>
        /// <param name="errorType">The type of error to check</param>
        /// <returns>True if quick fixes can be provided, false otherwise</returns>
        public bool CanProvideQuickFix(AiderVSExtension.Interfaces.ErrorType errorType)
        {
            // We can provide AI-powered fixes for most error types
            switch (errorType)
            {
                case AiderVSExtension.Interfaces.ErrorType.CompilationError:
                case AiderVSExtension.Interfaces.ErrorType.SyntaxError:
                case AiderVSExtension.Interfaces.ErrorType.TypeError:
                case AiderVSExtension.Interfaces.ErrorType.ReferenceError:
                case AiderVSExtension.Interfaces.ErrorType.BuildError:
                case AiderVSExtension.Interfaces.ErrorType.CodeAnalysisWarning:
                    return true;
                case AiderVSExtension.Interfaces.ErrorType.Warning: // Skip general warnings
                case AiderVSExtension.Interfaces.ErrorType.RuntimeError: // Can't fix runtime errors statically
                    return false;
                default:
                    return false;
            }
        }

        public QuickFixProvider(IServiceProvider serviceProvider, IAIModelManager aiModelManager, IErrorHandler errorHandler, IAiderService aiderService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _aiModelManager = aiModelManager ?? throw new ArgumentNullException(nameof(aiModelManager));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _aiderService = aiderService ?? throw new ArgumentNullException(nameof(aiderService));
        }

        /// <summary>
        /// Gets available quick fixes for the specified error context
        /// </summary>
        public async Task<IEnumerable<QuickFixAction>> GetQuickFixesAsync(AiderVSExtension.Interfaces.ErrorContext context)
        {
            if (context == null)
            {
                return Enumerable.Empty<QuickFixAction>();
            }

            var quickFixes = new List<QuickFixAction>();

            try
            {
                // Add "Fix with Aider" action for all error types
                if (CanProvideQuickFix(context.ErrorType))
                {
                    quickFixes.Add(new QuickFixAction
                    {
                        Title = "Fix with Aider AI",
                        Description = "Use Aider AI to analyze and fix this error",
                        ActionType = QuickFixActionType.FixWithAI,
                        Priority = 90,
                        Context = context,
                        WillModifyFiles = true,
                        FilesToModify = new List<string> { context.FilePath }
                    });
                }

                // Add "Add to Aider Chat" action
                quickFixes.Add(new QuickFixAction
                {
                    Title = "Add to Aider Chat",
                    Description = "Add this error to Aider chat for discussion",
                    ActionType = QuickFixActionType.AddToChat,
                    Priority = 80,
                    Context = context,
                    WillModifyFiles = false
                });

                // Add specific quick fixes based on error type
                switch (context.ErrorType)
                {
                    case AiderVSExtension.Interfaces.ErrorType.CompilationError:
                        quickFixes.AddRange(await GetCompilationErrorFixesAsync(context));
                        break;
                    case AiderVSExtension.Interfaces.ErrorType.SyntaxError:
                        quickFixes.AddRange(GetSyntaxErrorFixes(context));
                        break;
                    case AiderVSExtension.Interfaces.ErrorType.TypeError:
                        quickFixes.AddRange(GetTypeErrorFixes(context));
                        break;
                    case AiderVSExtension.Interfaces.ErrorType.ReferenceError:
                        quickFixes.AddRange(GetReferenceErrorFixes(context));
                        break;
                    case AiderVSExtension.Interfaces.ErrorType.BuildError:
                        quickFixes.AddRange(GetBuildErrorFixes(context));
                        break;
                }

                return quickFixes.OrderByDescending(q => q.Priority);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error getting quick fixes");
                return Enumerable.Empty<QuickFixAction>();
            }
        }


        /// <summary>
        /// Executes the specified quick fix action
        /// </summary>
        public async Task<bool> ExecuteQuickFixAsync(QuickFixAction action)
        {
            if (action == null)
            {
                return false;
            }

            try
            {
                switch (action.ActionType)
                {
                    case QuickFixActionType.FixWithAI:
                        return await ExecuteAIFixAsync(action);
                    case QuickFixActionType.AddToChat:
                        return await ExecuteAddToChatAsync(action);
                    case QuickFixActionType.AutomaticFix:
                        return await ExecuteAutomaticFixAsync(action);
                    case QuickFixActionType.Suggestion:
                        return await ExecuteSuggestionAsync(action);
                    case QuickFixActionType.AddUsing:
                        return await ExecuteAddUsingAsync(action);
                    case QuickFixActionType.CreateMember:
                        return await ExecuteCreateMemberAsync(action);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, $"Error executing quick fix: {action.Title}");
                return false;
            }
        }

        #region Private Methods - Error Type Specific Fixes

        private async Task<IEnumerable<QuickFixAction>> GetCompilationErrorFixesAsync(AiderVSExtension.Interfaces.ErrorContext context)
        {
            var fixes = new List<QuickFixAction>();

            // Check for common compilation errors
            if (context.Message.Contains("does not exist") || context.Message.Contains("not found"))
            {
                fixes.Add(new QuickFixAction
                {
                    Title = "Add Missing Using Statement",
                    Description = "Add the missing using statement or namespace reference",
                    ActionType = QuickFixActionType.AddUsing,
                    Priority = 70,
                    Context = context,
                    WillModifyFiles = true,
                    FilesToModify = new List<string> { context.FilePath }
                });
            }

            if (context.Message.Contains("does not contain a definition"))
            {
                fixes.Add(new QuickFixAction
                {
                    Title = "Create Missing Member",
                    Description = "Create the missing method, property, or field",
                    ActionType = QuickFixActionType.CreateMember,
                    Priority = 60,
                    Context = context,
                    WillModifyFiles = true,
                    FilesToModify = new List<string> { context.FilePath }
                });
            }

            return fixes;
        }

        private IEnumerable<QuickFixAction> GetSyntaxErrorFixes(AiderVSExtension.Interfaces.ErrorContext context)
        {
            var fixes = new List<QuickFixAction>();

            fixes.Add(new QuickFixAction
            {
                Title = "Auto-fix Syntax Error",
                Description = "Automatically fix common syntax errors",
                ActionType = QuickFixActionType.AutomaticFix,
                Priority = 75,
                Context = context,
                WillModifyFiles = true,
                FilesToModify = new List<string> { context.FilePath }
            });

            return fixes;
        }

        private IEnumerable<QuickFixAction> GetTypeErrorFixes(AiderVSExtension.Interfaces.ErrorContext context)
        {
            var fixes = new List<QuickFixAction>();

            fixes.Add(new QuickFixAction
            {
                Title = "Fix Type Mismatch",
                Description = "Suggest fixes for type conversion or casting",
                ActionType = QuickFixActionType.Suggestion,
                Priority = 65,
                Context = context,
                WillModifyFiles = true,
                FilesToModify = new List<string> { context.FilePath }
            });

            return fixes;
        }

        private IEnumerable<QuickFixAction> GetReferenceErrorFixes(AiderVSExtension.Interfaces.ErrorContext context)
        {
            var fixes = new List<QuickFixAction>();

            fixes.Add(new QuickFixAction
            {
                Title = "Add Missing Reference",
                Description = "Add the missing assembly or package reference",
                ActionType = QuickFixActionType.AddUsing,
                Priority = 70,
                Context = context,
                WillModifyFiles = true,
                FilesToModify = new List<string> { context.ProjectName + ".csproj" }
            });

            return fixes;
        }

        private IEnumerable<QuickFixAction> GetBuildErrorFixes(AiderVSExtension.Interfaces.ErrorContext context)
        {
            var fixes = new List<QuickFixAction>();

            fixes.Add(new QuickFixAction
            {
                Title = "Analyze Build Error",
                Description = "Get detailed analysis and suggestions for build errors",
                ActionType = QuickFixActionType.Suggestion,
                Priority = 60,
                Context = context,
                WillModifyFiles = false
            });

            return fixes;
        }

        #endregion

        #region Private Methods - Fix Execution

        private async Task<bool> ExecuteAIFixAsync(QuickFixAction action)
        {
            try
            {
                // Build context for AI
                var errorContext = BuildErrorContextForAI(action.Context);
                
                // Get AI suggestion
                var aiResponse = await _aiModelManager.GenerateCompletionAsync(errorContext);
                
                if (string.IsNullOrEmpty(aiResponse))
                {
                    return false;
                }

                // Show AI suggestion to user
                return await ShowAISuggestionAsync(action.Context, aiResponse);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error executing AI fix");
                return false;
            }
        }

        private async Task<bool> ExecuteAddToChatAsync(QuickFixAction action)
        {
            try
            {
                // Get the chat service and add error to chat
                var chatMessage = new ChatMessage
                {
                    Content = $"I'm getting this error: {action.Context.Message}\n\nFile: {action.Context.FilePath}\nLine: {action.Context.LineNumber}\nError Code: {action.Context.ErrorCode}",
                    Type = MessageType.User,
                    Timestamp = DateTime.UtcNow
                };

                // Add file reference if available
                if (!string.IsNullOrEmpty(action.Context.FilePath))
                {
                    chatMessage.References = new List<FileReference>
                    {
                        new FileReference
                        {
                            FilePath = action.Context.FilePath,
                            StartLine = action.Context.LineNumber,
                            EndLine = action.Context.LineNumber,
                            Type = ReferenceType.File
                        }
                    };
                }

                // Add to Aider chat service
                await _aiderService.SendMessageAsync(chatMessage);

                return true;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error adding to chat");
                return false;
            }
        }

        private async Task<bool> ExecuteAutomaticFixAsync(QuickFixAction action)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                var dte = (DTE)_serviceProvider.GetService(typeof(DTE));
                var document = dte.ActiveDocument;
                
                if (document?.Selection is EnvDTE.TextSelection textSelection)
                {
                    var context = action.Context;
                    
                    // Navigate to the error line
                    textSelection.GotoLine(context.LineNumber);
                    
                    // For now, just show a message about the fix
                    // In a full implementation, you would apply the actual fix
                    await _errorHandler.ShowErrorNotificationAsync(
                        $"Auto-fix would be applied to line {context.LineNumber}: {context.Message}", 
                        "Automatic Fix");
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error executing automatic fix");
                return false;
            }
        }

        private async Task<bool> ExecuteSuggestionAsync(QuickFixAction action)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                // Build context for AI suggestion
                var prompt = BuildErrorContextForAI(action.Context);
                var aiResponse = await _aiModelManager.GenerateCompletionAsync(prompt);
                
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    // Show the suggestion in a message box
                    VsShellUtilities.ShowMessageBox(
                        _serviceProvider,
                        aiResponse,
                        "AI Suggestion for Error Fix",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error showing suggestion");
                return false;
            }
        }

        private async Task<bool> ExecuteAddUsingAsync(QuickFixAction action)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                var dte = (DTE)_serviceProvider.GetService(typeof(DTE));
                var document = dte.ActiveDocument;
                
                if (document?.Selection is EnvDTE.TextSelection textSelection)
                {
                    var context = action.Context;
                    
                    // Extract the missing type from error message
                    var missingType = ExtractMissingTypeFromError(context.Message);
                    
                    if (!string.IsNullOrEmpty(missingType))
                    {
                        // Get common namespace mappings
                        var namespaceToAdd = GetNamespaceForType(missingType);
                        
                        if (!string.IsNullOrEmpty(namespaceToAdd))
                        {
                            // Find the insertion point (after existing using statements)
                            var insertPoint = FindUsingInsertionPoint(textSelection);
                            
                            insertPoint.Insert($"using {namespaceToAdd};\n");
                            document.Save();
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error adding using statement");
                return false;
            }
        }

        private async Task<bool> ExecuteCreateMemberAsync(QuickFixAction action)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                var dte = (DTE)_serviceProvider.GetService(typeof(DTE));
                var document = dte.ActiveDocument;
                
                if (document?.Selection is EnvDTE.TextSelection textSelection)
                {
                    var context = action.Context;
                    
                    // Extract member name from error message
                    var memberName = ExtractMissingMemberFromError(context.Message);
                    
                    if (!string.IsNullOrEmpty(memberName))
                    {
                        // Generate stub member based on usage context
                        var memberStub = GenerateMemberStub(memberName, context);
                        
                        if (!string.IsNullOrEmpty(memberStub))
                        {
                            // Find insertion point in class
                            var insertPoint = FindMemberInsertionPoint(textSelection, context);
                            
                            if (insertPoint != null)
                            {
                                insertPoint.Insert(memberStub);
                                document.Save();
                                return true;
                            }
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error creating missing member");
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        private string BuildErrorContextForAI(AiderVSExtension.Interfaces.ErrorContext context)
        {
            var prompt = $@"I have a {context.ErrorType} error in my C# code:

Error Message: {context.Message}
Error Code: {context.ErrorCode}
File: {context.FilePath}
Line: {context.LineNumber}
Column: {context.ColumnNumber}

Error Context: {context.ErrorSpan}

Please analyze this error and provide a specific fix with code examples. Focus on:
1. What caused the error
2. How to fix it
3. Code example of the fix
4. Best practices to avoid this error in the future

Please provide a clear, actionable solution.";

            return prompt;
        }

        private async Task<bool> ShowAISuggestionAsync(AiderVSExtension.Interfaces.ErrorContext context, string aiResponse)
        {
            try
            {
                // Use Visual Studio's message box to show AI suggestion
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                VsShellUtilities.ShowMessageBox(
                    _serviceProvider,
                    aiResponse,
                    "Aider AI Suggestion",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                return true;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error showing AI suggestion");
                return false;
            }
        }

        private string ExtractMissingTypeFromError(string errorMessage)
        {
            // Extract type name from common error patterns
            var patterns = new[]
            {
                @"The type or namespace name '(\w+)' could not be found",
                @"'(\w+)' does not exist in the current context",
                @"The name '(\w+)' does not exist"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(errorMessage, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        private string GetNamespaceForType(string typeName)
        {
            // Common type to namespace mappings
            var commonMappings = new Dictionary<string, string>
            {
                ["List"] = "System.Collections.Generic",
                ["Dictionary"] = "System.Collections.Generic",
                ["Task"] = "System.Threading.Tasks",
                ["CancellationToken"] = "System.Threading",
                ["HttpClient"] = "System.Net.Http",
                ["JsonSerializer"] = "System.Text.Json",
                ["StringBuilder"] = "System.Text",
                ["Regex"] = "System.Text.RegularExpressions",
                ["File"] = "System.IO",
                ["Path"] = "System.IO",
                ["Directory"] = "System.IO",
                ["XmlDocument"] = "System.Xml",
                ["Debug"] = "System.Diagnostics"
            };

            return commonMappings.TryGetValue(typeName, out var ns) ? ns : null;
        }

        private EditPoint FindUsingInsertionPoint(EnvDTE.TextSelection textSelection)
        {
            // For now, return a simple insertion point at the beginning
            // In a full implementation, you would parse the document to find the correct location
            return textSelection.ActivePoint.CreateEditPoint();
        }

        private string ExtractMissingMemberFromError(string errorMessage)
        {
            var patterns = new[]
            {
                @"'(\w+)' does not contain a definition for '(\w+)'",
                @"'(\w+)' is not a member of '(\w+)'"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(errorMessage, pattern);
                if (match.Success && match.Groups.Count > 2)
                {
                    return match.Groups[2].Value; // Return the member name
                }
            }

            return null;
        }

        private string GenerateMemberStub(string memberName, AiderVSExtension.Interfaces.ErrorContext context)
        {
            // Analyze context to determine member type
            var errorSpan = context.ErrorSpan?.ToLowerInvariant() ?? "";
            
            if (errorSpan.Contains("(") && errorSpan.Contains(")"))
            {
                // Looks like a method call
                return $"\n        public void {memberName}()\n        {{\n            throw new NotImplementedException();\n        }}\n";
            }
            else if (errorSpan.Contains("="))
            {
                // Looks like a property assignment
                return $"\n        public object {memberName} {{ get; set; }}\n";
            }
            else
            {
                // Default to property
                return $"\n        public object {memberName} {{ get; set; }}\n";
            }
        }

        private EditPoint FindMemberInsertionPoint(EnvDTE.TextSelection textSelection, AiderVSExtension.Interfaces.ErrorContext context)
        {
            // For now, return a simple insertion point at the current position
            // In a full implementation, you would parse the document to find the correct class location
            return textSelection.ActivePoint.CreateEditPoint();
        }

        #endregion
    }
}

