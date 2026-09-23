using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Template;
using Newtonsoft.Json;

namespace IDStack.Services
{
    /// <summary>
    /// Persists card templates as JSON .idcard files with atomic writes.
    /// </summary>
    public class TemplateService : ITemplateService
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            Formatting = Formatting.Indented,
            Converters = { new CanvasElementConverter() }
        };
        /// <inheritdoc />
        public CardTemplate CreateNew(string name)
        {
            return new CardTemplate { Name = string.IsNullOrWhiteSpace(name) ? "Untitled Template" : name };
        }

        /// <inheritdoc />
        public async Task<CardTemplate> LoadAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path is required", nameof(filePath));
            }

            using (var reader = new StreamReader(filePath))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var template = Serializer.Deserialize<CardTemplate>(jsonReader);
                if (template == null)
                {
                    throw new InvalidDataException("Template file is empty or invalid: " + filePath);
                }
                return template;
            }
        }

        /// <inheritdoc />
        public async Task SaveAsync(CardTemplate template, string filePath)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path is required", nameof(filePath));
            }

            template.ModifiedDate = DateTime.Now;

            var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = filePath + ".tmp";
            using (var writer = new StreamWriter(tempPath, false))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                Serializer.Serialize(jsonWriter, template);
                await jsonWriter.FlushAsync().ConfigureAwait(false);
            }

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }

        /// <inheritdoc />
        public List<string> Validate(CardTemplate template)
        {
            var problems = new List<string>();
            if (template == null)
            {
                problems.Add("Template is null.");
                return problems;
            }

            if (string.IsNullOrWhiteSpace(template.Name))
            {
                problems.Add("Template name is empty.");
            }

            foreach (var side in new[] { template.FrontSide, template.BackSide })
            {
                if (side == null)
                {
                    problems.Add("Template is missing a side.");
                    continue;
                }

                if (side.CanvasWidth <= 0 || side.CanvasHeight <= 0)
                {
                    problems.Add(side.SideType + " side has invalid canvas dimensions.");
                }

                foreach (var element in side.Elements)
                {
                    if (element.Width <= 0 || element.Height <= 0)
                    {
                        problems.Add(element.Name + " on " + side.SideType + " side has invalid size.");
                    }

                    if (element is PlaceholderElement placeholder && string.IsNullOrWhiteSpace(placeholder.ColumnName))
                    {
                        problems.Add(placeholder.Name + " on " + side.SideType + " side has no data column set.");
                    }
                }
            }

            return problems;
        }
    }
}
