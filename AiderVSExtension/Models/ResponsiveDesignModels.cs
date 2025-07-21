using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Responsive UI settings
    /// </summary>
    public class ResponsiveUISettings
    {
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public double DpiScaling { get; set; }
        public bool IsSmallScreen { get; set; }
        public bool IsMediumScreen { get; set; }
        public bool IsLargeScreen { get; set; }
        public double FontSize { get; set; }
        public double IconSize { get; set; }
        public double Spacing { get; set; }
        public bool PreferCompactLayout { get; set; }
        public bool ShowDetailedViews { get; set; }
        public bool UseCollapsibleSections { get; set; }
    }

    /// <summary>
    /// Responsive configuration for UI elements
    /// </summary>
    public class ResponsiveConfiguration
    {
        /// <summary>
        /// Size constraints for different breakpoints
        /// </summary>
        public Dictionary<string, SizeConstraints> SizeConstraints { get; set; } = new Dictionary<string, SizeConstraints>();

        /// <summary>
        /// Visibility settings for different breakpoints
        /// </summary>
        public Dictionary<string, Visibility> Visibility { get; set; } = new Dictionary<string, Visibility>();

        /// <summary>
        /// Layout properties for different breakpoints
        /// </summary>
        public Dictionary<string, LayoutProperties> LayoutProperties { get; set; } = new Dictionary<string, LayoutProperties>();

        /// <summary>
        /// Responsive typography configuration
        /// </summary>
        public ResponsiveTypography Typography { get; set; }

        /// <summary>
        /// Responsive spacing configuration
        /// </summary>
        public ResponsiveSpacing Spacing { get; set; }

        /// <summary>
        /// Custom properties for different breakpoints
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> CustomProperties { get; set; } = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Whether to animate transitions between breakpoints
        /// </summary>
        public bool AnimateTransitions { get; set; } = true;

        /// <summary>
        /// Transition duration in milliseconds
        /// </summary>
        public int TransitionDuration { get; set; } = 300;
    }

    /// <summary>
    /// Size constraints for responsive elements
    /// </summary>
    public class SizeConstraints
    {
        /// <summary>
        /// Minimum width
        /// </summary>
        public double MinWidth { get; set; }

        /// <summary>
        /// Maximum width
        /// </summary>
        public double MaxWidth { get; set; }

        /// <summary>
        /// Minimum height
        /// </summary>
        public double MinHeight { get; set; }

        /// <summary>
        /// Maximum height
        /// </summary>
        public double MaxHeight { get; set; }

        /// <summary>
        /// Preferred width
        /// </summary>
        public double PreferredWidth { get; set; }

        /// <summary>
        /// Preferred height
        /// </summary>
        public double PreferredHeight { get; set; }

        /// <summary>
        /// Aspect ratio to maintain
        /// </summary>
        public double AspectRatio { get; set; }
    }

    /// <summary>
    /// Layout properties for responsive elements
    /// </summary>
    public class LayoutProperties
    {
        /// <summary>
        /// Horizontal alignment
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Vertical alignment
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Margin
        /// </summary>
        public Thickness Margin { get; set; }

        /// <summary>
        /// Padding (for supported elements)
        /// </summary>
        public Thickness Padding { get; set; }

        /// <summary>
        /// Grid row (for Grid children)
        /// </summary>
        public int GridRow { get; set; }

        /// <summary>
        /// Grid column (for Grid children)
        /// </summary>
        public int GridColumn { get; set; }

        /// <summary>
        /// Grid row span (for Grid children)
        /// </summary>
        public int GridRowSpan { get; set; }

        /// <summary>
        /// Grid column span (for Grid children)
        /// </summary>
        public int GridColumnSpan { get; set; }
    }

    /// <summary>
    /// Responsive breakpoint definition
    /// </summary>
    public class ResponsiveBreakpoint
    {
        /// <summary>
        /// Breakpoint name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Minimum width for this breakpoint
        /// </summary>
        public int MinWidth { get; set; }

        /// <summary>
        /// Maximum width for this breakpoint
        /// </summary>
        public int MaxWidth { get; set; }

        /// <summary>
        /// Breakpoint priority (higher = takes precedence)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Description of the breakpoint
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this breakpoint is for mobile devices
        /// </summary>
        public bool IsMobile { get; set; }

        /// <summary>
        /// Whether this breakpoint is for tablet devices
        /// </summary>
        public bool IsTablet { get; set; }

        /// <summary>
        /// Whether this breakpoint is for desktop devices
        /// </summary>
        public bool IsDesktop { get; set; }

        /// <summary>
        /// Custom properties for this breakpoint
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Responsive layout configuration
    /// </summary>
    public class ResponsiveLayout
    {
        /// <summary>
        /// Default layout configuration
        /// </summary>
        public LayoutConfiguration DefaultLayout { get; set; } = new LayoutConfiguration();

        /// <summary>
        /// Layout configurations for different breakpoints
        /// </summary>
        public Dictionary<string, LayoutConfiguration> BreakpointLayouts { get; set; } = new Dictionary<string, LayoutConfiguration>();

        /// <summary>
        /// Whether to animate layout changes
        /// </summary>
        public bool AnimateChanges { get; set; } = true;

        /// <summary>
        /// Layout change animation duration
        /// </summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(300);
    }

    /// <summary>
    /// Layout configuration for a container
    /// </summary>
    public class LayoutConfiguration
    {
        /// <summary>
        /// Container orientation (for supported containers)
        /// </summary>
        public Orientation Orientation { get; set; }

        /// <summary>
        /// Container background
        /// </summary>
        public Brush Background { get; set; }

        /// <summary>
        /// Container margin
        /// </summary>
        public Thickness Margin { get; set; }

        /// <summary>
        /// Container padding
        /// </summary>
        public Thickness Padding { get; set; }

        /// <summary>
        /// Child element arrangements
        /// </summary>
        public List<ChildArrangement> ChildArrangements { get; set; } = new List<ChildArrangement>();

        /// <summary>
        /// Whether to use flex layout
        /// </summary>
        public bool UseFlex { get; set; } = false;

        /// <summary>
        /// Flex direction
        /// </summary>
        public FlexDirection FlexDirection { get; set; } = FlexDirection.Row;

        /// <summary>
        /// Justify content
        /// </summary>
        public JustifyContent JustifyContent { get; set; } = JustifyContent.Start;

        /// <summary>
        /// Align items
        /// </summary>
        public AlignItems AlignItems { get; set; } = AlignItems.Stretch;
    }

    /// <summary>
    /// Child element arrangement
    /// </summary>
    public class ChildArrangement
    {
        /// <summary>
        /// Child visibility
        /// </summary>
        public Visibility
		 Visibility { get; set; }

        /// <summary>
        /// Child horizontal alignment
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Child vertical alignment
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Child margin
        /// </summary>
        public Thickness Margin { get; set; }

        /// <summary>
        /// Child flex grow
        /// </summary>
        public double FlexGrow { get; set; }

        /// <summary>
        /// Child flex shrink
        /// </summary>
        public double FlexShrink { get; set; }

        /// <summary>
        /// Child flex basis
        /// </summary>
        public double FlexBasis { get; set; }

        /// <summary>
        /// Child order
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Responsive grid configuration
    /// </summary>
    public class ResponsiveGridConfiguration
    {
        /// <summary>
        /// Default grid configuration
        /// </summary>
        public GridConfiguration DefaultConfiguration { get; set; } = new GridConfiguration();

        /// <summary>
        /// Grid configurations for different breakpoints
        /// </summary>
        public Dictionary<string, GridConfiguration> BreakpointConfigurations { get; set; } = new Dictionary<string, GridConfiguration>();

        /// <summary>
        /// Whether to animate grid changes
        /// </summary>
        public bool AnimateChanges { get; set; } = true;

        /// <summary>
        /// Grid change animation duration
        /// </summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(300);
    }

    /// <summary>
    /// Grid configuration
    /// </summary>
    public class GridConfiguration
    {
        /// <summary>
        /// Number of rows
        /// </summary>
        public int Rows { get; set; } = 1;

        /// <summary>
        /// Number of columns
        /// </summary>
        public int Columns { get; set; } = 1;

        /// <summary>
        /// Row heights
        /// </summary>
        public List<GridLength> RowHeights { get; set; } = new List<GridLength>();

        /// <summary>
        /// Column widths
        /// </summary>
        public List<GridLength> ColumnWidths { get; set; } = new List<GridLength>();

        /// <summary>
        /// Row spacing
        /// </summary>
        public double RowSpacing { get; set; } = 0;

        /// <summary>
        /// Column spacing
        /// </summary>
        public double ColumnSpacing { get; set; } = 0;
    }

    /// <summary>
    /// Responsive typography configuration
    /// </summary>
    public class ResponsiveTypography
    {
        /// <summary>
        /// Default typography
        /// </summary>
        public TypographyConfiguration DefaultTypography { get; set; } = new TypographyConfiguration();

        /// <summary>
        /// Typography configurations for different breakpoints
        /// </summary>
        public Dictionary<string, TypographyConfiguration> BreakpointTypography { get; set; } = new Dictionary<string, TypographyConfiguration>();

        /// <summary>
        /// Whether to scale typography automatically
        /// </summary>
        public bool AutoScale { get; set; } = true;

        /// <summary>
        /// Minimum font size
        /// </summary>
        public double MinFontSize { get; set; } = 8;

        /// <summary>
        /// Maximum font size
        /// </summary>
        public double MaxFontSize { get; set; } = 72;
    }

    /// <summary>
    /// Typography configuration
    /// </summary>
    public class TypographyConfiguration
    {
        /// <summary>
        /// Font size
        /// </summary>
        public double FontSize { get; set; } = 12;

        /// <summary>
        /// Font family
        /// </summary>
        public string FontFamily { get; set; } = "Segoe UI";

        /// <summary>
        /// Font weight
        /// </summary>
        public FontWeight FontWeight { get; set; } = FontWeights.Normal;

        /// <summary>
        /// Font style
        /// </summary>
        public FontStyle FontStyle { get; set; } = FontStyles.Normal;

        /// <summary>
        /// Line height
        /// </summary>
        public double LineHeight { get; set; } = 1.2;

        /// <summary>
        /// Letter spacing
        /// </summary>
        public double LetterSpacing { get; set; } = 0;

        /// <summary>
        /// Text decoration
        /// </summary>
        public TextDecorationCollection TextDecorations { get; set; }
    }

    /// <summary>
    /// Responsive spacing configuration
    /// </summary>
    public class ResponsiveSpacing
    {
        /// <summary>
        /// Default spacing
        /// </summary>
        public SpacingConfiguration DefaultSpacing { get; set; } = new SpacingConfiguration();

        /// <summary>
        /// Spacing configurations for different breakpoints
        /// </summary>
        public Dictionary<string, SpacingConfiguration> BreakpointSpacing { get; set; } = new Dictionary<string, SpacingConfiguration>();

        /// <summary>
        /// Base spacing unit
        /// </summary>
        public double BaseUnit { get; set; } = 8;

        /// <summary>
        /// Spacing scale factors for different breakpoints
        /// </summary>
        public Dictionary<string, double> ScaleFactors { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Spacing configuration
    /// </summary>
    public class SpacingConfiguration
    {
        /// <summary>
        /// Margin
        /// </summary>
        public Thickness Margin { get; set; } = new Thickness(0);

        /// <summary>
        /// Padding
        /// </summary>
        public Thickness Padding { get; set; } = new Thickness(0);

        /// <summary>
        /// Additional spacing properties
        /// </summary>
        public Dictionary<string, double> CustomSpacing { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Responsive state changed event arguments
    /// </summary>
    public class ResponsiveStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Previous responsive settings
        /// </summary>
        public ResponsiveUISettings PreviousSettings { get; set; }

        /// <summary>
        /// New responsive settings
        /// </summary>
        public ResponsiveUISettings NewSettings { get; set; }

        /// <summary>
        /// Previous active breakpoint
        /// </summary>
        public string PreviousBreakpoint { get; set; }

        /// <summary>
        /// New active breakpoint
        /// </summary>
        public string NewBreakpoint { get; set; }

        /// <summary>
        /// Change timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether the change was triggered by a window resize
        /// </summary>
        public bool TriggeredByResize { get; set; }
    }

    /// <summary>
    /// Flex direction enumeration
    /// </summary>
    public enum FlexDirection
    {
        Row,
        RowReverse,
        Column,
        ColumnReverse
    }

    /// <summary>
    /// Justify content enumeration
    /// </summary>
    public enum JustifyContent
    {
        Start,
        End,
        Center,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly
    }

    /// <summary>
    /// Align items enumeration
    /// </summary>
    public enum AlignItems
    {
        Start,
        End,
        Center,
        Stretch,
        Baseline
    }
}