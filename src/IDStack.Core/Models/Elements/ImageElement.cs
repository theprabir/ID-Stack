namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Image element rendering a picture file or data-bound photo.
    /// </summary>
    public class ImageElement : CanvasElement
    {
        /// <summary>Creates an image element with square defaults.</summary>
        public ImageElement()
        {
            Name = "Image";
            Width = 20;
            Height = 25;
        }

        /// <inheritdoc />
        public override ElementType ElementType => ElementType.Image;

        /// <summary>Image file path (or placeholder token for data-bound photos).</summary>
        public string Source { get; set; }

        /// <summary>Stretch mode: Fill, Uniform, or UniformToFill.</summary>
        public string Stretch { get; set; } = "Uniform";

        /// <summary>Border color as hex string (empty for none).</summary>
        public string BorderColor { get; set; } = string.Empty;

        /// <summary>Border width in millimeters (0 for none).</summary>
        public double BorderWidth { get; set; }

        /// <inheritdoc />
        public override CanvasElement Clone()
        {
            return CopyBase(new ImageElement
            {
                Source = Source,
                Stretch = Stretch,
                BorderColor = BorderColor,
                BorderWidth = BorderWidth
            });
        }
    }
}
