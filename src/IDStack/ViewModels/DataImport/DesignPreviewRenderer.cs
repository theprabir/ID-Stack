using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Template;
using IDStack.Views.TemplateEditor;

namespace IDStack.ViewModels.DataImport
{
    /// <summary>
    /// Renders a template side to a small preview bitmap (background + all
    /// element visuals), so the import step shows the actual design.
    /// </summary>
    public static class DesignPreviewRenderer
    {
        private const double PreviewWidthPx = 240;

        /// <summary>
        /// Renders the front side of a template to a frozen preview bitmap.
        /// </summary>
        /// <param name="template">Loaded design.</param>
        /// <returns>Preview bitmap, or null if rendering is not possible.</returns>
        public static BitmapSource Render(CardTemplate template)
        {
            if (template?.FrontSide == null)
            {
                return null;
            }

            var side = template.FrontSide;
            var canvasWidth = side.CanvasWidth > 0 ? side.CanvasWidth : 85.6;
            var canvasHeight = side.CanvasHeight > 0 ? side.CanvasHeight : 54.0;
            var scale = PreviewWidthPx / canvasWidth;
            var pixelHeight = (int)Math.Max(1, Math.Round(canvasHeight * scale));

            var canvas = new Canvas
            {
                Width = PreviewWidthPx,
                Height = pixelHeight,
                Background = MakeBackgroundBrush(side)
            };

            foreach (var element in side.Elements)
            {
                try
                {
                    var visual = CanvasElementRenderer.CreateVisual(element, scale);
                    if (visual != null)
                    {
                        canvas.Children.Add(visual);
                    }
                }
                catch
                {
                    // A single bad element must never break the preview.
                }
            }

            canvas.Measure(new Size(PreviewWidthPx, pixelHeight));
            canvas.Arrange(new Rect(0, 0, PreviewWidthPx, pixelHeight));

            var bitmap = new RenderTargetBitmap(
                (int)Math.Round(PreviewWidthPx), pixelHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(canvas);
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>Builds the side background (color and/or image).</summary>
        private static Brush MakeBackgroundBrush(TemplateSide side)
        {
            Brush fill = null;
            if (!string.IsNullOrWhiteSpace(side.BackgroundColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(side.BackgroundColor);
                    fill = new SolidColorBrush(color);
                }
                catch (FormatException)
                {
                    fill = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(side.BackgroundImage) && File.Exists(side.BackgroundImage))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(side.BackgroundImage);
                    image.EndInit();
                    image.Freeze();

                    var brush = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
                    return (Brush)brush;
                }
                catch (IOException)
                {
                    // Fall through to the color fill.
                }
            }

            return fill ?? Brushes.White;
        }
    }
}
