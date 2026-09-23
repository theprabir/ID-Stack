using System;
using System.Globalization;
using System.Windows.Data;
using IDCardSoftware.Core.Constants;

namespace IDCardSoftware.Converters
{
    /// <summary>
    /// Converts millimeters to device-independent pixels using the default screen DPI.
    /// </summary>
    public class MillimeterToPixelConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double millimeters)
            {
                return millimeters * UnitConstants.ScreenDpi / UnitConstants.MillimetersPerInch;
            }

            return 0.0;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pixels)
            {
                return pixels * UnitConstants.MillimetersPerInch / UnitConstants.ScreenDpi;
            }

            return 0.0;
        }
    }
}
