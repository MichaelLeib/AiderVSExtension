using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Completion
{
    /// <summary>
    /// AI-powered completion provider that generates code suggestions
    /// </summary>
    public class AICompletionProvider : ICompletionProvider
    {
        private readonly IAIModelManager _aiModelManager;
        private readonly IErrorHandler _errorHandler;
        private readonly IConfigurationService _configurationService;
        private readonly CompletionCache _cache;

        public bool IsEnabled { get; set; } = true;

        public IEnumerable<char> TriggerCharacters => new[] { '.', '(', '[', '<', ' ', '\n', '\t' };

        public AICompletionProvider(
            IAIModelManager aiModelManager,
            IErrorHandler errorHandler,
            IConfigurationService configurationService)
        {
            _aiModelManager = aiModelManager ?? throw new ArgumentNullException(nameof(aiModelManager));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _cache = new CompletionCache();
        }

        public async Task<IEnumerable<CompletionItem>> GetCompletionsAsync(CompletionContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (!IsEnabled || context == null)
                {
                    return Enumerable.Empty<CompletionItem>();
                }

                // Check cache first
                var cacheKey = CreateCacheKey(context);
                if (_cache.TryGetCompletions(cacheKey, out var cachedCompletions))
                {
                    await _errorHandler.LogInfoAsync("Retrieved completions from cache", "AICompletionProvider.GetCompletionsAsync");
                    return cachedCompletions;
                }

                // Generate AI completions
                var completions = await GenerateAICompletionsAsync(context, cancellationToken);
                
                // Cache the results
                _cache.CacheCompletions(cacheKey, completions);
                
                await _errorHandler.LogInfoAsync($"Generated {completions.Count()} AI completions", "AICompletionProvider.GetCompletionsAsync");
                
                return completions;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AICompletionProvider.GetCompletionsAsync");
                return Enumerable.Empty<CompletionItem>();
            }
        }

        public async Task<CompletionDetails> GetCompletionDetailsAsync(CompletionItem item, CancellationToken cancellationToken)
        {
            try
            {
                if (item == null)
                {
                    return null;
                }

                // Check if we have cached details
                var cacheKey = $"details_{item.Label}_{item.Kind}";
                if (_cache.TryGetDetails(cacheKey, out var cachedDetails))
                {
                    return cachedDetails;
                }

                // Generate detailed information using AI
                var details = await GenerateCompletionDetailsAsync(item, cancellationToken);
                
                // Cache the details
                if (details != null)
                {
                    _cache.CacheDetails(cacheKey, details);
                }
                
                return details;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AICompletionProvider.GetCompletionDetailsAsync");
                return null;
            }
        }

        public bool ShouldTriggerCompletion(CompletionContext context)
        {
            try
            {
                if (!IsEnabled || context == null)
                {
                    return false;
                }

                // Trigger on explicit invocation
                if (context.TriggerKind == CompletionTriggerKind.Invoked)
                {
                    return true;
                }

                // Trigger on specific characters
                if (context.TriggerKind == CompletionTriggerKind.TriggerCharacter)
                {
                    return TriggerCharacters.Contains(context.TriggerCharacter);
                }

                return false;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error checking completion trigger: {ex.Message}", "AICompletionProvider.ShouldTriggerCompletion");
                return false;
            }
        }

        private async Task<IEnumerable<CompletionItem>> GenerateAICompletionsAsync(CompletionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var completions = new List<CompletionItem>();

                // Get the current AI model
                var currentModel = _aiModelManager.GetCurrentModel();
                if (currentModel == null)
                {
                    await _errorHandler.LogWarningAsync("No AI model configured for completion", "AICompletionProvider.GenerateAICompletionsAsync");
                    return completions;
                }

                // Build the completion prompt
                var prompt = BuildCompletionPrompt(context);
                
                // Generate completions using the AI model
                var response = await _aiModelManager.GenerateCompletionAsync(prompt, cancellationToken);
                
                if (!string.IsNullOrEmpty(response))
                {
                    // Parse the AI response into completion items
                    var aiCompletions = ParseAIResponse(response, context);
                    completions.AddRange(aiCompletions);
                }

                // Add some common fallback completions
                completions.AddRange(GenerateFallbackCompletions(context));

                // Sort by priority
                return completions.OrderByDescending(c => c.Priority).Take(20);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AICompletionProvider.GenerateAICompletionsAsync");
                return Enumerable.Empty<CompletionItem>();
            }
        }

        private string BuildCompletionPrompt(CompletionContext context)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("You are a code completion assistant. Generate relevant code completions for the given context.");
            prompt.AppendLine();
            
            if (!string.IsNullOrEmpty(context.Language))
            {
                prompt.AppendLine($"Language: {context.Language}");
            }
            
            if (!string.IsNullOrEmpty(context.FilePath))
            {
                prompt.AppendLine($"File: {System.IO.Path.GetFileName(context.FilePath)}");
            }
            
            prompt.AppendLine($"Line: {context.Line}, Column: {context.Column}");
            prompt.AppendLine();
            
            // Add context before cursor
            if (!string.IsNullOrEmpty(context.TextBeforeCursor))
            {
                var contextLines = context.TextBeforeCursor.Split('\n');
                var relevantLines = contextLines.TakeLast(10).ToArray();
                
                prompt.AppendLine("Code context:");
                prompt.AppendLine("```");
                foreach (var line in relevantLines)
                {
                    prompt.AppendLine(line);
                }
                prompt.AppendLine("```");
                prompt.AppendLine();
            }
            
            // Add imported namespaces
            if (context.ImportedNamespaces.Any())
            {
                prompt.AppendLine("Available namespaces:");
                foreach (var ns in context.ImportedNamespaces)
                {
                    prompt.AppendLine($"- {ns}");
                }
                prompt.AppendLine();
            }
            
            prompt.AppendLine("Provide up to 10 relevant completions. Format each as:");
            prompt.AppendLine("COMPLETION: [label] | [insert_text] | [kind] | [detail]");
            prompt.AppendLine("Where kind is one of: method, function, class, interface, property, field, variable, keyword, snippet");
            
            return prompt.ToString();
        }

        private IEnumerable<CompletionItem> ParseAIResponse(string response, CompletionContext context)
        {
            try
            {
                var completions = new List<CompletionItem>();
                var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("COMPLETION:"))
                    {
                        var parts = line.Substring(11).Split('|');
                        if (parts.Length >= 4)
                        {
                            var label = parts[0].Trim();
                            var insertText = parts[1].Trim();
                            var kindStr = parts[2].Trim();
                            var detail = parts[3].Trim();
                            
                            if (Enum.TryParse<CompletionItemKind>(kindStr, true, out var kind))
                            {
                                completions.Add(new CompletionItem
                                {
                                    Label = label,
                                    InsertText = insertText,
                                    Kind = kind,
                                    Detail = detail,
                                    Priority = GetPriorityForKind(kind),
                                    FilterText = label,
                                    SortText = label
                                });
                            }
                        }
                    }
                }
                
                return completions;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error parsing AI response: {ex.Message}", "AICompletionProvider.ParseAIResponse");
                return Enumerable.Empty<CompletionItem>();
            }
        }

        private IEnumerable<CompletionItem> GenerateFallbackCompletions(CompletionContext context)
        {
            var completions = new List<CompletionItem>();
            
            try
            {
                // Add common language keywords based on context
                if (context.Language?.ToLower().Contains("csharp") == true)
                {
                    completions.AddRange(GetCSharpKeywords());
                }
                else if (context.Language?.ToLower().Contains("javascript") == true)
                {
                    completions.AddRange(GetJavaScriptKeywords());
                }
                
                // Add common snippets
                completions.AddRange(GetCommonSnippets(context));
                
                return completions;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error generating fallback completions: {ex.Message}", "AICompletionProvider.GenerateFallbackCompletions");
                return Enumerable.Empty<CompletionItem>();
            }
        }

        private IEnumerable<CompletionItem> GetCSharpKeywords()
        {
            var keywords = new[] { "public", "private", "protected", "internal", "static", "readonly", "const", "virtual", "override", "abstract", "sealed", "async", "await", "var", "new", "this", "base", "if", "else", "for", "foreach", "while", "do", "switch", "case", "default", "try", "catch", "finally", "throw", "using", "namespace", "class", "interface", "struct", "enum", "delegate", "event", "return", "break", "continue" };
            
            return keywords.Select(kw => new CompletionItem
            {
                Label = kw,
                InsertText = kw,
                Kind = CompletionItemKind.Keyword,
                Priority = 5,
                FilterText = kw,
                SortText = kw
            });
        }

        private IEnumerable<CompletionItem> GetJavaScriptKeywords()
        {
            var keywords = new[] { "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch", "case", "default", "try", "catch", "finally", "throw", "return", "break", "continue", "new", "this", "typeof", "instanceof", "true", "false", "null", "undefined", "async", "await", "class", "extends", "super", "import", "export", "from", "default" };
            
            return keywords.Select(kw => new CompletionItem
            {
                Label = kw,
                InsertText = kw,
                Kind = CompletionItemKind.Keyword,
                Priority = 5,
                FilterText = kw,
                SortText = kw
            });
        }

        private IEnumerable<CompletionItem> GetCommonSnippets(CompletionContext context)
        {
            var snippets = new List<CompletionItem>();
            
            if (context.Language?.ToLower().Contains("csharp") == true)
            {
                snippets.Add(new CompletionItem
                {
                    Label = "prop",
                    InsertText = "public ${1:int} ${2:Property} { get; set; }",
                    Kind = CompletionItemKind.Snippet,
                    Detail = "Auto-implemented property",
                    Priority = 8,
                    IsSnippet = true
                });
                
                snippets.Add(new CompletionItem
                {
                    Label = "ctor",
                    InsertText = "public ${1:ClassName}(${2:parameters})\n{\n    ${3:// constructor body}\n}",
                    Kind = CompletionItemKind.Snippet,
                    Detail = "Constructor",
                    Priority = 8,
                    IsSnippet = true
                });
            }
            
            return snippets;
        }

        private int GetPriorityForKind(CompletionItemKind kind)
        {
            return kind switch
            {
                CompletionItemKind.Method => 10,
                CompletionItemKind.Function => 10,
                CompletionItemKind.Property => 9,
                CompletionItemKind.Field => 8,
                CompletionItemKind.Variable => 7,
                CompletionItemKind.Class => 6,
                CompletionItemKind.Interface => 6,
                CompletionItemKind.Keyword => 5,
                CompletionItemKind.Snippet => 8,
                _ => 3
            };
        }

        private async Task<CompletionDetails> GenerateCompletionDetailsAsync(CompletionItem item, CancellationToken cancellationToken)
        {
            try
            {
                // Build prompt for detailed information
                var prompt = $"Provide detailed information about the following code completion item:\n\n" +
                           $"Item: {item.Label}\n" +
                           $"Kind: {item.Kind}\n" +
                           $"Detail: {item.Detail}\n\n" +
                           $"Please provide:\n" +
                           $"1. Detailed description\n" +
                           $"2. Parameters (if applicable)\n" +
                           $"3. Return type (if applicable)\n" +
                           $"4. Usage examples\n\n" +
                           $"Format as:\n" +
                           $"DESCRIPTION: [detailed description]\n" +
                           $"PARAMETERS: [param1:type:description, param2:type:description]\n" +
                           $"RETURNS: [return type]\n" +
                           $"EXAMPLE: [usage example]";

                var response = await _aiModelManager.GenerateCompletionAsync(prompt, cancellationToken);
                
                if (!string.IsNullOrEmpty(response))
                {
                    return ParseCompletionDetails(response);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AICompletionProvider.GenerateCompletionDetailsAsync");
                return null;
            }
        }

        private CompletionDetails ParseCompletionDetails(string response)
        {
            try
            {
                var details = new CompletionDetails();
                var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("DESCRIPTION:"))
                    {
                        details.DetailedDescription = line.Substring(12).Trim();
                    }
                    else if (line.StartsWith("PARAMETERS:"))
                    {
                        var paramStr = line.Substring(11).Trim();
                        details.Parameters = ParseParameters(paramStr);
                    }
                    else if (line.StartsWith("RETURNS:"))
                    {
                        details.ReturnType = line.Substring(8).Trim();
                    }
                    else if (line.StartsWith("EXAMPLE:"))
                    {
                        details.Examples.Add(line.Substring(8).Trim());
                    }
                }
                
                return details;
            }
            catch (Exception ex)
            {
                _errorHandler?.LogWarningAsync($"Error parsing completion details: {ex.Message}", "AICompletionProvider.ParseCompletionDetails");
                return new CompletionDetails();
            }
        }

        private List<ParameterInfo> ParseParameters(string paramStr)
        {
            var parameters = new List<ParameterInfo>();
            
            if (string.IsNullOrEmpty(paramStr))
                return parameters;
            
            var parts = paramStr.Split(',');
            foreach (var part in parts)
            {
                var paramParts = part.Split(':');
                if (paramParts.Length >= 3)
                {
                    parameters.Add(new ParameterInfo
                    {
                        Name = paramParts[0].Trim(),
                        Type = paramParts[1].Trim(),
                        Description = paramParts[2].Trim()
                    });
                }
            }
            
            return parameters;
        }

        private string CreateCacheKey(CompletionContext context)
        {
            return $"{context.FilePath}:{context.Line}:{context.Column}:{context.TextBeforeCursor?.GetHashCode()}";
        }
    }
}