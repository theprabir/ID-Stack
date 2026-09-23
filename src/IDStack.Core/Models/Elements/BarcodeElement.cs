namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Barcode element; actual barcode rendering lands with the batch engine.
    /// </summary>
    public class BarcodeElement : CanvasElement
    {
        /// <summary>Creates a QR barcode element with square defaults.</summary>
        public BarcodeElement()
        {
            Name = "Barcode";
            Width = 20;
            Height = 20;
            BarcodeType = BarcodeType.QR;
            ForegroundColor = "#000000";
            BackgroundColor = "#FFFFFF";
        }

        /// <inheritdoc />
        public override ElementType ElementType => ElementType.Barcode;

        /// <summary>Symbology to render.</summary>
        public BarcodeType BarcodeType { get; set; }

        /// <summary>Static data to encode (ignored when PlaceholderColumn is set).</summary>
        public string Data { get; set; }

        /// <summary>When set, encodes the value of this data column instead of Data.</summary>
        public string PlaceholderColumn { get; set; }

        /// <summary>Barcode foreground color as hex string.</summary>
        public string ForegroundColor { get; set; }

        /// <summary>Barcode background color as hex string.</summary>
        public string BackgroundColor { get; set; }

        /// <inheritdoc />
        public override CanvasElement Clone()
        {
            return CopyBase(new BarcodeElement
            {
                BarcodeType = BarcodeType,
                Data = Data,
                PlaceholderColumn = PlaceholderColumn,
                ForegroundColor = ForegroundColor,
                BackgroundColor = BackgroundColor
            });
        }
    }
}
