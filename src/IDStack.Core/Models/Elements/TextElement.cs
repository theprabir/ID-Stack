namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Static text element with font styling.
    /// </summary>
    public class TextElement : CanvasElement
    {
        /// <summary>Creates a text element with sensible defaults.</summary>
        public TextElement()
        {
            Name = "Text";
            Text = "Text";
            FontFamily = "Segoe UI";
            FontSize = 10;
            IsBold = false;
            IsItalic = false;
            IsUnderline = false;
            TextColor = "#000000";
            Alignment = "Left";
        }

        /// <inheritdoc />
        public override ElementType ElementType => ElementType.Text;

        /// <summary>The text content.</summary>
        public string Text { get; set; }

        /// <summary>Font family name.</summary>
        public string FontFamily { get; set; }

        /// <summary>Font size in points.</summary>
        public double FontSize { get; set; }

        /// <summary>Bold flag.</summary>
        public bool IsBold { get; set; }

        /// <summary>Italic flag.</summary>
        public bool IsItalic { get; set; }

        /// <summary>Underline flag.</summary>
        public bool IsUnderline { get; set; }

        /// <summary>Text color as hex string.</summary>
        public string TextColor { get; set; }

        /// <summary>Horizontal alignment: Left, Center, or Right.</summary>
        public string Alignment { get; set; }

        /// <inheritdoc />
        public override CanvasElement Clone()
        {
            return CopyBase(new TextElement
            {
                Text = Text,
                FontFamily = FontFamily,
                FontSize = FontSize,
                IsBold = IsBold,
                IsItalic = IsItalic,
                IsUnderline = IsUnderline,
                TextColor = TextColor,
                Alignment = Alignment
            });
        }
    }
}
