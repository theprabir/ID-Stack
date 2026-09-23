using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IDCardSoftware.Converters
{
    /// <summary>
    /// Converts a bool to Visibility, with an optional invert behavior.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>When true, false maps to Visible and true to Collapsed.</summary>
        public bool Invert { get; set; }

        /// <summary>Visibility used when the value is not a bool.</summary>
        public Visibility FallbackValue { get; set; } = Visibility.Collapsed;

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = value as bool? ?? false;
            if (Invert)
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                var visible = visibility == Visibility.Visible;
                return Invert ? !visible : visible;
            }

            return false;
        }
    }
}
