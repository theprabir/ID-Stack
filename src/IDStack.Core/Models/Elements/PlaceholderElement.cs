namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Data-bound text element: renders the value of an imported data column.
    /// </summary>
    public class PlaceholderElement : CanvasElement
    {
        /// <summary>Creates a placeholder with defaults.</summary>
        public PlaceholderElement()
        {
            Name = "Placeholder";
            FontFamily = "Segoe UI";
            FontSize = 10;
            TextColor = "#000000";
            IsBold = false;
            IsItalic = false;
            Alignment = "Left";
            AutoResize = true;
        }

        /// <inheritdoc />
        public override ElementType ElementType => ElementType.Placeholder;

        /// <summary>Data column name this placeholder binds to (without braces).</summary>
        public string ColumnName { get; set; }

        /// <summary>Preview label shown in the editor when no data is bound.</summary>
        public string Text { get; set; } = "{{Column}}";

        /// <summary>Fallback text shown when the column value is missing.</summary>
        public string DefaultValue { get; set; }

        /// <summary>Optional display format (dates, numbers).</summary>
        public string TextFormat { get; set; }

        /// <summary>Font family name.</summary>
        public string FontFamily { get; set; }

        /// <summary>Font size in points.</summary>
        public double FontSize { get; set; }

        /// <summary>Text color as hex string.</summary>
        public string TextColor { get; set; }

        /// <summary>Bold flag.</summary>
        public bool IsBold { get; set; }

        /// <summary>Italic flag.</summary>
        public bool IsItalic { get; set; }

        /// <summary>Horizontal alignment: Left, Center, or Right.</summary>
        public string Alignment { get; set; }

        /// <summary>Whether to shrink/expand to fit content.</summary>
        public bool AutoResize { get; set; }

        /// <inheritdoc />
        public override CanvasElement Clone()
        {
            return CopyBase(new PlaceholderElement
            {
                ColumnName = ColumnName,
                DefaultValue = DefaultValue,
                TextFormat = TextFormat,
                FontFamily = FontFamily,
                FontSize = FontSize,
                TextColor = TextColor,
                IsBold = IsBold,
                IsItalic = IsItalic,
                Alignment = Alignment,
                AutoResize = AutoResize
            });
        }
    }
}
