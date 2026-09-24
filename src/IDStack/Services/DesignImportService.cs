using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Import;
using IDStack.Core.Models.Template;

namespace IDStack.Services
{
    /// <summary>
    /// Loads .idcard designs or imports Photoshop .psd files, then detects every
    /// mappable placeholder: text layers receive column text, image layers
    /// receive matched photos. Placeholder names come from the design's layer
    /// names and are renameable so mapping stays unambiguous.
    /// </summary>
    public class DesignImportService : IDesignImportService
    {
        private readonly ITemplateService _templateService;
        private readonly PsdDesignImporter _psdImporter;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates the design import service.
        /// </summary>
        /// <param name="templateService">Template persistence.</param>
        /// <param name="psdImporter">PSD converter.</param>
        /// <param name="logger">Logger.</param>
        public DesignImportService(ITemplateService templateService, PsdDesignImporter psdImporter, ILogger logger)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _psdImporter = psdImporter ?? throw new ArgumentNullException(nameof(psdImporter));
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<CardTemplate> LoadIdcardAsync(string filePath)
        {
            return _templateService.LoadAsync(filePath);
        }

        /// <inheritdoc />
        public async Task<object> ImportPsdAsync(string filePath)
        {
            return await Task.Run(() => (object)_psdImporter.Import(filePath)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public List<DesignPlaceholder> DetectPlaceholders(CardTemplate template)
        {
            var placeholders = new List<DesignPlaceholder>();
            if (template == null)
            {
                return placeholders;
            }

            DetectFromSide(template.FrontSide, placeholders);
            DetectFromSide(template.BackSide, placeholders);

            // Ensure unique names: "Name", "Name 2", "Name 3"…
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var placeholder in placeholders)
            {
                var unique = placeholder.SuggestedName;
                var counter = 2;
                while (!used.Add(unique))
                {
                    unique = placeholder.SuggestedName + " " + counter;
                    counter++;
                }
                placeholder.Name = unique;
            }

            _logger?.Info("Detected " + placeholders.Count + " placeholders (" +
                          placeholders.Count(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text) + " text, " +
                          placeholders.Count(p => p.Kind == DesignPlaceholder.PlaceholderKind.Image) + " image).");
            return placeholders;
        }

        private static void DetectFromSide(TemplateSide side, List<DesignPlaceholder> placeholders)
        {
            if (side == null)
            {
                return;
            }

            foreach (var element in side.Elements)
            {
                if (element is TextElement text && !string.IsNullOrWhiteSpace(text.Text))
                {
                    var placeholder = new DesignPlaceholder(
                        DesignPlaceholder.PlaceholderKind.Text,
                        element.Id,
                        SuggestName(text.Text, element.Name),
                        side.SideType)
                    {
                        SampleText = text.Text
                    };
                    placeholders.Add(placeholder);
                }
                else if (element is ImageElement image && !IsDesignBackground(image))
                {
                    placeholders.Add(new DesignPlaceholder(
                        DesignPlaceholder.PlaceholderKind.Image,
                        element.Id,
                        string.IsNullOrWhiteSpace(element.Name) ? "Photo" : element.Name.Trim(),
                        side.SideType));
                }
            }
        }

        private static bool IsDesignBackground(ImageElement image)
        {
            return string.Equals(image.Name, "Design", StringComparison.OrdinalIgnoreCase);
        }

        private static string SuggestName(string text, string fallback)
        {
            // Text layer content is usually the best name ("Full Name", "ID No"…);
            // trim to a sane length and fall back to the layer name.
            var candidate = (text ?? string.Empty).Trim();
            if (candidate.Length > 30)
            {
                candidate = candidate.Substring(0, 30).Trim();
            }

            if (candidate.Length == 0)
            {
                candidate = string.IsNullOrWhiteSpace(fallback) ? "Text" : fallback.Trim();
            }

            return candidate;
        }
    }
}
