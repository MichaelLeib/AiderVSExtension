using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using System.IO;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for context-aware UI updates and adaptive interface behavior
    /// </summary>
    public class ContextAwareUIService : IContextAwareUIService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IFileContextService _fileContextService;
        private readonly INotificationService _notificationService;
        private readonly IGitService _gitService;
        private readonly Dictionary<string, AiderVSExtension.Models.UIContext> _contexts = new Dictionary<string, AiderVSExtension.Models.UIContext>();
        private readonly Dictionary<FrameworkElement, ContextAwareElement> _contextElements = new Dictionary<FrameworkElement, ContextAwareElement>();
        private AiderVSExtension.Models.UIContext _currentContext;
        private bool _disposed = false;

        public event EventHandler<ContextChangedEventArgs> ContextChanged;
        public event EventHandler<UIUpdateEventArgs> UIUpdated;

        public ContextAwareUIService(
            IErrorHandler errorHandler,
            IFileContextService fileContextService,
            INotificationService notificationService,
            IGitService gitService)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _fileContextService = fileContextService ?? throw new ArgumentNullException(nameof(fileContextService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));

            InitializeContexts();
            _currentContext = _contexts["default"];
        }

        /// <summary>
        /// Registers a UI element to be context-aware
        /// </summary>
        /// <param name="element">Element to register</param>
        /// <param name="configuration">Context configuration</param>
        public void RegisterContextAwareElement(FrameworkElement element, ContextAwareConfiguration configuration)
        {
            try
            {
                if (element == null || configuration == null)
                    return;

                var contextElement = new ContextAwareElement
                {
                    Element = element,
                    Configuration = configuration,
                    LastUpdateTime = DateTime.UtcNow
                };

                _contextElements[element] = contextElement;
                
                // Apply initial context
                ApplyContextToElement(element, _currentContext);
                
                // Subscribe to element events
                element.Loaded += OnElementLoaded;
                element.Unloaded += OnElementUnloaded;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.RegisterContextAwareElement");
            }
        }

        /// <summary>
        /// Unregisters a UI element from context-aware updates
        /// </summary>
        /// <param name="element">Element to unregister</param>
        public void UnregisterContextAwareElement(FrameworkElement element)
        {
            try
            {
                if (element == null || !_contextElements.ContainsKey(element))
                    return;

                element.Loaded -= OnElementLoaded;
                element.Unloaded -= OnElementUnloaded;
                
                _contextElements.Remove(element);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.UnregisterContextAwareElement");
            }
        }

        /// <summary>
        /// Updates the current UI context
        /// </summary>
        /// <param name="contextInfo">Context information</param>
        public async Task UpdateContextAsync(ContextInfo contextInfo)
        {
            try
            {
                if (contextInfo == null)
                    return;

                var previousContext = _currentContext;
                _currentContext = await BuildContextAsync(contextInfo);
                
                // Update all registered elements
                await UpdateAllElementsAsync();
                
                // Fire context changed event
                ContextChanged?.Invoke(this, new ContextChangedEventArgs
                {
                    PreviousContext = previousContext,
                    NewContext = _currentContext,
                    ContextInfo = contextInfo,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ContextAwareUIService.UpdateContextAsync");
            }
        }

        /// <summary>
        /// Gets the current UI context
        /// </summary>
        /// <returns>Current context</returns>
        public AiderVSExtension.Models.UIContext GetCurrentContext()
        {
            return _currentContext;
        }

        /// <summary>
        /// Updates a specific element based on current context
        /// </summary>
        /// <param name="element">Element to update</param>
        public async Task UpdateElementAsync(FrameworkElement element)
        {
            try
            {
                if (element == null || !_contextElements.ContainsKey(element))
                    return;

                await ApplyContextToElementAsync(element, _currentContext);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ContextAwareUIService.UpdateElementAsync");
            }
        }

        /// <summary>
        /// Forces an update of all context-aware elements
        /// </summary>
        public async Task UpdateAllElementsAsync()
        {
            try
            {
                var tasks = _contextElements.Keys.Select(element => UpdateElementAsync(element));
                await Task.WhenAll(tasks);
                
                UIUpdated?.Invoke(this, new UIUpdateEventArgs
                {
                    Context = _currentContext,
                    UpdatedElements = _contextElements.Keys.ToList(),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ContextAwareUIService.UpdateAllElementsAsync");
            }
        }

        /// <summary>
        /// Adds a custom context provider
        /// </summary>
        /// <param name="name">Provider name</param>
        /// <param name="provider">Context provider</param>
        public void AddContextProvider(string name, IContextProvider provider)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || provider == null)
                    return;

                // Implementation would store and use custom context providers
                // For now, we'll just track the registration
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.AddContextProvider");
            }
        }

        /// <summary>
        /// Creates a context snapshot for debugging
        /// </summary>
        /// <returns>Context snapshot</returns>
        public ContextSnapshot CreateSnapshot()
        {
            try
            {
                return new ContextSnapshot
                {
                    Context = _currentContext,
                    RegisteredElements = _contextElements.Values.ToList(),
                    Timestamp = DateTime.UtcNow,
                    ContextProviders = new List<string>(), // Would list active providers
                    ActiveFeatures = GetActiveFeatures()
                };
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.CreateSnapshot");
                return new ContextSnapshot { Timestamp = DateTime.UtcNow };
            }
        }

        #region Private Methods

        private void InitializeContexts()
        {
            _contexts["default"] = new AiderVSExtension.Models.UIContext
            {
                Name = "Default",
                Type = ContextType.Default,
                Properties = new Dictionary<string, object>
                {
                    ["IsFileOpen"] = false,
                    ["HasSelection"] = false,
                    ["IsGitRepository"] = false,
                    ["Language"] = "text",
                    ["Theme"] = "default"
                }
            };
            
            _contexts["coding"] = new AiderVSExtension.Models.UIContext
            {
                Name = "Coding",
                Type = ContextType.Coding,
                Properties = new Dictionary<string, object>
                {
                    ["IsFileOpen"] = true,
                    ["HasSelection"] = false,
                    ["Language"] = "csharp",
                    ["ShowCodeActions"] = true,
                    ["ShowCompletion"] = true
                }
            };
            
            _contexts["debugging"] = new AiderVSExtension.Models.UIContext
            {
                Name = "Debugging",
                Type = ContextType.Debugging,
                Properties = new Dictionary<string, object>
                {
                    ["IsDebugging"] = true,
                    ["ShowBreakpoints"] = true,
                    ["ShowCallStack"] = true,
                    ["ShowWatch"] = true
                }
            };
            
            _contexts["git"] = new AiderVSExtension.Models.UIContext
            {
                Name = "Git",
                Type = ContextType.Git,
                Properties = new Dictionary<string, object>
                {
                    ["IsGitRepository"] = true,
                    ["HasChanges"] = false,
                    ["CurrentBranch"] = "main",
                    ["ShowGitActions"] = true
                }
            };
        }

        private async Task<AiderVSExtension.Models.UIContext> BuildContextAsync(ContextInfo contextInfo)
        {
            try
            {
                var context = new AiderVSExtension.Models.UIContext
                {
                    Name = DetermineContextName(contextInfo),
                    Type = DetermineContextType(contextInfo),
                    Properties = new Dictionary<string, object>(),
                    LastUpdated = DateTime.UtcNow
                };

                // Build context properties
                await BuildFileContextPropertiesAsync(context, contextInfo);
                await BuildGitContextPropertiesAsync(context, contextInfo);
                await BuildEditorContextPropertiesAsync(context, contextInfo);
                await BuildProjectContextPropertiesAsync(context, contextInfo);
                
                return context;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ContextAwareUIService.BuildContextAsync");
                return _contexts["default"];
            }
        }

        private async Task BuildFileContextPropertiesAsync(AiderVSExtension.Models.UIContext context, ContextInfo contextInfo)
        {
            try
            {
                if (!string.IsNullOrEmpty(contextInfo.ActiveFile))
                {
                    context.Properties["IsFileOpen"] = true;
                    context.Properties["ActiveFile"] = contextInfo.ActiveFile;
                    context.Properties["Language"] = DetectLanguage(contextInfo.ActiveFile);
                    context.Properties["FileSize"] = await GetFileSizeAsync(contextInfo.ActiveFile);
                    context.Properties["IsReadOnly"] = await IsFileReadOnlyAsync(contextInfo.ActiveFile);
                }
                else
                {
                    context.Properties["IsFileOpen"] = false;
                }
                
                context.Properties["HasSelection"] = !string.IsNullOrEmpty(contextInfo.SelectedText);
                context.Properties["SelectionLength"] = contextInfo.SelectedText?.Length ?? 0;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ContextAwareUIService.BuildFileContextPropertiesAsync");
            }
        }

        private async Task BuildGitContextPropertiesAsync(AiderVSExtension.Models.UIContext context, ContextInfo contextInfo)
        {
            try
            {
                if (!string.IsNullOrEmpty(contextInfo.ActiveFile))
                {
                    var gitContext = await _gitService.GetFileContextAsync(contextInfo.ActiveFile);
                    if (gitContext != null)
                    {
                        context.Properties["IsGitRepository"] = true;
                        context.Properties["CurrentBranch"] = gitContext.CurrentBranch;
                        context.Properties["HasChanges"] = gitContext.HasChanges;
                        context.Properties["IsDirty"] = gitContext.HasUncommittedChanges;
                        context.Properties["LastCommit"] = gitContext.LastCommit?.ShortSha;
                    }
                    else
                    {
                        context.Properties["IsGitRepository"] = false;
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ContextAwareUIService.BuildGitContextPropertiesAsync");
            }
        }

        private async Task BuildEditorContextPropertiesAsync(AiderVSExtension.Models.UIContext context, ContextInfo contextInfo)
        {
            try
            {
                context.Properties["CaretPosition"] = contextInfo.CaretPosition;
                context.Properties["LineNumber"] = contextInfo.LineNumber;
                context.Properties["ColumnNumber"] = contextInfo.ColumnNumber;
                context.Properties["HasErrors"] = contextInfo.HasErrors;
                context.Properties["HasWarnings"] = contextInfo.HasWarnings;
                context.Properties["IsModified"] = contextInfo.IsModified;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ContextAwareUIService.BuildEditorContextPropertiesAsync");
            }
        }

        private async Task BuildProjectContextPropertiesAsync(AiderVSExtension.Models.UIContext context, ContextInfo contextInfo)
        {
            try
            {
                if (!string.IsNullOrEmpty(contextInfo.ProjectPath))
                {
                    context.Properties["HasProject"] = true;
                    context.Properties["ProjectPath"] = contextInfo.ProjectPath;
                    context.Properties["ProjectType"] = DetectProjectType(contextInfo.ProjectPath);
                    context.Properties["IsBuilding"] = contextInfo.IsBuilding;
                    context.Properties["IsDebugging"] = contextInfo.IsDebugging;
                }
                else
                {
                    context.Properties["HasProject"] = false;
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ContextAwareUIService.BuildProjectContextPropertiesAsync");
            }
        }

        private string DetermineContextName(ContextInfo contextInfo)
        {
            if (contextInfo.IsDebugging)
                return "Debugging";
            else if (!string.IsNullOrEmpty(contextInfo.ActiveFile))
                return "Coding";
            else if (!string.IsNullOrEmpty(contextInfo.ProjectPath))
                return "Project";
            else
                return "Default";
        }

        private ContextType DetermineContextType(ContextInfo contextInfo)
        {
            if (contextInfo.IsDebugging)
                return ContextType.Debugging;
            else if (!string.IsNullOrEmpty(contextInfo.ActiveFile))
                return ContextType.Coding;
            else if (!string.IsNullOrEmpty(contextInfo.ProjectPath))
                return ContextType.Project;
            else
                return ContextType.Default;
        }

        private string DetectLanguage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "text";

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".cs" => "csharp",
                ".vb" => "vb",
                ".fs" => "fsharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".java" => "java",
                ".cpp" or ".cxx" or ".cc" => "cpp",
                ".c" => "c",
                ".h" or ".hpp" => "cpp",
                ".html" or ".htm" => "html",
                ".css" => "css",
                ".xml" => "xml",
                ".json" => "json",
                ".yaml" or ".yml" => "yaml",
                ".sql" => "sql",
                ".md" => "markdown",
                _ => "text"
            };
        }

        private async Task<long> GetFileSizeAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var info = new FileInfo(filePath);
                    return info.Length;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<bool> IsFileReadOnlyAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var info = new FileInfo(filePath);
                    return info.IsReadOnly;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private string DetectProjectType(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
                return "unknown";

            var extension = Path.GetExtension(projectPath).ToLowerInvariant();
            return extension switch
            {
                ".csproj" => "csharp",
                ".vbproj" => "vb",
                ".fsproj" => "fsharp",
                ".vcxproj" => "cpp",
                ".sln" => "solution",
                _ => "unknown"
            };
        }

        private void ApplyContextToElement(FrameworkElement element, AiderVSExtension.Models.UIContext context)
        {
            try
            {
                if (!_contextElements.ContainsKey(element))
                    return;

                var contextElement = _contextElements[element];
                var config = contextElement.Configuration;
                
                // Apply visibility rules
                ApplyVisibilityRules(element, context, config);
                
                // Apply property updates
                ApplyPropertyUpdates(element, context, config);
                
                // Apply styling
                ApplyStyling(element, context, config);
                
                // Update last update time
                contextElement.LastUpdateTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.ApplyContextToElement");
            }
        }

        private async Task ApplyContextToElementAsync(FrameworkElement element, AiderVSExtension.Models.UIContext context)
        {
            await Task.Run(() => ApplyContextToElement(element, context));
        }

        private void ApplyVisibilityRules(FrameworkElement element, AiderVSExtension.Models.UIContext context, ContextAwareConfiguration config)
        {
            try
            {
                if (config.VisibilityRules.Any())
                {
                    var shouldBeVisible = config.VisibilityRules.All(rule => EvaluateRule(rule, context));
                    element.Visibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.ApplyVisibilityRules");
            }
        }

        private void ApplyPropertyUpdates(FrameworkElement element, AiderVSExtension.Models.UIContext context, ContextAwareConfiguration config)
        {
            try
            {
                foreach (var update in config.PropertyUpdates)
                {
                    if (EvaluateRule(update.Condition, context))
                    {
                        var value = EvaluatePropertyValue(update.Value, context);
                        SetElementProperty(element, update.PropertyName, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.ApplyPropertyUpdates");
            }
        }

        private void ApplyStyling(FrameworkElement element, AiderVSExtension.Models.UIContext context, ContextAwareConfiguration config)
        {
            try
            {
                foreach (var styling in config.Styling)
                {
                    if (EvaluateRule(styling.Condition, context))
                    {
                        if (styling.Style != null)
                        {
                            element.Style = styling.Style;
                        }
                        
                        if (styling.Resources != null)
                        {
                            foreach (var resource in styling.Resources)
                            {
                                element.Resources[resource.Key] = resource.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.ApplyStyling");
            }
        }

        private bool EvaluateRule(ContextRule rule, AiderVSExtension.Models.UIContext context)
        {
            try
            {
                if (rule == null)
                    return true;

                return rule.Conditions.All(condition => EvaluateCondition(condition, context));
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.EvaluateRule");
                return false;
            }
        }

        private bool EvaluateCondition(ContextCondition condition, AiderVSExtension.Models.UIContext context)
        {
            try
            {
                if (!context.Properties.ContainsKey(condition.PropertyName))
                    return false;

                var value = context.Properties[condition.PropertyName];
                
                return condition.Operator switch
                {
                    ConditionOperator.Equals => Equals(value, condition.Value),
                    ConditionOperator.NotEquals => !Equals(value, condition.Value),
                    ConditionOperator.GreaterThan => CompareValues(value, condition.Value) > 0,
                    ConditionOperator.LessThan => CompareValues(value, condition.Value) < 0,
                    ConditionOperator.Contains => value?.ToString().Contains(condition.Value?.ToString()) == true,
                    ConditionOperator.StartsWith => value?.ToString().StartsWith(condition.Value?.ToString()) == true,
                    ConditionOperator.EndsWith => value?.ToString().EndsWith(condition.Value?.ToString()) == true,
                    ConditionOperator.IsTrue => value is bool boolValue && boolValue,
                    ConditionOperator.IsFalse => value is bool boolValue2 && !boolValue2,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.EvaluateCondition");
                return false;
            }
        }

        private int CompareValues(object value1, object value2)
        {
            if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
            {
                return comparable1.CompareTo(comparable2);
            }
            return 0;
        }

        private object EvaluatePropertyValue(object value, AiderVSExtension.Models.UIContext context)
        {
            if (value is string stringValue && stringValue.StartsWith("$"))
            {
                var propertyName = stringValue.Substring(1);
                return context.Properties.GetValueOrDefault(propertyName);
            }
            return value;
        }

        private void SetElementProperty(FrameworkElement element, string propertyName, object value)
        {
            try
            {
                var property = element.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(element, value);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ContextAwareUIService.SetElementProperty");
            }
        }

        private List<string> GetActiveFeatures()
        {
            var features = new List<string>();
            
            if (_contextElements.Any())
                features.Add("Context-aware elements");
            
            if (_currentContext.Properties.GetValueOrDefault("IsGitRepository") is bool isGit && isGit)
                features.Add("Git integration");
            
            if (_currentContext.Properties.GetValueOrDefault("IsFileOpen") is bool isFile && isFile)
                features.Add("File context");
            
            return features;
        }

        private async void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                await UpdateElementAsync(element);
            }
        }

        private void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UnregisterContextAwareElement(element);
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var element in _contextElements.Keys.ToList())
                {
                    UnregisterContextAwareElement(element);
                }
                
                _contextElements.Clear();
                _contexts.Clear();
                _disposed = true;
            }
        }
    }
}