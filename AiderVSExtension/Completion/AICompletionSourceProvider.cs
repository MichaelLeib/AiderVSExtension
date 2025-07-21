using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Services;

namespace AiderVSExtension.Completion
{
    /// <summary>
    /// Provider for AI completion sources
    /// </summary>
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("AiderAICompletionSourceProvider")]
    [ContentType("CSharp")]
    [ContentType("JavaScript")]
    [ContentType("TypeScript")]
    [ContentType("Python")]
    [ContentType("JSON")]
    [ContentType("XML")]
    [ContentType("XAML")]
    internal class AICompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        [Import]
        private ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        [Import(AllowDefault = true)]
        private ICompletionProvider CompletionProvider { get; set; }

        [Import(AllowDefault = true)]
        private IErrorHandler ErrorHandler { get; set; }

        [Import(AllowDefault = true)]
        private IConfigurationService ConfigurationService { get; set; }

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            try
            {
                // Create a new completion source for this text view
                return new AICompletionSource(
                    TextStructureNavigatorSelector,
                    CompletionProvider,
                    ErrorHandler,
                    ConfigurationService);
            }
            catch (Exception ex)
            {
                ErrorHandler?.LogErrorAsync($"Error creating completion source: {ex.Message}", ex, "AICompletionSourceProvider.GetOrCreate");
                return null;
            }
        }
    }
}