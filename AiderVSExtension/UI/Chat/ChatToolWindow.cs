using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using AiderVSExtension.Models;

namespace AiderVSExtension.UI.Chat
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// This class derives from the ToolWindowPane class provided by the Managed Package Framework
    /// and defines the behavior of this tool window.
    /// </remarks>
    [Guid("1D9C7B8A-5E4F-4C3A-B2D1-8F3E5A4C7B9D")]
    public sealed class ChatToolWindow : ToolWindowPane
    {
        private ChatControl _chatControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatToolWindow"/> class.
        /// </summary>
        public ChatToolWindow() : base(null)
        {
            Caption = "Aider AI Chat";
            BitmapResourceID = 301;
            BitmapIndex = 1;

            // Create the chat control
            _chatControl = new ChatControl();
            Content = _chatControl;
        }

        /// <summary>
        /// Gets the chat control instance
        /// </summary>
        public ChatControl ChatControl => _chatControl;

        /// <summary>
        /// This method is called when the tool window is created.
        /// </summary>
        /// <param name="frame">The tool window frame</param>
        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            
            // Initialize the chat control with services
            InitializeChatControl();
        }

        /// <summary>
        /// Initializes the chat control with required services
        /// </summary>
        private void InitializeChatControl()
        {
            try
            {
                // Get the service container from the package
                var package = Package.GetGlobalService(typeof(SProfferService)) as IServiceProvider;
                if (package != null)
                {
                    _chatControl.Initialize(package);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error initializing chat control: {ex.Message}");
            }
        }

        /// <summary>
        /// This method is called when the tool window is being closed.
        /// </summary>
        protected override void OnClose()
        {
            // Clean up resources
            _chatControl?.Dispose();
            base.OnClose();
        }

        /// <summary>
        /// Handles the search in the tool window
        /// </summary>
        /// <param name="task">The search task</param>
        /// <returns>True if the search was handled</returns>
        public override bool SearchEnabled => true;

        /// <summary>
        /// Provides search functionality for the tool window
        /// </summary>
        /// <param name="search">The search object</param>
        public override void ProvideSearchSettings(IVsUIDataSource search)
        {
            // Allow searching within chat messages
            Utilities.SetValue(search, SearchSettingsDataSource.PropertyNames.ControlMaxWidth, (uint)200);
            Utilities.SetValue(search, SearchSettingsDataSource.PropertyNames.SearchStartType, (uint)VSSEARCHSTARTTYPE.SST_INSTANT);
        }

        /// <summary>
        /// Adds a file reference to the chat input
        /// </summary>
        /// <param name="fileReference">The file reference to add</param>
        public void AddFileReferenceToChat(FileReference fileReference)
        {
            try
            {
                if (fileReference != null)
                {
                    _chatControl?.AddFileReference(fileReference);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding file reference to chat: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles search within the tool window
        /// </summary>
        /// <param name="search">The search task</param>
        /// <returns>The search result</returns>
        public override IVsSearchTask CreateSearch(uint cookie, IVsSearchQuery searchQuery, IVsSearchCallback searchCallback)
        {
            if (searchQuery == null)
                return null;

            return new ChatSearchTask(cookie, searchQuery, searchCallback, _chatControl);
        }
    }

    /// <summary>
    /// Implementation of search task for chat messages
    /// </summary>
    internal class ChatSearchTask : VsSearchTask
    {
        private readonly ChatControl _chatControl;

        public ChatSearchTask(uint cookie, IVsSearchQuery searchQuery, IVsSearchCallback searchCallback, ChatControl chatControl)
            : base(cookie, searchQuery, searchCallback)
        {
            _chatControl = chatControl;
        }

        protected override void OnStartSearch()
        {
            try
            {
                var searchText = SearchQuery.SearchString;
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    // Perform search in chat control
                    var results = _chatControl.SearchMessages(searchText);
                    
                    // Report search results
                    SearchCallback.ReportComplete(this, (uint)results.Count);
                    
                    // Highlight first result if found
                    if (results.Count > 0)
                    {
                        _chatControl.HighlightSearchResult(results[0]);
                    }
                }
                else
                {
                    SearchCallback.ReportComplete(this, 0);
                }
            }
            catch (Exception ex)
            {
                SearchCallback.ReportComplete(this, 0);
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
        }

        protected override void OnStopSearch()
        {
            _chatControl?.ClearSearchHighlight();
        }
    }
}