using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Visual Studio theme information
    /// </summary>
    public class VSTheme
    {
        /// <summary>
        /// Theme name (e.g., "Dark", "Light", "Blue")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Theme ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Whether this is a dark theme
        /// </summary>
        public bool IsDark { get; set; }

        /// <summary>
        /// Whether this is a light theme
        /// </summary>
        public bool IsLight => !IsDark;

        /// <summary>
        /// Theme colors dictionary
        /// </summary>
        public Dictionary<string, Color> Colors { get; set; } = new Dictionary<string, Color>();

        /// <summary>
        /// Theme brushes dictionary
        /// </summary>
        public Dictionary<string, Brush> Brushes { get; set; } = new Dictionary<string, Brush>();

        /// <summary>
        /// Theme contrast ratio
        /// </summary>
        public double ContrastRatio { get; set; }

        /// <summary>
        /// Theme creation date
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Syntax highlighting theme
    /// </summary>
    public class SyntaxHighlightingTheme
    {
        /// <summary>
        /// Theme name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Background color
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// Foreground/text color
        /// </summary>
        public Color ForegroundColor { get; set; }

        /// <summary>
        /// Comment color
        /// </summary>
        public Color CommentColor { get; set; }

        /// <summary>
        /// Keyword color
        /// </summary>
        public Color KeywordColor { get; set; }

        /// <summary>
        /// String literal color
        /// </summary>
        public Color StringColor { get; set; }

        /// <summary>
        /// Number color
        /// </summary>
        public Color NumberColor { get; set; }

        /// <summary>
        /// Operator color
        /// </summary>
        public Color OperatorColor { get; set; }

        /// <summary>
        /// Type/class name color
        /// </summary>
        public Color TypeColor { get; set; }

        /// <summary>
        /// Method/function name color
        /// </summary>
        public Color MethodColor { get; set; }

        /// <summary>
        /// Variable name color
        /// </summary>
        public Color VariableColor { get; set; }

        /// <summary>
        /// Error highlight color
        /// </summary>
        public Color ErrorColor { get; set; }

        /// <summary>
        /// Warning highlight color
        /// </summary>
        public Color WarningColor { get; set; }

        /// <summary>
        /// Line number color
        /// </summary>
        public Color LineNumberColor { get; set; }

        /// <summary>
        /// Selection background color
        /// </summary>
        public Color SelectionBackgroundColor { get; set; }

        /// <summary>
        /// Current line highlight color
        /// </summary>
        public Color CurrentLineColor { get; set; }

        /// <summary>
        /// Additional language-specific colors
        /// </summary>
        public Dictionary<string, Color> LanguageColors { get; set; } = new Dictionary<string, Color>();

        /// <summary>
        /// Font weights for different syntax elements
        /// </summary>
        public Dictionary<string, FontWeight> FontWeights { get; set; } = new Dictionary<string, FontWeight>();

        /// <summary>
        /// Font styles for different syntax elements
        /// </summary>
        public Dictionary<string, FontStyle> FontStyles { get; set; } = new Dictionary<string, FontStyle>();
    }

    /// <summary>
    /// Custom theme profile
    /// </summary>
    public class ThemeProfile
    {
        /// <summary>
        /// Profile ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Profile name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Profile description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this is a built-in profile
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Base VS theme to inherit from
        /// </summary>
        public string BaseTheme { get; set; }

        /// <summary>
        /// Custom color overrides
        /// </summary>
        public Dictionary<string, Color> ColorOverrides { get; set; } = new Dictionary<string, Color>();

        /// <summary>
        /// Custom brush overrides
        /// </summary>
        public Dictionary<string, string> BrushOverrides { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Syntax highlighting customizations
        /// </summary>
        public SyntaxHighlightingTheme SyntaxTheme { get; set; }

        /// <summary>
        /// UI element styles
        /// </summary>
        public Dictionary<string, ThemeStyle> ElementStyles { get; set; } = new Dictionary<string, ThemeStyle>();

        /// <summary>
        /// Profile version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Profile author
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the profile supports high contrast
        /// </summary>
        public bool SupportsHighContrast { get; set; }

        /// <summary>
        /// High contrast variant
        /// </summary>
        public HighContrastTheme HighContrastVariant { get; set; }
    }

    /// <summary>
    /// Theme style for UI elements
    /// </summary>
    public class ThemeStyle
    {
        /// <summary>
        /// Background color
        /// </summary>
        public Color? BackgroundColor { get; set; }

        /// <summary>
        /// Foreground color
        /// </summary>
        public Color? ForegroundColor { get; set; }

        /// <summary>
        /// Border color
        /// </summary>
        public Color? BorderColor { get; set; }

        /// <summary>
        /// Border thickness
        /// </summary>
        public Thickness? BorderThickness { get; set; }

        /// <summary>
        /// Corner radius
        /// </summary>
        public CornerRadius? CornerRadius { get; set; }

        /// <summary>
        /// Padding
        /// </summary>
        public Thickness? Padding { get; set; }

        /// <summary>
        /// Margin
        /// </summary>
        public Thickness? Margin { get; set; }

        /// <summary>
        /// Font family
        /// </summary>
        public string FontFamily { get; set; }

        /// <summary>
        /// Font size
        /// </summary>
        public double? FontSize { get; set; }

        /// <summary>
        /// Font weight
        /// </summary>
        public FontWeight? FontWeight { get; set; }

        /// <summary>
        /// Font style
        /// </summary>
        public FontStyle? FontStyle { get; set; }

        /// <summary>
        /// Additional properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// High contrast theme
    /// </summary>
    public class HighContrastTheme
    {
        /// <summary>
        /// Theme name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether this is a high contrast black theme
        /// </summary>
        public bool IsBlackTheme { get; set; }

        /// <summary>
        /// Whether this is a high contrast white theme
        /// </summary>
        public bool IsWhiteTheme { get; set; }

        /// <summary>
        /// High contrast colors
        /// </summary>
        public Dictionary<string, Color> Colors { get; set; } = new Dictionary<string, Color>();

        /// <summary>
        /// Minimum contrast ratio requirements
        /// </summary>
        public double MinimumContrastRatio { get; set; } = 7.0; // WCAG AAA

        /// <summary>
        /// Alternative text colors for better contrast
        /// </summary>
        public Dictionary<string, Color> AlternativeTextColors { get; set; } = new Dictionary<string, Color>();
    }

    /// <summary>
    /// Accessibility color scheme
    /// </summary>
    public class AccessibilityColorScheme
    {
        /// <summary>
        /// Primary text color
        /// </summary>
        public Color PrimaryTextColor { get; set; }

        /// <summary>
        /// Secondary text color
        /// </summary>
        public Color SecondaryTextColor { get; set; }

        /// <summary>
        /// Disabled text color
        /// </summary>
        public Color DisabledTextColor { get; set; }

        /// <summary>
        /// Link color
        /// </summary>
        public Color LinkColor { get; set; }

        /// <summary>
        /// Visited link color
        /// </summary>
        public Color VisitedLinkColor { get; set; }

        /// <summary>
        /// Error color
        /// </summary>
        public Color ErrorColor { get; set; }

        /// <summary>
        /// Warning color
        /// </summary>
        public Color WarningColor { get; set; }

        /// <summary>
        /// Success color
        /// </summary>
        public Color SuccessColor { get; set; }

        /// <summary>
        /// Info color
        /// </summary>
        public Color InfoColor { get; set; }

        /// <summary>
        /// Focus indicator color
        /// </summary>
        public Color FocusColor { get; set; }

        /// <summary>
        /// Selection color
        /// </summary>
        public Color SelectionColor { get; set; }

        /// <summary>
        /// Contrast ratios for color combinations
        /// </summary>
        public Dictionary<string, double> ContrastRatios { get; set; } = new Dictionary<string, double>();
    }


    /// <summary>
    /// Theme validation result
    /// </summary>
    public class ThemeValidationResult
    {
        /// <summary>
        /// Whether the theme is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Accessibility compliance level
        /// </summary>
        public AccessibilityLevel AccessibilityLevel { get; set; }

        /// <summary>
        /// Contrast ratio analysis
        /// </summary>
        public Dictionary<string, double> ContrastAnalysis { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// UI responsiveness configuration
    /// </summary>
    public class ResponsiveUIConfig
    {
        /// <summary>
        /// Breakpoints for different screen sizes
        /// </summary>
        public Dictionary<string, double> Breakpoints { get; set; } = new Dictionary<string, double>
        {
            ["XSmall"] = 480,
            ["Small"] = 768,
            ["Medium"] = 1024,
            ["Large"] = 1440,
            ["XLarge"] = 1920
        };

        /// <summary>
        /// Layout configurations for different screen sizes
        /// </summary>
        public Dictionary<string, LayoutConfig> LayoutConfigs { get; set; } = new Dictionary<string, LayoutConfig>();

        /// <summary>
        /// Font size scaling factors
        /// </summary>
        public Dictionary<string, double> FontScaling { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Spacing scaling factors
        /// </summary>
        public Dictionary<string, double> SpacingScaling { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Whether to enable adaptive layouts
        /// </summary>
        public bool EnableAdaptiveLayouts { get; set; } = true;

        /// <summary>
        /// Minimum accessible target size (for touch interfaces)
        /// </summary>
        public double MinimumTargetSize { get; set; } = 44; // 44x44 pixels recommended
    }

    /// <summary>
    /// Layout configuration
    /// </summary>
    public class LayoutConfig
    {
        /// <summary>
        /// Panel orientation
        /// </summary>
        public Orientation Orientation { get; set; }

        /// <summary>
        /// Column/row definitions
        /// </summary>
        public List<string> GridDefinitions { get; set; } = new List<string>();

        /// <summary>
        /// Element visibility settings
        /// </summary>
        public Dictionary<string, Visibility> ElementVisibility { get; set; } = new Dictionary<string, Visibility>();

        /// <summary>
        /// Element sizing
        /// </summary>
        public Dictionary<string, Size> ElementSizes { get; set; } = new Dictionary<string, Size>();

        /// <summary>
        /// Layout-specific properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Accessibility compliance levels
    /// </summary>
    public enum AccessibilityLevel
    {
        /// <summary>
        /// Non-compliant
        /// </summary>
        None,

        /// <summary>
        /// WCAG 2.1 A compliance
        /// </summary>
        WCAG_A,

        /// <summary>
        /// WCAG 2.1 AA compliance
        /// </summary>
        WCAG_AA,

        /// <summary>
        /// WCAG 2.1 AAA compliance
        /// </summary>
        WCAG_AAA
    }

    /// <summary>
    /// Theme categories
    /// </summary>
    public enum ThemeCategory
    {
        /// <summary>
        /// Built-in Visual Studio themes
        /// </summary>
        BuiltIn,

        /// <summary>
        /// User-created custom themes
        /// </summary>
        Custom,

        /// <summary>
        /// Community-shared themes
        /// </summary>
        Community,

        /// <summary>
        /// High contrast accessibility themes
        /// </summary>
        HighContrast,

        /// <summary>
        /// Experimental themes
        /// </summary>
        Experimental
    }
}