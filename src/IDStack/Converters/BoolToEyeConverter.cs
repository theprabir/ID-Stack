using System;
using System.Globalization;
using System.Windows.Data;

namespace IDStack.Converters
{
    /// <summary>
    /// Converts element visibility into an eye glyph for layer rows.
    /// </summary>
    public class BoolToEyeConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool visible && visible ? "👁" : "–";
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
