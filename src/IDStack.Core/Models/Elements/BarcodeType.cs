namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Barcode symbologies supported by the application.
    /// </summary>
    public enum BarcodeType
    {
        /// <summary>QR two-dimensional code.</summary>
        QR,

        /// <summary>Code 128 linear barcode.</summary>
        Code128,

        /// <summary>Code 39 linear barcode.</summary>
        Code39,

        /// <summary>EAN-13 retail barcode.</summary>
        EAN13,

        /// <summary>UPC-A retail barcode.</summary>
        UPCA
    }
}
