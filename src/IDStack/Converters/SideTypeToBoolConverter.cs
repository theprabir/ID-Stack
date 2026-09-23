using System;
using System.Globalization;
using System.Windows.Data;
using IDStack.Core.Models.Template;

namespace IDStack.Converters
{
    /// <summary>
    /// Converts a SideType to bool for radio-button binding (ConverterParameter = "Front"/"Back").
    /// </summary>
    public class SideTypeToBoolConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SideType sideType && parameter is string expected)
            {
                return sideType.ToString() == expected;
            }
            return false;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string expected)
            {
                return expected == "Front" ? SideType.Front : SideType.Back;
            }
            return Binding.DoNothing;
        }
    }
}
