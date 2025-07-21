using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Services;

namespace AiderVSExtension.Completion
{
    /// <summary>
    /// AI-powered completion source for Visual Studio
    /// </summary>
    [Export(typeof(IAsyncCompletionSource))]
    [Name("AiderAICompletionSource")]
    [ContentType("text")]
    [Order(Before = "default")]
    internal class AICompletionSource : IAsyncCompletionSource
    {
        private readonly ICompletionProvider _completionProvider;
        private readonly ITextStructureNavigatorSelectorService _structureNavigatorSelector;
        private readonly IErrorHandler _errorHandler;
        private readonly IConfigurationService _configurationService;

        [ImportingConstructor]
        public AICompletionSource(
            ITextStructureNavigatorSelectorService structureNavigatorSelector,
            [Import(AllowDefault = true)] ICompletionProvider completionProvider,
            [Import(AllowDefault = true)] IErrorHandler errorHandler,
            [Import(AllowDefault = true)] IConfigurationService configurationService)
        {
            _structureNavigatorSelector = structureNavigatorSelector;
            _completionProvider = completionProvider;
            _errorHandler = errorHandler;
            _configurationService = configurationService;
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            try
            {
                // Check if AI completion is enabled
                if (!IsAICompletionEnabled())
                {
                    return CompletionStartData.DoesNotParticipateInCompletion;
                }

                // Check if we should trigger completion
                if (!ShouldTriggerCompletion(trigger, triggerLocation))
                {
                    return CompletionStartData.DoesNotParticipateInCompletion;
                }

                // Find the applicable span
                var applicableSpan = FindApplicableSpan(triggerLocation);
                if (applicableSpan == null)
                {
                    return CompletionStartData.DoesNotParticipateInCompletion;
                }

                return new CompletionStartData(
                    participation: CompletionParticipation.ProvidesItems,
                    applicableToSpan: applicableSpan.Value);
            }
            catch (Exception ex)
            {
                _errorHandler?.LogErrorAsync($"Error initializing completion: {ex.Message}", ex, "AICompletionSource.InitializeCompletion");
                return CompletionStartData.DoesNotParticipateInCompletion;
            }
        }

        public async Task<Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            try
            {
                if (_completionProvider == null || !IsAICompletionEnabled())
                {
                    return new Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionContext(ImmutableArray<Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem>.Empty);
                }

                // Create completion context
                var context = CreateCompletionContext(triggerLocation, trigger);
                
                // Get AI completions
                var aiCompletions = await _completionProvider.GetCompletionsAsync(context, token);
                
                // Convert to VS completion items
                var vsCompletionItems = ConvertToVSCompletionItems(aiCompletions, applicableToSpan);
                
                await _errorHandler?.LogInfoAsync($"Generated {vsCompletionItems.Length} AI completion items", "AICompletionSource.GetCompletionContextAsync");
                
                return new CompletionContext(vsCompletionItems);
            }
            catch (Exception ex)
            {
                await _errorHandler?.LogErrorAsync($"Error getting completion context: {ex.Message}", ex, "AICompletionSource.GetCompletionContextAsync");
                return new CompletionContext(ImmutableArray<CompletionItem>.Empty);
            }
        }

        public async Task<object> GetDescriptionAsync(
            IAsyncCompletionSession session,
            Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem item,
            CancellationToken token)
        {
            try
            {
                if (_completionProvider == null || !IsAICompletionEnabled())
                {
                    return null;
                }

                // Check if this is our item
                if (item.Properties.TryGetProperty<Interfaces.CompletionItem>("AICompletionItem", out var aiItem))
                {
                    // Get detailed information from AI
                    var details = await _completionProvider.GetCompletionDetailsAsync(aiItem, token);
                    
                    if (details != null)
                    {
                        return CreateDescriptionContent(details);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                await _errorHandler?.LogErrorAsync($"Error getting completion description: {ex.Message}", ex, "AICompletionSource.GetDescriptionAsync");
                return null;
            }
        }

        private bool IsAICompletionEnabled()
        {
            try
            {
                return _configurationService?.GetSetting(Models.AiderVSExtension.Models.Constants.ConfigurationKeys.AICompletionEnabled, Models.Constants.DefaultValues.DefaultAICompletionEnabled) ?? false;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error checking AI completion setting: {ex.Message}", "AICompletionSource.IsAICompletionEnabled");
                return false;
            }
        }

        private bool ShouldTriggerCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation)
        {
            try
            {
                if (_completionProvider == null)
                    return false;

                // Always trigger on explicit invocation
                if (trigger.Reason == CompletionTriggerReason.Invoke)
                    return true;

                // Check trigger characters
                if (trigger.Reason == CompletionTriggerReason.Insertion)
                {
                    var triggerChar = trigger.Character;
                    var triggerChars = _completionProvider.TriggerCharacters;
                    return triggerChars.Contains(triggerChar);
                }

                return false;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error checking completion trigger: {ex.Message}", "AICompletionSource.ShouldTriggerCompletion");
                return false;
            }
        }

        private SnapshotSpan? FindApplicableSpan(SnapshotPoint triggerLocation)
        {
            try
            {
                var snapshot = triggerLocation.Snapshot;
                var line = snapshot.GetLineFromPosition(triggerLocation.Position);
                var lineText = line.GetText();
                
                // Find the word being typed
                var position = triggerLocation.Position - line.Start.Position;
                var start = position;
                var end = position;

                // Find start of word
                while (start > 0 && IsIdentifierCharacter(lineText[start - 1]))
                {
                    start--;
                }

                // Find end of word
                while (end < lineText.Length && IsIdentifierCharacter(lineText[end]))
                {
                    end++;
                }

                var spanStart = line.Start.Position + start;
                var spanEnd = line.Start.Position + end;

                return new SnapshotSpan(snapshot, spanStart, spanEnd - spanStart);
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error finding applicable span: {ex.Message}", "AICompletionSource.FindApplicableSpan");
                return null;
            }
        }

        private bool IsIdentifierCharacter(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        private Interfaces.CompletionContext CreateCompletionContext(SnapshotPoint triggerLocation, CompletionTrigger trigger)
        {
            var snapshot = triggerLocation.Snapshot;
            var line = snapshot.GetLineFromPosition(triggerLocation.Position);
            var lineNumber = line.LineNumber;
            var column = triggerLocation.Position - line.Start.Position;
            
            // Get text before and after cursor
            var textBefore = snapshot.GetText(0, triggerLocation.Position);
            var textAfter = snapshot.GetText(triggerLocation.Position, snapshot.Length - triggerLocation.Position);
            
            // Get file path
            var filePath = GetFilePath(snapshot.TextBuffer);
            
            // Get language
            var language = GetLanguage(snapshot.TextBuffer);

            return new Interfaces.CompletionContext
            {
                FilePath = filePath,
                Language = language,
                TextBeforeCursor = textBefore,
                TextAfterCursor = textAfter,
                Line = lineNumber + 1, // Convert to 1-based
                Column = column,
                TriggerCharacter = trigger.Character,
                TriggerKind = ConvertTriggerKind(trigger.Reason),
                ProjectContext = GetProjectContext(snapshot.TextBuffer),
                ImportedNamespaces = GetImportedNamespaces(snapshot),
                AdditionalContext = new Dictionary<string, object>()
            };
        }

        private string GetFilePath(ITextBuffer textBuffer)
        {
            try
            {
                if (textBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDocument))
                {
                    return textDocument.FilePath;
                }
                return null;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error getting file path: {ex.Message}", "AICompletionSource.GetFilePath");
                return null;
            }
        }

        private string GetLanguage(ITextBuffer textBuffer)
        {
            try
            {
                var contentType = textBuffer.ContentType;
                return contentType.DisplayName;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error getting language: {ex.Message}", "AICompletionSource.GetLanguage");
                return "text";
            }
        }

        private string GetProjectContext(ITextBuffer textBuffer)
        {
            try
            {
                // Extract project context from current solution and file
                var context = new System.Collections.Generic.List<string>();
                
                // Get current file information from text buffer properties
                string filePath = null;
                if (textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc))
                {
                    filePath = textDoc.FilePath;
                }
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    context.Add($"Current file: {System.IO.Path.GetFileName(filePath)}");
                    
                    // Try to get project type from file extension
                    var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                    switch (extension)
                    {
                        case ".cs":
                            context.Add("Language: C#");
                            break;
                        case ".vb":
                            context.Add("Language: Visual Basic");
                            break;
                        case ".cpp":
                        case ".h":
                            context.Add("Language: C++");
                            break;
                        case ".js":
                        case ".ts":
                            context.Add("Language: JavaScript/TypeScript");
                            break;
                    }
                }
                
                // Get namespace/class context from buffer content
                var bufferText = textBuffer.CurrentSnapshot.GetText();
                if (!string.IsNullOrEmpty(bufferText))
                {
                    var lines = bufferText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines.Take(50)) // Look at first 50 lines for context
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("namespace "))
                        {
                            context.Add($"Namespace: {trimmedLine}");
                        }
                        else if (trimmedLine.Contains("class ") || trimmedLine.Contains("interface ") || trimmedLine.Contains("struct "))
                        {
                            context.Add($"Type: {trimmedLine}");
                        }
                        else if (trimmedLine.StartsWith("using ") && trimmedLine.EndsWith(";"))
                        {
                            context.Add($"Import: {trimmedLine}");
                        }
                    }
                }
                
                return context.Any() ? string.Join("\n", context) : null;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error getting project context: {ex.Message}", "AICompletionSource.GetProjectContext");
                return null;
            }
        }

        private List<string> GetImportedNamespaces(ITextSnapshot snapshot)
        {
            try
            {
                var namespaces = new List<string>();
                var text = snapshot.GetText();
                var lines = text.Split('\n');

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                    {
                        var namespaceName = trimmed.Substring(6, trimmed.Length - 7).Trim();
                        namespaces.Add(namespaceName);
                    }
                }

                return namespaces;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error getting imported namespaces: {ex.Message}", "AICompletionSource.GetImportedNamespaces");
                return new List<string>();
            }
        }

        private Interfaces.CompletionTriggerKind ConvertTriggerKind(CompletionTriggerReason reason)
        {
            return reason switch
            {
                CompletionTriggerReason.Invoke => Interfaces.CompletionTriggerKind.Invoked,
                CompletionTriggerReason.Insertion => Interfaces.CompletionTriggerKind.TriggerCharacter,
                CompletionTriggerReason.Backspace => Interfaces.CompletionTriggerKind.TriggerForIncompleteCompletions,
                _ => Interfaces.CompletionTriggerKind.Invoked
            };
        }

        private ImmutableArray<Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem> ConvertToVSCompletionItems(
            IEnumerable<Interfaces.CompletionItem> aiCompletions,
            SnapshotSpan applicableToSpan)
        {
            try
            {
                var items = new List<CompletionItem>();

                foreach (var aiItem in aiCompletions)
                {
                    var vsItem = new CompletionItem(
                        displayText: aiItem.Label,
                        source: this,
                        icon: GetCompletionIcon(aiItem.Kind),
                        filters: ImmutableArray.Create(GetCompletionFilter(aiItem.Kind)),
                        suffix: aiItem.Detail,
                        insertText: aiItem.InsertText ?? aiItem.Label,
                        sortText: aiItem.SortText ?? aiItem.Label,
                        filterText: aiItem.FilterText ?? aiItem.Label,
                        automationText: aiItem.Label,
                        attributeIcons: ImmutableArray<ImageMoniker>.Empty);

                    // Store the original AI item for later reference
                    vsItem.Properties.AddProperty("AICompletionItem", aiItem);

                    items.Add(vsItem);
                }

                return items.ToImmutableArray();
            }
            catch (Exception ex)
            {
                _errorHandler?.LogErrorAsync($"Error converting completion items: {ex.Message}", ex, "AICompletionSource.ConvertToVSCompletionItems");
                return ImmutableArray<CompletionItem>.Empty;
            }
        }

        private ImageMoniker GetCompletionIcon(Interfaces.CompletionItemKind kind)
        {
            try
            {
                // Map AI completion kinds to Visual Studio's standard completion icons
                var iconMoniker = kind switch
                {
                    Interfaces.CompletionItemKind.Method => KnownMonikers.Method,
                    Interfaces.CompletionItemKind.Function => KnownMonikers.Method,
                    Interfaces.CompletionItemKind.Constructor => KnownMonikers.Method,
                    Interfaces.CompletionItemKind.Field => KnownMonikers.Field,
                    Interfaces.CompletionItemKind.Variable => KnownMonikers.LocalVariable,
                    Interfaces.CompletionItemKind.Class => KnownMonikers.Class,
                    Interfaces.CompletionItemKind.Interface => KnownMonikers.Interface,
                    Interfaces.CompletionItemKind.Module => KnownMonikers.Module,
                    Interfaces.CompletionItemKind.Property => KnownMonikers.Property,
                    Interfaces.CompletionItemKind.Event => KnownMonikers.Event,
                    Interfaces.CompletionItemKind.Enum => KnownMonikers.Enumeration,
                    Interfaces.CompletionItemKind.Keyword => KnownMonikers.Keyword,
                    Interfaces.CompletionItemKind.Snippet => KnownMonikers.Snippet,
                    Interfaces.CompletionItemKind.Text => KnownMonikers.String,
                    Interfaces.CompletionItemKind.Color => KnownMonikers.ColorPalette,
                    Interfaces.CompletionItemKind.File => KnownMonikers.Document,
                    Interfaces.CompletionItemKind.Reference => KnownMonikers.Reference,
                    Interfaces.CompletionItemKind.Folder => KnownMonikers.Folder,
                    Interfaces.CompletionItemKind.EnumMember => KnownMonikers.EnumerationItemPublic,
                    Interfaces.CompletionItemKind.Constant => KnownMonikers.Constant,
                    Interfaces.CompletionItemKind.Struct => KnownMonikers.Structure,
                    Interfaces.CompletionItemKind.Unit => KnownMonikers.Namespace,
                    Interfaces.CompletionItemKind.Value => KnownMonikers.LocalVariable,
                    Interfaces.CompletionItemKind.TypeParameter => KnownMonikers.Type,
                    _ => KnownMonikers.Method // Default to method icon
                };

                return iconMoniker;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error getting completion icon: {ex.Message}", "AICompletionSource.GetCompletionIcon");
                return KnownMonikers.Method; // Fallback icon
            }
        }

        private CompletionFilter GetCompletionFilter(Interfaces.CompletionItemKind kind)
        {
            return kind switch
            {
                Interfaces.CompletionItemKind.Method => CompletionFilter.Method,
                Interfaces.CompletionItemKind.Function => CompletionFilter.Method,
                Interfaces.CompletionItemKind.Class => CompletionFilter.Class,
                Interfaces.CompletionItemKind.Interface => CompletionFilter.Interface,
                Interfaces.CompletionItemKind.Property => CompletionFilter.Property,
                Interfaces.CompletionItemKind.Field => CompletionFilter.Field,
                Interfaces.CompletionItemKind.Variable => CompletionFilter.LocalAndParameter,
                Interfaces.CompletionItemKind.Keyword => CompletionFilter.Keyword,
                Interfaces.CompletionItemKind.Snippet => CompletionFilter.Snippet,
                _ => CompletionFilter.All
            };
        }

        private object CreateDescriptionContent(Interfaces.CompletionDetails details)
        {
            try
            {
                var content = new System.Text.StringBuilder();
                
                if (!string.IsNullOrEmpty(details.DetailedDescription))
                {
                    content.AppendLine(details.DetailedDescription);
                }

                if (!string.IsNullOrEmpty(details.Documentation))
                {
                    content.AppendLine();
                    content.AppendLine(details.Documentation);
                }

                if (details.Parameters.Any())
                {
                    content.AppendLine();
                    content.AppendLine("Parameters:");
                    foreach (var param in details.Parameters)
                    {
                        content.AppendLine($"  {param.Name} ({param.Type}): {param.Description}");
                    }
                }

                if (!string.IsNullOrEmpty(details.ReturnType))
                {
                    content.AppendLine();
                    content.AppendLine($"Returns: {details.ReturnType}");
                }

                if (details.Examples.Any())
                {
                    content.AppendLine();
                    content.AppendLine("Examples:");
                    foreach (var example in details.Examples)
                    {
                        content.AppendLine($"  {example}");
                    }
                }

                return content.ToString();
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error creating description content: {ex.Message}", "AICompletionSource.CreateDescriptionContent");
                return details.Documentation ?? details.DetailedDescription ?? "AI-generated completion";
            }
        }
    }
}