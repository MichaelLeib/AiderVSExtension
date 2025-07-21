using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using AiderVSExtension.Services;
using AiderVSExtension.UI.Chat;

namespace AiderVSExtension.Editor
{
    /// <summary>
    /// Provides context menu commands for editor integration
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class EditorContextMenuProvider : IWpfTextViewCreationListener
    {
        [Import]
        private SVsServiceProvider ServiceProvider { get; set; }

        // Command IDs
        private const int CMDID_ADD_TO_CHAT = 0x0100;
        private const int CMDID_SHOW_CHAT_WINDOW = 0x0101;

        // Command set GUID
        private static readonly Guid CommandSetGuid = new Guid("A7C02A2B-8B4E-4F5D-9B3C-1E2F3A4B5C6D");

        public void TextViewCreated(IWpfTextView textView)
        {
            try
            {
                // Create and register context menu commands
                RegisterContextMenuCommands(textView);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EditorContextMenuProvider.TextViewCreated: {ex}");
            }
        }

        private void RegisterContextMenuCommands(IWpfTextView textView)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                // Get the command service
                var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
                if (commandService == null) return;

                // Create "Add to Chat" command
                var addToChatCommandId = new CommandID(CommandSetGuid, CMDID_ADD_TO_CHAT);
                var addToChatCommand = new MenuCommand(
                    (sender, e) => HandleAddToChat(textView),
                    addToChatCommandId);
                
                addToChatCommand.BeforeQueryStatus += (sender, e) =>
                {
                    var command = sender as MenuCommand;
                    if (command != null)
                    {
                        command.Enabled = !textView.Selection.IsEmpty;
                        command.Visible = true;
                    }
                };

                commandService.AddCommand(addToChatCommand);

                // Create "Show Chat Window" command
                var showChatCommandId = new CommandID(CommandSetGuid, CMDID_SHOW_CHAT_WINDOW);
                var showChatCommand = new MenuCommand(
                    (sender, e) => HandleShowChatWindow(),
                    showChatCommandId);

                showChatCommand.BeforeQueryStatus += (sender, e) =>
                {
                    var command = sender as MenuCommand;
                    if (command != null)
                    {
                        command.Enabled = true;
                        command.Visible = true;
                    }
                };

                commandService.AddCommand(showChatCommand);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering context menu commands: {ex}");
            }
        }

        private void HandleAddToChat(IWpfTextView textView)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (textView.Selection.IsEmpty)
                {
                    ShowMessage("No text selected. Please select some text to add to chat.");
                    return;
                }

                var selectionInfo = GetSelectionInfo(textView);
                if (selectionInfo == null)
                {
                    ShowMessage("Unable to get selection information.");
                    return;
                }

                // Create a file reference from the selection
                var fileReference = new Models.FileReference
                {
                    FilePath = selectionInfo.FilePath,
                    Content = selectionInfo.SelectedText,
                    Type = Models.ReferenceType.Selection,
                    StartLine = selectionInfo.StartLine,
                    EndLine = selectionInfo.EndLine,
                    Timestamp = DateTime.Now
                };

                // Get the chat tool window and add the reference
                var package = ServiceProvider.GetService(typeof(AiderVSExtensionPackage)) as AiderVSExtensionPackage;
                var chatWindow = package?.GetChatToolWindow();
                
                if (chatWindow?.ChatControl != null)
                {
                    chatWindow.ChatControl.AddFileReference(fileReference);
                    package.ShowChatToolWindow();
                    ShowMessage($"Added selection to chat: {fileReference.DisplayName}");
                }
                else
                {
                    ShowMessage("Chat window not available. Please open the Aider AI Chat window.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleAddToChat: {ex}");
                ShowMessage($"Error adding to chat: {ex.Message}");
            }
        }

        private void HandleShowChatWindow()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var package = ServiceProvider.GetService(typeof(AiderVSExtensionPackage)) as AiderVSExtensionPackage;
                package?.ShowChatToolWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleShowChatWindow: {ex}");
                ShowMessage($"Error showing chat window: {ex.Message}");
            }
        }

        private SelectionInfo GetSelectionInfo(IWpfTextView textView)
        {
            try
            {
                var selection = textView.Selection;
                if (selection.IsEmpty) return null;

                var selectedText = selection.StreamSelectionSpan.GetText();
                if (string.IsNullOrWhiteSpace(selectedText)) return null;

                var filePath = GetFilePath(textView);
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

        private string GetFilePath(IWpfTextView textView)
        {
            try
            {
                if (textView.TextBuffer.Properties.TryGetProperty<Microsoft.VisualStudio.Text.ITextDocument>(typeof(Microsoft.VisualStudio.Text.ITextDocument), out var textDocument))
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

        private void ShowMessage(string message)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var uiShell = ServiceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
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