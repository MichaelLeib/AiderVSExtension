namespace AiderVSExtension.Constants
{
    /// <summary>
    /// Command IDs for the Aider Visual Studio Extension
    /// </summary>
    public static class PackageIds
    {
        /// <summary>
        /// Command ID for opening the chat tool window
        /// </summary>
        public const int OpenChatToolWindowCommand = 0x0100;

        /// <summary>
        /// Command ID for adding selected text to chat
        /// </summary>
        public const int AddToAiderChatCommand = 0x0101;

        /// <summary>
        /// Command ID for fixing error with Aider
        /// </summary>
        public const int FixWithAiderCommand = 0x0102;

        /// <summary>
        /// Command ID for adding file to chat
        /// </summary>
        public const int AddFileToAiderChatCommand = 0x0103;

        /// <summary>
        /// Command ID for toggling AI completion
        /// </summary>
        public const int ToggleAICompletionCommand = 0x0104;

        /// <summary>
        /// Command ID for switching AI model
        /// </summary>
        public const int SwitchAIModelCommand = 0x0105;

        /// <summary>
        /// Command ID for opening extension settings
        /// </summary>
        public const int OpenSettingsCommand = 0x0106;

        /// <summary>
        /// Command ID for clearing chat history
        /// </summary>
        public const int ClearChatHistoryCommand = 0x0107;

        /// <summary>
        /// Command ID for exporting chat conversation
        /// </summary>
        public const int ExportChatCommand = 0x0108;

        /// <summary>
        /// Command ID for importing chat conversation
        /// </summary>
        public const int ImportChatCommand = 0x0109;

        /// <summary>
        /// Command ID for starting new chat session
        /// </summary>
        public const int NewChatSessionCommand = 0x010A;

        /// <summary>
        /// Command ID for adding project context to chat
        /// </summary>
        public const int AddProjectContextCommand = 0x010B;

        /// <summary>
        /// Command ID for adding Git context to chat
        /// </summary>
        public const int AddGitContextCommand = 0x010C;

        /// <summary>
        /// Command ID for adding current file to chat
        /// </summary>
        public const int AddCurrentFileCommand = 0x010D;

        /// <summary>
        /// Command ID for adding clipboard content to chat
        /// </summary>
        public const int AddClipboardCommand = 0x010E;

        /// <summary>
        /// Command ID for web search integration
        /// </summary>
        public const int WebSearchCommand = 0x010F;

        /// <summary>
        /// Command ID for documentation search
        /// </summary>
        public const int DocSearchCommand = 0x0110;

        /// <summary>
        /// Command ID for testing AI connection
        /// </summary>
        public const int TestAIConnectionCommand = 0x0111;

        /// <summary>
        /// Command ID for refreshing AI models
        /// </summary>
        public const int RefreshAIModelsCommand = 0x0112;

        /// <summary>
        /// Command ID for showing extension about dialog
        /// </summary>
        public const int ShowAboutCommand = 0x0113;

        /// <summary>
        /// Command ID for showing extension help
        /// </summary>
        public const int ShowHelpCommand = 0x0114;

        /// <summary>
        /// Command ID for reporting an issue
        /// </summary>
        public const int ReportIssueCommand = 0x0115;

        // Context menu groups
        /// <summary>
        /// Group ID for editor context menu items
        /// </summary>
        public const int EditorContextMenuGroup = 0x1020;

        /// <summary>
        /// Group ID for error list context menu items
        /// </summary>
        public const int ErrorListContextMenuGroup = 0x1021;

        /// <summary>
        /// Group ID for solution explorer context menu items
        /// </summary>
        public const int SolutionExplorerContextMenuGroup = 0x1022;

        /// <summary>
        /// Group ID for chat window context menu items
        /// </summary>
        public const int ChatWindowContextMenuGroup = 0x1023;

        // Menu IDs
        /// <summary>
        /// Menu ID for the main Aider menu
        /// </summary>
        public const int AiderMainMenu = 0x1050;

        /// <summary>
        /// Menu ID for the Aider toolbar
        /// </summary>
        public const int AiderToolbar = 0x1051;

        /// <summary>
        /// Menu ID for the context menu in editor
        /// </summary>
        public const int EditorContextMenu = 0x1052;

        /// <summary>
        /// Menu ID for the context menu in error list
        /// </summary>
        public const int ErrorListContextMenu = 0x1053;

        /// <summary>
        /// Menu ID for the context menu in solution explorer
        /// </summary>
        public const int SolutionExplorerContextMenu = 0x1054;

        /// <summary>
        /// Menu ID for the context menu in chat window
        /// </summary>
        public const int ChatWindowContextMenu = 0x1055;
    }
}