using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AiderVSExtension.UI.Converters
{
    /// <summary>
    /// Converter that converts boolean values to Visibility values
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            
            return false;
        }
    }
}