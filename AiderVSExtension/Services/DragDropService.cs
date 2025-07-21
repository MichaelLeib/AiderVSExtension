using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for handling drag-and-drop operations
    /// </summary>
    public class DragDropService : IDragDropService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IFileContextService _fileContextService;
        private readonly INotificationService _notificationService;
        private readonly Dictionary<FrameworkElement, DragDropHandler> _handlers = new Dictionary<FrameworkElement, DragDropHandler>();
        private bool _disposed = false;

        public event EventHandler<DragDropEventArgs> FilesDropped;
        public event EventHandler<DragDropEventArgs> TextDropped;
        public event EventHandler<DragDropEventArgs> DragEnter;
        public event EventHandler<DragDropEventArgs> DragLeave;

        public DragDropService(IErrorHandler errorHandler, IFileContextService fileContextService, INotificationService notificationService)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _fileContextService = fileContextService ?? throw new ArgumentNullException(nameof(fileContextService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// Enables drag-and-drop on a UI element
        /// </summary>
        /// <param name="element">Element to enable drag-drop on</param>
        /// <param name="options">Drag-drop options</param>
        public void EnableDragDrop(FrameworkElement element, DragDropOptions options = null)
        {
            try
            {
                if (element == null)
                    return;

                options = options ?? new DragDropOptions();
                
                var handler = new DragDropHandler(this, options);
                _handlers[element] = handler;

                // Enable drop
                element.AllowDrop = true;

                // Subscribe to events
                element.DragEnter += handler.OnDragEnter;
                element.DragOver += handler.OnDragOver;
                element.DragLeave += handler.OnDragLeave;
                element.Drop += handler.OnDrop;

                // Enable drag if specified
                if (options.AllowDrag)
                {
                    element.MouseLeftButtonDown += handler.OnMouseLeftButtonDown;
                    element.MouseMove += handler.OnMouseMove;
                    element.MouseLeftButtonUp += handler.OnMouseLeftButtonUp;
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "DragDropService.EnableDragDrop");
            }
        }

        /// <summary>
        /// Disables drag-and-drop on a UI element
        /// </summary>
        /// <param name="element">Element to disable drag-drop on</param>
        public void DisableDragDrop(FrameworkElement element)
        {
            try
            {
                if (element == null || !_handlers.ContainsKey(element))
                    return;

                var handler = _handlers[element];
                
                // Disable drop
                element.AllowDrop = false;

                // Unsubscribe from events
                element.DragEnter -= handler.OnDragEnter;
                element.DragOver -= handler.OnDragOver;
                element.DragLeave -= handler.OnDragLeave;
                element.Drop -= handler.OnDrop;

                // Disable drag
                element.MouseLeftButtonDown -= handler.OnMouseLeftButtonDown;
                element.MouseMove -= handler.OnMouseMove;
                element.MouseLeftButtonUp -= handler.OnMouseLeftButtonUp;

                _handlers.Remove(element);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "DragDropService.DisableDragDrop");
            }
        }

        /// <summary>
        /// Processes dropped files
        /// </summary>
        /// <param name="filePaths">File paths that were dropped</param>
        /// <param name="element">Target element</param>
        /// <returns>Processing result</returns>
        public async Task<DragDropResult> ProcessDroppedFilesAsync(IEnumerable<string> filePaths, FrameworkElement element)
        {
            try
            {
                var result = new DragDropResult();
                var validFiles = new List<string>();
                var invalidFiles = new List<string>();

                // Validate files
                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        validFiles.Add(filePath);
                    }
                    else if (Directory.Exists(filePath))
                    {
                        // Add directory files
                        var directoryFiles = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories)
                            .Where(f => IsValidFileType(f))
                            .ToList();
                        validFiles.AddRange(directoryFiles);
                    }
                    else
                    {
                        invalidFiles.Add(filePath);
                    }
                }

                // Process valid files
                var processedFiles = new List<FileReference>();
                foreach (var filePath in validFiles)
                {
                    try
                    {
                        var fileReference = await _fileContextService.GetFileReferenceAsync(filePath);
                        if (fileReference != null)
                        {
                            processedFiles.Add(fileReference);
                        }
                    }
                    catch (Exception ex)
                    {
                        invalidFiles.Add(filePath);
                        await _errorHandler.HandleExceptionAsync(ex, $"DragDropService.ProcessDroppedFilesAsync - Processing {filePath}");
                    }
                }

                result.ProcessedFiles = processedFiles;
                result.InvalidFiles = invalidFiles;
                result.Success = processedFiles.Any();

                // Fire event
                FilesDropped?.Invoke(this, new DragDropEventArgs
                {
                    Files = processedFiles,
                    Element = element,
                    DropPoint = new Point(0, 0) // Would be set by handler
                });

                // Show notification
                if (result.Success)
                {
                    var message = $"Added {processedFiles.Count} file(s) to chat";
                    if (invalidFiles.Any())
                    {
                        message += $" ({invalidFiles.Count} invalid file(s) skipped)";
                    }
                    
                    await _notificationService.ShowSuccessAsync(message);
                }
                else if (invalidFiles.Any())
                {
                    await _notificationService.ShowWarningAsync($"Could not process {invalidFiles.Count} file(s)");
                }

                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "DragDropService.ProcessDroppedFilesAsync");
                return new DragDropResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Processes dropped text
        /// </summary>
        /// <param name="text">Text that was dropped</param>
        /// <param name="element">Target element</param>
        /// <returns>Processing result</returns>
        public async Task<DragDropResult> ProcessDroppedTextAsync(string text, FrameworkElement element)
        {
            try
            {
                var result = new DragDropResult
                {
                    Text = text,
                    Success = !string.IsNullOrEmpty(text)
                };

                if (result.Success)
                {
                    // Fire event
                    TextDropped?.Invoke(this, new DragDropEventArgs
                    {
                        Text = text,
                        Element = element,
                        DropPoint = new Point(0, 0) // Would be set by handler
                    });

                    await _notificationService.ShowSuccessAsync("Text added to chat");
                }

                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "DragDropService.ProcessDroppedTextAsync");
                return new DragDropResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Gets drag effects for data
        /// </summary>
        /// <param name="data">Drag data</param>
        /// <param name="options">Drag-drop options</param>
        /// <returns>Drag effects</returns>
        public DragDropEffects GetDragEffects(IDataObject data, DragDropOptions options)
        {
            try
            {
                if (data == null)
                    return DragDropEffects.None;

                var effects = DragDropEffects.None;

                // Check for files
                if (options.AcceptFiles && data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Any())
                    {
                        // Check if any files are valid
                        var hasValidFiles = files.Any(f => IsValidFileType(f) && (File.Exists(f) || Directory.Exists(f)));
                        if (hasValidFiles)
                        {
                            effects |= DragDropEffects.Copy;
                        }
                    }
                }

                // Check for text
                if (options.AcceptText && data.GetDataPresent(DataFormats.Text))
                {
                    var text = data.GetData(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        effects |= DragDropEffects.Copy;
                    }
                }

                // Check for VS-specific formats
                if (options.AcceptVSItems)
                {
                    if (data.GetDataPresent("CF_VSSTGPROJECTITEMS") || 
                        data.GetDataPresent("CF_VSREFPROJECTITEMS"))
                    {
                        effects |= DragDropEffects.Copy;
                    }
                }

                return effects;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "DragDropService.GetDragEffects");
                return DragDropEffects.None;
            }
        }

        /// <summary>
        /// Checks if a file type is valid for processing
        /// </summary>
        /// <param name="filePath">File path to check</param>
        /// <returns>True if valid</returns>
        public bool IsValidFileType(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                // Common source code extensions
                var validExtensions = new HashSet<string>
                {
                    ".cs", ".vb", ".fs", ".cpp", ".c", ".h", ".hpp", ".cxx", ".cc",
                    ".js", ".ts", ".jsx", ".tsx", ".vue", ".svelte",
                    ".py", ".java", ".kt", ".scala", ".go", ".rs", ".swift",
                    ".php", ".rb", ".pl", ".sh", ".ps1", ".bat", ".cmd",
                    ".html", ".htm", ".xml", ".xaml", ".json", ".yaml", ".yml",
                    ".css", ".scss", ".sass", ".less", ".sql", ".md", ".txt",
                    ".config", ".ini", ".toml", ".properties", ".gitignore",
                    ".dockerfile", ".dockerignore", ".editorconfig"
                };

                return validExtensions.Contains(extension);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "DragDropService.IsValidFileType");
                return false;
            }
        }

        /// <summary>
        /// Creates drag data for an element
        /// </summary>
        /// <param name="element">Element to create drag data for</param>
        /// <returns>Drag data</returns>
        public DataObject CreateDragData(FrameworkElement element)
        {
            try
            {
                var dataObject = new DataObject();
                
                // Add element reference
                dataObject.SetData("AiderVSExtension.Element", element);
                
                // Add text if element has text content
                if (element is TextBlock textBlock)
                {
                    dataObject.SetData(DataFormats.Text, textBlock.Text);
                }
                else if (element is TextBox textBox)
                {
                    dataObject.SetData(DataFormats.Text, textBox.Text);
                }
                else if (element is ContentControl contentControl)
                {
                    dataObject.SetData(DataFormats.Text, contentControl.Content?.ToString());
                }

                return dataObject;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "DragDropService.CreateDragData");
                return new DataObject();
            }
        }

        internal void FireDragEnter(DragDropEventArgs args)
        {
            DragEnter?.Invoke(this, args);
        }

        internal void FireDragLeave(DragDropEventArgs args)
        {
            DragLeave?.Invoke(this, args);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var kvp in _handlers.ToList())
                {
                    DisableDragDrop(kvp.Key);
                }
                _handlers.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Internal drag-drop handler
    /// </summary>
    internal class DragDropHandler
    {
        private readonly DragDropService _service;
        private readonly DragDropOptions _options;
        private bool _isDragging = false;
        private Point _dragStartPoint;

        public DragDropHandler(DragDropService service, DragDropOptions options)
        {
            _service = service;
            _options = options;
        }

        public async void OnDragEnter(object sender, DragEventArgs e)
        {
            try
            {
                var element = sender as FrameworkElement;
                var effects = _service.GetDragEffects(e.Data, _options);
                
                e.Effects = effects;
                
                _service.FireDragEnter(new DragDropEventArgs
                {
                    Element = element,
                    Data = e.Data,
                    Effects = effects,
                    DropPoint = e.GetPosition(element)
                });
            }
            catch (Exception ex)
            {
                await _service._errorHandler.HandleExceptionAsync(ex, "DragDropHandler.OnDragEnter");
            }
        }

        public void OnDragOver(object sender, DragEventArgs e)
        {
            try
            {
                var effects = _service.GetDragEffects(e.Data, _options);
                e.Effects = effects;
            }
            catch (Exception ex)
            {
                _service._errorHandler?.HandleExceptionAsync(ex, "DragDropHandler.OnDragOver");
            }
        }

        public async void OnDragLeave(object sender, DragEventArgs e)
        {
            try
            {
                var element = sender as FrameworkElement;
                
                _service.FireDragLeave(new DragDropEventArgs
                {
                    Element = element,
                    Data = e.Data,
                    DropPoint = e.GetPosition(element)
                });
            }
            catch (Exception ex)
            {
                await _service._errorHandler.HandleExceptionAsync(ex, "DragDropHandler.OnDragLeave");
            }
        }

        public async void OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                var element = sender as FrameworkElement;
                var dropPoint = e.GetPosition(element);

                // Handle files
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Any())
                    {
                        await _service.ProcessDroppedFilesAsync(files, element);
                    }
                }
                // Handle text
                else if (e.Data.GetDataPresent(DataFormats.Text))
                {
                    var text = e.Data.GetData(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(text))
                    {
                        await _service.ProcessDroppedTextAsync(text, element);
                    }
                }
            }
            catch (Exception ex)
            {
                await _service._errorHandler.HandleExceptionAsync(ex, "DragDropHandler.OnDrop");
            }
        }

        public void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (_options.AllowDrag)
                {
                    _dragStartPoint = e.GetPosition(sender as FrameworkElement);
                    _isDragging = true;
                }
            }
            catch (Exception ex)
            {
                _service._errorHandler?.HandleExceptionAsync(ex, "DragDropHandler.OnMouseLeftButtonDown");
            }
        }

        public void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (_isDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    var element = sender as FrameworkElement;
                    var currentPoint = e.GetPosition(element);
                    var diff = _dragStartPoint - currentPoint;

                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        var dragData = _service.CreateDragData(element);
                        DragDrop.DoDragDrop(element, dragData, DragDropEffects.Copy);
                        _isDragging = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _service._errorHandler?.HandleExceptionAsync(ex, "DragDropHandler.OnMouseMove");
            }
        }

        public void OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
        }
    }
}