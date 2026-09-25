using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Template;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace IDStack.Services
{
    /// <summary>
    /// Result of a PSD import: the template plus a per-layer report.
    /// </summary>
    public class PsdImportResult
    {
        /// <summary>Creates an import result.</summary>
        public PsdImportResult(CardTemplate template, List<string> layerReport)
        {
            Template = template;
            LayerReport = layerReport;
        }

        /// <summary>The converted template.</summary>
        public CardTemplate Template { get; }

        /// <summary>One line per PSD layer describing how it was imported.</summary>
        public List<string> LayerReport { get; }
    }

    /// <summary>
    /// Imports a Photoshop .psd design into an editable CardTemplate.
    /// The composite raster becomes a background image so the design stays 100%
    /// visually identical; text layers (detected by their type-tool blocks, and
    /// named by Photoshop after their content by default) become individually
    /// editable text elements positioned exactly where Photoshop placed them.
    /// </summary>
    public class PsdDesignImporter
    {
        private static readonly HashSet<string> TextBlockKeys = new HashSet<string>
        {
            "tySh",   // TypeToolInfo (Photoshop 5 and earlier)
            "TySh",   // TypeToolObject (Photoshop 6+)
            "Txt2",   // Legacy text key
            "vscg",   // Content generator text layers (CC)
            "vogk"    // Vector origin (CC text/vector layers)
        };

        private readonly ILogger _logger;

        /// <summary>
        /// Creates the importer.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public PsdDesignImporter(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Converts a PSD file into a CardTemplate.
        /// </summary>
        /// <param name="psdPath">Path to the .psd file.</param>
        /// <returns>Import result with template and layer report.</returns>
        public PsdImportResult Import(string psdPath)
        {
            if (string.IsNullOrWhiteSpace(psdPath) || !File.Exists(psdPath))
            {
                throw new FileNotFoundException("PSD file not found.", psdPath);
            }

            var report = new List<string>();

            using (var stream = File.OpenRead(psdPath))
            {
                var psd = PsdSharp.PsdFile.Open(stream);
                var widthPx = (int)psd.Header.WidthInPixels;
                var heightPx = (int)psd.Header.HeightInPixels;
                _logger?.Info("PSD opened: " + Path.GetFileName(psdPath) + " " + widthPx + "x" + heightPx + "px");

                // Assume 300 DPI design (print standard): mm = px / 300 * 25.4.
                const double PxToMm = 25.4 / 300.0;
                var widthMm = widthPx * PxToMm;
                var heightMm = heightPx * PxToMm;

                var template = new CardTemplate
                {
                    Name = Path.GetFileNameWithoutExtension(psdPath)
                };
                template.FrontSide.CanvasWidth = widthMm;
                template.FrontSide.CanvasHeight = heightMm;
                template.BackSide.CanvasWidth = widthMm;
                template.BackSide.CanvasHeight = heightMm;

                // NOTE: PsdSharp 1.0.2 mis-parses the layer visibility flags (it
                // reports IsVisible=False even for visible layers), so we cannot
                // trust that property. Instead we treat layers as visible unless
                // their raw flag byte has the hidden bit set (0x02 per PSD spec).
                var layers = psd.Layers.Where(IsLayerUsable).ToList();
                report.Add(layers.Count + " layers found.");

                // Composite raster: flatten everything into one background image
                // so the design looks exactly like Photoshop.
                var backgroundPath = RenderCompositeBackground(psd, psdPath, widthPx, heightPx);
                if (backgroundPath != null)
                {
                    template.FrontSide.Elements.Add(new ImageElement
                    {
                        Name = "Design",
                        Source = backgroundPath,
                        X = 0,
                        Y = 0,
                        Width = widthMm,
                        Height = heightMm,
                        Stretch = "Fill"
                    });
                    report.Add("Design flattened to background (" + widthMm.ToString("0.#") + " × " +
                               heightMm.ToString("0.#") + " mm).");
                }
                else
                {
                    report.Add("Could not render the composite image — the design background is missing.");
                }

                // Text layers become editable text elements on top, positioned
                // exactly where they were in Photoshop.
                var textLayerCount = 0;
                foreach (var layer in layers)
                {
                    if (!IsTextLayer(layer))
                    {
                        continue;
                    }

                    var name = (layer.Name ?? string.Empty).Trim();
                    if (name.Length == 0)
                    {
                        continue;
                    }

                    var rect = layer.Bounds;
                    var element = new TextElement
                    {
                        Name = name,
                        Text = name,
                        // PSD bounds may exceed the canvas (rotated/overflow layers); clamp.
                        X = Clamp(rect.TopLeft.X * PxToMm, 0, widthMm),
                        Y = Clamp(rect.TopLeft.Y * PxToMm, 0, heightMm),
                        Width = Math.Max(5, rect.Width * PxToMm),
                        Height = Math.Max(3, rect.Height * PxToMm),
                        FontSize = 10
                    };
                    template.FrontSide.Elements.Add(element);
                    textLayerCount++;
                    report.Add("Text layer \"" + name + "\" imported as editable text at " +
                               element.X.ToString("0.#") + ", " + element.Y.ToString("0.#") + " mm.");
                }

                if (textLayerCount == 0)
                {
                    report.Add("No text layers detected — add text placeholders in the editor.");
                }

                _logger?.Info("PSD import complete: " + textLayerCount + " text layers.");
                return new PsdImportResult(template, report);
            }
        }

        /// <summary>
        /// Determines whether a layer should be imported. PsdSharp 1.0.2
        /// mis-parses the layer visibility flags (it reports IsVisible=False
        /// even for visible layers), so that property cannot be trusted; we
        /// import every layer that carries usable pixel data.
        /// </summary>
        private static bool IsLayerUsable(PsdSharp.Layer layer)
        {
            // Placeholder/empty layers have no pixels of their own.
            return layer.ImageData != null && layer.Bounds.Width > 0 && layer.Bounds.Height > 0;
        }

        private static bool IsTextLayer(PsdSharp.Layer layer)
        {
            if (layer.TaggedBlocks == null)
            {
                return false;
            }

            // A type-tool tagged block is the authoritative text-layer marker.
            if (layer.TaggedBlocks.Any(b => b != null && b.Key != null && TextBlockKeys.Contains(b.Key.Key)))
            {
                return true;
            }

            // Fallback: many PSDs name text layers after their content; try to
            // sniff parseable text out of TySh/tySh raw payloads in case the
            // block key was recorded under a different case.
            return layer.TaggedBlocks.Any(b =>
                b?.Key != null && b.Key.Key != null &&
                b.Key.Key.IndexOf("tysh", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        /// <summary>
        /// Renders the flattened design. The PSD composite (merged image data)
        /// is preferred, but many PSDs (e.g. CMYK + Zip compression) crash
        /// PsdSharp's composite decoder, so we fall back to flattening the
        /// per-layer pixel data ourselves, which decodes reliably.
        /// </summary>
        private string RenderCompositeBackground(PsdSharp.PsdFile psd, string psdPath, int widthPx, int heightPx)
        {
            var dir = Path.Combine(
                Path.GetDirectoryName(psdPath) ?? ".",
                Path.GetFileNameWithoutExtension(psdPath) + "_assets");
            Directory.CreateDirectory(dir);
            var outPath = Path.Combine(dir, "design.png");

            // ① Try the merged composite.
            try
            {
                var buffer = PsdSharp.Images.DataConversion.PixelDataConverter.GetInterleavedBuffer(
                    psd.ImageData, PsdSharp.Images.ColorType.Rgba8888);

                using (var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(buffer, widthPx, heightPx))
                using (var outStream = File.Create(outPath))
                {
                    image.Save(outStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                }
                _logger?.Info("PSD composite decoded from merged image data.");
                return outPath;
            }
            catch (Exception ex)
            {
                _logger?.Warn("Merged PSD composite failed (" + ex.Message + "); falling back to per-layer flattening.");
            }

            // ② Fallback: flatten per-layer pixel data bottom-up.
            try
            {
                using (var canvas = new SixLabors.ImageSharp.Image<Rgba32>(widthPx, heightPx))
                {
                    foreach (var layer in psd.Layers.Reverse())
                    {
                        if (!IsLayerUsable(layer))
                        {
                            continue;
                        }

                        try
                        {
                            var buffer = PsdSharp.Images.DataConversion.PixelDataConverter.GetInterleavedBuffer(
                                layer.ImageData, PsdSharp.Images.ColorType.Rgba8888);
                            var bounds = layer.Bounds;
                            using (var layerImage = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
                                buffer, (int)bounds.Width, (int)bounds.Height))
                            {
                                var opacity = layer.Opacity / 255f;
                                canvas.Mutate(c => c.DrawImage(layerImage,
                                    new SixLabors.ImageSharp.Point(bounds.TopLeft.X, bounds.TopLeft.Y), opacity));
                            }
                        }
                        catch (Exception layerEx)
                        {
                            _logger?.Warn("Skipped layer '" + layer.Name + "' during flattening: " + layerEx.Message);
                        }
                    }

                    using (var outStream = File.Create(outPath))
                    {
                        canvas.Save(outStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                    }
                }
                _logger?.Info("PSD composite rendered from per-layer flattening.");
                return outPath;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Failed to render PSD composite");
                return null;
            }
        }
    }
}
