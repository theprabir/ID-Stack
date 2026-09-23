namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Vector shape element: rectangle, ellipse, or line.
    /// </summary>
    public class ShapeElement : CanvasElement
    {
        /// <summary>Creates a rectangle shape with default styling.</summary>
        public ShapeElement()
        {
            Name = "Rectangle";
            ShapeKind = ShapeKind.Rectangle;
            FillColor = "#2563EB";
            StrokeColor = "#1D4ED8";
            StrokeWidth = 0.3;
        }

        /// <inheritdoc />
        public override ElementType ElementType => ElementType.Shape;

        /// <summary>Which shape geometry to draw.</summary>
        public ShapeKind ShapeKind { get; set; }

        /// <summary>Fill color as hex string (empty for no fill).</summary>
        public string FillColor { get; set; }

        /// <summary>Stroke color as hex string.</summary>
        public string StrokeColor { get; set; }

        /// <summary>Stroke width in millimeters.</summary>
        public double StrokeWidth { get; set; }

        /// <summary>Corner radius for rectangles, in millimeters.</summary>
        public double CornerRadius { get; set; }

        /// <inheritdoc />
        public override CanvasElement Clone()
        {
            return CopyBase(new ShapeElement
            {
                ShapeKind = ShapeKind,
                FillColor = FillColor,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                CornerRadius = CornerRadius
            });
        }
    }
}
