using System;

namespace IDStack.Core.Constants
{
    /// <summary>
    /// Unit conversion constants shared across modules.
    /// </summary>
    public static class UnitConstants
    {
        /// <summary>Default screen DPI used for mm-to-pixel conversions.</summary>
        public const double ScreenDpi = 96.0;

        /// <summary>Millimeters per inch.</summary>
        public const double MillimetersPerInch = 25.4;

        /// <summary>Points per inch (typography points).</summary>
        public const double PointsPerInch = 72.0;
    }
}
