using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IDStack.Core.Models.Elements;

namespace IDStack.Views.TemplateEditor
{
    /// <summary>
    /// Builds the WPF visual for a canvas element model. Kept as a static factory
    /// so the canvas ItemsControl can bind models while staying MVVM-clean.
    /// </summary>
    public static class CanvasElementRenderer
    {
        /// <summary>
        /// Creates the framework element visualizing the given element model.
        /// </summary>
        /// <param name="element">Element model.</param>
        /// <param name="scale">Pixels per millimeter.</param>
        /// <returns>A configured visual, or null for unsupported kinds.</returns>
        public static FrameworkElement CreateVisual(CanvasElement element, double scale)
        {
            if (element == null)
            {
                return null;
            }

            FrameworkElement visual;
            switch (element.ElementType)
            {
                case ElementType.Text:
                    visual = CreateTextVisual((TextElement)element);
                    break;
                case ElementType.Placeholder:
                    visual = CreatePlaceholderVisual((PlaceholderElement)element);
                    break;
                case ElementType.Image:
                    visual = CreateImageVisual((ImageElement)element);
                    break;
                case ElementType.Shape:
                    visual = CreateShapeVisual((ShapeElement)element);
                    break;
                case ElementType.Barcode:
                    visual = CreateBarcodeVisual((BarcodeElement)element);
                    break;
                default:
                    return null;
            }

            ApplyGeometry(visual, element, scale);
            return visual;
        }

        /// <summary>Positions and sizes a visual from model geometry.</summary>
        /// <param name="visual">Target visual.</param>
        /// <param name="element">Source model.</param>
        /// <param name="scale">Pixels per millimeter.</param>
        public static void ApplyGeometry(FrameworkElement visual, CanvasElement element, double scale)
        {
            Canvas.SetLeft(visual, element.X * scale);
            Canvas.SetTop(visual, element.Y * scale);
            visual.Width = Math.Max(1, element.Width * scale);
            visual.Height = Math.Max(1, element.Height * scale);
            visual.Opacity = element.Opacity;
            visual.Visibility = element.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            var transform = new RotateTransform(element.Rotation);
            transform.CenterX = visual.Width / 2;
            transform.CenterY = visual.Height / 2;
            visual.RenderTransform = transform;
        }

        private static FrameworkElement CreateTextVisual(TextElement text)
        {
            var block = new TextBlock
            {
                Text = text.Text,
                FontFamily = new FontFamily(text.FontFamily),
                FontSize = text.FontSize * 96.0 / 72.0,
                Foreground = ParseBrush(text.TextColor, Brushes.Black),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = ParseAlignment(text.Alignment)
            };
            if (text.IsBold) { block.FontWeight = FontWeights.Bold; }
            if (text.IsItalic) { block.FontStyle = FontStyles.Italic; }
            if (text.IsUnderline) { block.TextDecorations = TextDecorations.Underline; }
            return block;
        }

        private static FrameworkElement CreatePlaceholderVisual(PlaceholderElement placeholder)
        {
            var border = new Border
            {
                BorderBrush = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = string.IsNullOrEmpty(placeholder.ColumnName)
                        ? (placeholder.Text ?? "{{Column}}")
                        : "{{" + placeholder.ColumnName + "}}",
                    FontFamily = new FontFamily(placeholder.FontFamily),
                    FontSize = placeholder.FontSize * 96.0 / 72.0,
                    Foreground = ParseBrush(placeholder.TextColor, Brushes.Black),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = ParseAlignment(placeholder.Alignment),
                    Padding = new Thickness(2)
                }
            };
            if (placeholder.IsBold)
            {
                ((TextBlock)border.Child).FontWeight = FontWeights.Bold;
            }
            return border;
        }

        private static FrameworkElement CreateImageVisual(ImageElement image)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                BorderBrush = string.IsNullOrEmpty(image.BorderColor)
                    ? null
                    : ParseBrush(image.BorderColor, Brushes.Gray),
                BorderThickness = image.BorderWidth > 0
                    ? new Thickness(image.BorderWidth * 3.78)
                    : new Thickness(0)
            };

            var text = new TextBlock
            {
                Text = "🖼",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16
            };
            border.Child = text;
            return border;
        }

        private static FrameworkElement CreateShapeVisual(ShapeElement shape)
        {
            Shape visual;
            switch (shape.ShapeKind)
            {
                case ShapeKind.Ellipse:
                    visual = new Ellipse
                    {
                        Fill = ParseBrush(shape.FillColor, null),
                        Stroke = ParseBrush(shape.StrokeColor, Brushes.Gray),
                        StrokeThickness = Math.Max(1, shape.StrokeWidth * 3.78)
                    };
                    break;
                case ShapeKind.Line:
                    visual = new Rectangle
                    {
                        Fill = ParseBrush(shape.StrokeColor, Brushes.Gray),
                        StrokeThickness = 0
                    };
                    break;
                default:
                    visual = new Rectangle
                    {
                        RadiusX = shape.CornerRadius * 3.78,
                        RadiusY = shape.CornerRadius * 3.78,
                        Fill = ParseBrush(shape.FillColor, null),
                        Stroke = ParseBrush(shape.StrokeColor, Brushes.Gray),
                        StrokeThickness = Math.Max(1, shape.StrokeWidth * 3.78)
                    };
                    break;
            }
            return visual;
        }

        private static FrameworkElement CreateBarcodeVisual(BarcodeElement barcode)
        {
            var border = new Border
            {
                Background = ParseBrush(barcode.BackgroundColor, Brushes.White),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            };
            border.Child = new TextBlock
            {
                Text = barcode.BarcodeType.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 9,
                Foreground = ParseBrush(barcode.ForegroundColor, Brushes.Black)
            };
            return border;
        }

        private static Brush ParseBrush(string hex, Brush fallback)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return fallback;
            }

            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                brush.Freeze();
                return brush;
            }
            catch (FormatException)
            {
                return fallback;
            }
        }

        private static TextAlignment ParseAlignment(string alignment)
        {
            switch (alignment)
            {
                case "Center": return TextAlignment.Center;
                case "Right": return TextAlignment.Right;
                default: return TextAlignment.Left;
            }
        }
    }
}
