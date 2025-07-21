using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Editor;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using AiderVSExtension.UI.Chat;

namespace AiderVSExtension.Editor
{
    /// <summary>
    /// Handles text selection in the editor and provides "Add to Chat" functionality
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class TextSelectionHandler : IWpfTextViewCreationListener
    {
        [Import]
        private IEditorOperationsFactoryService EditorOperationsFactory { get; set; }

        [Import]
        private ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        [Import]
        private SVsServiceProvider ServiceProvider { get; set; }

        private ServiceContainer _serviceContainer;
        private IAiderService _aiderService;
        private IFileContextService _fileContextService;

        public void TextViewCreated(IWpfTextView textView)
        {
            try
            {
                // Initialize services
                InitializeServices();

                // Subscribe to selection changed events
                textView.Selection.SelectionChanged += OnSelectionChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in TextSelectionHandler.TextViewCreated: {ex}");
            }
        }

        private void InitializeServices()
        {
            try
            {
                _serviceContainer = ServiceProvider.GetService(typeof(ServiceContainer)) as ServiceContainer;
                if (_serviceContainer != null)
                {
                    _aiderService = _serviceContainer.GetService<IAiderService>();
                    _fileContextService = _serviceContainer.GetService<IFileContextService>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing services in TextSelectionHandler: {ex}");
            }
        }


        private void OnSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                var textView = sender as IWpfTextView;
                if (textView == null) return;

                var selection = textView.Selection;
                if (selection.IsEmpty) return;

                // Get selected text information
                var selectedText = selection.StreamSelectionSpan.GetText();
                if (string.IsNullOrWhiteSpace(selectedText)) return;

                // Get file path and line numbers
                var filePath = GetFilePath(textView);
                var startLine = selection.Start.Position.GetContainingLine().LineNumber + 1;
                var endLine = selection.End.Position.GetContainingLine().LineNumber + 1;

                // Store selection information for context menu
                StoreSelectionInfo(textView, new SelectionInfo
                {
                    SelectedText = selectedText,
                    FilePath = filePath,
                    StartLine = startLine,
                    EndLine = endLine
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSelectionChanged: {ex}");
            }
        }

        private string GetFilePath(IWpfTextView textView)
        {
            try
            {
                var textDocument = textView.TextBuffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument));
                return textDocument?.FilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting file path: {ex}");
                return null;
            }
        }

        private void StoreSelectionInfo(IWpfTextView textView, SelectionInfo selectionInfo)
        {
            try
            {
                // Store selection info in the text view properties for later retrieval
                textView.Properties[typeof(SelectionInfo)] = selectionInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error storing selection info: {ex}");
            }
        }

        /// <summary>
        /// Gets the current selection information from the active text view
        /// </summary>
        /// <returns>Selection information or null if no selection</returns>
        public static SelectionInfo GetCurrentSelection()
        {
            try
            {
                // Get the active text view
                var activeTextView = GetActiveTextView();
                if (activeTextView == null) return null;

                // Get stored selection info
                if (activeTextView.Properties.TryGetProperty<SelectionInfo>(typeof(SelectionInfo), out var selectionInfo))
                {
                    return selectionInfo;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current selection: {ex}");
                return null;
            }
        }

        private static IWpfTextView GetActiveTextView()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var serviceProvider = ServiceProvider.GlobalProvider;
                var textManager = serviceProvider.GetService(typeof(SVsTextManager)) as IVsTextManager;
                if (textManager == null) return null;

                textManager.GetActiveView(1, null, out IVsTextView activeView);
                if (activeView == null) return null;

                var userData = activeView as IVsUserData;
                if (userData == null) return null;

                var guidViewHost = DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out object holder);
                
                var viewHost = holder as IWpfTextViewHost;
                return viewHost?.TextView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting active text view: {ex}");
                return null;
            }
        }
    }

    /// <summary>
    /// Contains information about the current text selection
    /// </summary>
    public class SelectionInfo
    {
        public string SelectedText { get; set; }
        public string FilePath { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}