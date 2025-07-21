using System;

namespace AiderVSExtension.Constants
{
    /// <summary>
    /// Package GUIDs for the Aider Visual Studio Extension
    /// </summary>
    public static class PackageGuids
    {
        /// <summary>
        /// GUID for the main Aider VS Extension package
        /// </summary>
        public const string AiderVSExtensionPackageString = "A7F8B2C1-3D4E-4F5A-9B8C-7E6D5A4B3C2F";
        
        /// <summary>
        /// GUID for the main Aider VS Extension package
        /// </summary>
        public static readonly Guid AiderVSExtensionPackage = new Guid(AiderVSExtensionPackageString);

        /// <summary>
        /// GUID for the chat tool window
        /// </summary>
        public const string ChatToolWindowString = "B8E9C3D2-4E5F-5A6B-AC9D-8F7E6B5C4D3E";
        
        /// <summary>
        /// GUID for the chat tool window
        /// </summary>
        public static readonly Guid ChatToolWindow = new Guid(ChatToolWindowString);

        /// <summary>
        /// GUID for the command set
        /// </summary>
        public const string CommandSetString = "C9FA4E53-5F60-6B7C-BD0E-9A8F7C6D5E4F";
        
        /// <summary>
        /// GUID for the command set
        /// </summary>
        public static readonly Guid CommandSet = new Guid(CommandSetString);

        /// <summary>
        /// GUID for the editor context menu
        /// </summary>
        public const string EditorContextMenuString = "D0AB5F64-6071-7C8D-CE1F-AB9A8D7E6F5A";
        
        /// <summary>
        /// GUID for the editor context menu
        /// </summary>
        public static readonly Guid EditorContextMenu = new Guid(EditorContextMenuString);

        /// <summary>
        /// GUID for the error list context menu
        /// </summary>
        public const string ErrorListContextMenuString = "E1BC6A75-7182-8D9E-DF2A-BCA9B8E9F7A6";
        
        /// <summary>
        /// GUID for the error list context menu
        /// </summary>
        public static readonly Guid ErrorListContextMenu = new Guid(ErrorListContextMenuString);

        /// <summary>
        /// GUID for the configuration page
        /// </summary>
        public const string ConfigurationPageString = "F2CD7B86-8293-9EAF-EA3B-CDBACAFA8B7C";
        
        /// <summary>
        /// GUID for the configuration page
        /// </summary>
        public static readonly Guid ConfigurationPage = new Guid(ConfigurationPageString);

        /// <summary>
        /// GUID for the completion provider
        /// </summary>
        public const string CompletionProviderString = "A3DE8C97-9344-AFBA-FB4C-DECBDBAB9C8D";
        
        /// <summary>
        /// GUID for the completion provider
        /// </summary>
        public static readonly Guid CompletionProvider = new Guid(CompletionProviderString);

        /// <summary>
        /// GUID for the quick fix provider
        /// </summary>
        public const string QuickFixProviderString = "B4EF9DA8-A455-BACB-AC5D-EFDCECBCAD9E";
        
        /// <summary>
        /// GUID for the quick fix provider
        /// </summary>
        public static readonly Guid QuickFixProvider = new Guid(QuickFixProviderString);
    }
}