using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IDStack.Converters
{
    /// <summary>
    /// Converts a hex color string (e.g. "#FF0080") to a SolidColorBrush.
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        /// <summary>Brush returned when conversion fails.</summary>
        public Brush FallbackBrush { get; set; } = Brushes.Transparent;

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var hex = value as string;
            if (string.IsNullOrWhiteSpace(hex))
            {
                return FallbackBrush;
            }

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
            catch (FormatException)
            {
                return FallbackBrush;
            }
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var brush = value as SolidColorBrush;
            if (brush == null)
            {
                return null;
            }

            var color = brush.Color;
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
