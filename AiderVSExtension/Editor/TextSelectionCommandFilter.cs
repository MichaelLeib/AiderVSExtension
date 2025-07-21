using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.UI.Chat;

namespace AiderVSExtension.Editor
{
    /// <summary>
    /// Command filter for handling text selection commands and context menu actions
    /// </summary>
    internal class TextSelectionCommandFilter : IOleCommandTarget
    {
        private readonly IWpfTextView _textView;
        private readonly IEditorOperationsFactoryService _editorOperationsFactory;
        private readonly IAiderService _aiderService;
        private readonly IFileContextService _fileContextService;
        private readonly SVsServiceProvider _serviceProvider;

        public IOleCommandTarget NextCommandTarget { get; set; }

        // Command IDs for context menu
        private const uint CMDID_ADD_TO_CHAT = 0x0100;
        private static readonly Guid CMD_SET_GUID = new Guid("A7C02A2B-8B4E-4F5D-9B3C-1E2F3A4B5C6D");

        public TextSelectionCommandFilter(
            IWpfTextView textView,
            IEditorOperationsFactoryService editorOperationsFactory,
            IAiderService aiderService,
            IFileContextService fileContextService,
            SVsServiceProvider serviceProvider)
        {
            _textView = textView;
            _editorOperationsFactory = editorOperationsFactory;
            _aiderService = aiderService;
            _fileContextService = fileContextService;
            _serviceProvider = serviceProvider;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            try
            {
                if (pguidCmdGroup == CMD_SET_GUID)
                {
                    for (uint i = 0; i < cCmds; i++)
                    {
                        switch (prgCmds[i].cmdID)
                        {
                            case CMDID_ADD_TO_CHAT:
                                // Enable the command only if there's a text selection
                                var hasSelection = !_textView.Selection.IsEmpty;
                                prgCmds[i].cmdf = hasSelection
                                    ? (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED)
                                    : (uint)OLECMDF.OLECMDF_SUPPORTED;
                                return VSConstants.S_OK;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in QueryStatus: {ex}");
            }

            return NextCommandTarget?.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText) ?? VSConstants.E_FAIL;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            try
            {
                if (pguidCmdGroup == CMD_SET_GUID)
                {
                    switch (nCmdID)
                    {
                        case CMDID_ADD_TO_CHAT:
                            HandleAddToChat();
                            return VSConstants.S_OK;
                    }
                }

                // Handle standard commands
                if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
                {
                    switch (nCmdID)
                    {
                        case (uint)VSConstants.VSStd97CmdID.Copy:
                        case (uint)VSConstants.VSStd97CmdID.Cut:
                            // Store selection info when copying/cutting
                            StoreCurrentSelection();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Exec: {ex}");
            }

            return NextCommandTarget?.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut) ?? VSConstants.E_FAIL;
        }

        private void HandleAddToChat()
        {
            try
            {
                if (_textView.Selection.IsEmpty)
                {
                    ShowMessage("No text selected. Please select some text to add to chat.");
                    return;
                }

                var selectionInfo = GetCurrentSelectionInfo();
                if (selectionInfo == null)
                {
                    ShowMessage("Unable to get selection information.");
                    return;
                }

                // Create a file reference from the selection
                var fileReference = new FileReference
                {
                    FilePath = selectionInfo.FilePath,
                    Content = selectionInfo.SelectedText,
                    Type = ReferenceType.Selection,
                    StartLine = selectionInfo.StartLine,
                    EndLine = selectionInfo.EndLine,
                    Timestamp = DateTime.Now
                };

                // Add to chat
                AddToChat(fileReference);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleAddToChat: {ex}");
                ShowMessage($"Error adding to chat: {ex.Message}");
            }
        }

        private void StoreCurrentSelection()
        {
            try
            {
                if (_textView.Selection.IsEmpty) return;

                var selectionInfo = GetCurrentSelectionInfo();
                if (selectionInfo != null)
                {
                    _textView.Properties[typeof(SelectionInfo)] = selectionInfo;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error storing current selection: {ex}");
            }
        }

        private SelectionInfo GetCurrentSelectionInfo()
        {
            try
            {
                var selection = _textView.Selection;
                if (selection.IsEmpty) return null;

                var selectedText = selection.StreamSelectionSpan.GetText();
                if (string.IsNullOrWhiteSpace(selectedText)) return null;

                var filePath = GetFilePath();
                var startLine = selection.Start.Position.GetContainingLine().LineNumber + 1;
                var endLine = selection.End.Position.GetContainingLine().LineNumber + 1;

                return new SelectionInfo
                {
                    SelectedText = selectedText,
                    FilePath = filePath,
                    StartLine = startLine,
                    EndLine = endLine,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting selection info: {ex}");
                return null;
            }
        }

        private string GetFilePath()
        {
            try
            {
                if (_textView.TextBuffer.Properties.TryGetProperty<Microsoft.VisualStudio.Text.ITextDocument>(typeof(Microsoft.VisualStudio.Text.ITextDocument), out var textDocument))
                {
                    return textDocument?.FilePath;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting file path: {ex}");
                return null;
            }
        }

        private void AddToChat(FileReference fileReference)
        {
            try
            {
                // Get the chat tool window
                var chatToolWindow = GetChatToolWindow();
                if (chatToolWindow?.ChatControl != null)
                {
                    // Add the file reference to the chat control
                    chatToolWindow.ChatControl.AddFileReference(fileReference);
                    
                    // Show the chat window
                    ShowChatWindow(chatToolWindow);
                    
                    ShowMessage($"Added selection to chat: {fileReference.DisplayName}");
                }
                else
                {
                    ShowMessage("Chat window not available. Please open the Aider AI Chat window.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding to chat: {ex}");
                ShowMessage($"Error adding to chat: {ex.Message}");
            }
        }

        private ChatToolWindow GetChatToolWindow()
        {
            try
            {
                var package = _serviceProvider.GetService(typeof(AiderVSExtensionPackage)) as AiderVSExtensionPackage;
                return package?.GetChatToolWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting chat tool window: {ex}");
                return null;
            }
        }

        private void ShowChatWindow(ChatToolWindow chatToolWindow)
        {
            try
            {
                var windowFrame = chatToolWindow.Frame as Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame;
                if (windowFrame != null)
                {
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing chat window: {ex}");
            }
        }

        private void ShowMessage(string message)
        {
            try
            {
                var uiShell = _serviceProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell)) as Microsoft.VisualStudio.Shell.Interop.IVsUIShell;
                if (uiShell != null)
                {
                    var clsid = Guid.Empty;
                    uiShell.ShowMessageBox(
                        0,
                        ref clsid,
                        "Aider VS Extension",
                        message,
                        string.Empty,
                        0,
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_INFO,
                        0,
                        out int result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing message: {ex}");
            }
        }
    }
}