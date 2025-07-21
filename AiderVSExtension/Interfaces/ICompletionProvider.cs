using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for AI-powered completion provider
    /// </summary>
    public interface ICompletionProvider
    {
        /// <summary>
        /// Gets completion suggestions for the given context
        /// </summary>
        /// <param name="context">The completion context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of completion items</returns>
        Task<IEnumerable<CompletionItem>> GetCompletionsAsync(CompletionContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Gets detailed information for a completion item
        /// </summary>
        /// <param name="item">The completion item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Detailed completion information</returns>
        Task<CompletionDetails> GetCompletionDetailsAsync(CompletionItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Determines if completion should be triggered for the given context
        /// </summary>
        /// <param name="context">The completion context</param>
        /// <returns>True if completion should be triggered</returns>
        bool ShouldTriggerCompletion(CompletionContext context);

        /// <summary>
        /// Gets the completion trigger characters
        /// </summary>
        IEnumerable<char> TriggerCharacters { get; }

        /// <summary>
        /// Gets or sets whether the provider is enabled
        /// </summary>
        bool IsEnabled { get; set; }
    }

    /// <summary>
    /// Represents completion context information
    /// </summary>
    public class CompletionContext
    {
        public string FilePath { get; set; }
        public string Language { get; set; }
        public string TextBeforeCursor { get; set; }
        public string TextAfterCursor { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public char TriggerCharacter { get; set; }
        public CompletionTriggerKind TriggerKind { get; set; }
        public string ProjectContext { get; set; }
        public List<string> ImportedNamespaces { get; set; }
        public Dictionary<string, object> AdditionalContext { get; set; }

        public CompletionContext()
        {
            ImportedNamespaces = new List<string>();
            AdditionalContext = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Represents a completion item
    /// </summary>
    public class CompletionItem
    {
        public string Label { get; set; }
        public string InsertText { get; set; }
        public string Detail { get; set; }
        public string Documentation { get; set; }
        public CompletionItemKind Kind { get; set; }
        public int Priority { get; set; }
        public bool IsSnippet { get; set; }
        public string FilterText { get; set; }
        public string SortText { get; set; }
        public List<TextEdit> AdditionalTextEdits { get; set; }
        public object Data { get; set; }

        public CompletionItem()
        {
            AdditionalTextEdits = new List<TextEdit>();
            Priority = 0;
        }
    }

    /// <summary>
    /// Represents detailed completion information
    /// </summary>
    public class CompletionDetails
    {
        public string Documentation { get; set; }
        public string DetailedDescription { get; set; }
        public List<ParameterInfo> Parameters { get; set; }
        public string ReturnType { get; set; }
        public List<string> Examples { get; set; }

        public CompletionDetails()
        {
            Parameters = new List<ParameterInfo>();
            Examples = new List<string>();
        }
    }

    /// <summary>
    /// Represents a text edit
    /// </summary>
    public class TextEdit
    {
        public TextRange Range { get; set; }
        public string NewText { get; set; }
    }

    /// <summary>
    /// Represents a text range
    /// </summary>
    public class TextRange
    {
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
    }

    /// <summary>
    /// Represents parameter information
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool IsOptional { get; set; }
        public string DefaultValue { get; set; }
    }

    /// <summary>
    /// Completion trigger kinds
    /// </summary>
    public enum CompletionTriggerKind
    {
        Invoked,
        TriggerCharacter,
        TriggerForIncompleteCompletions
    }

    /// <summary>
    /// Completion item kinds
    /// </summary>
    public enum CompletionItemKind
    {
        Text,
        Method,
        Function,
        Constructor,
        Field,
        Variable,
        Class,
        Interface,
        Module,
        Property,
        Unit,
        Value,
        Enum,
        Keyword,
        Snippet,
        Color,
        File,
        Reference,
        Folder,
        EnumMember,
        Constant,
        Struct,
        Event,
        Operator,
        TypeParameter
    }
}