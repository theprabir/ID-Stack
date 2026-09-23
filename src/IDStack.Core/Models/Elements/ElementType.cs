namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Discriminates concrete canvas element kinds.
    /// </summary>
    public enum ElementType
    {
        /// <summary>Static text element.</summary>
        Text,

        /// <summary>Image element.</summary>
        Image,

        /// <summary>Shape element (rectangle, circle, line).</summary>
        Shape,

        /// <summary>Barcode element (QR, Code 128, etc.).</summary>
        Barcode,

        /// <summary>Data-bound placeholder element.</summary>
        Placeholder
    }
}
