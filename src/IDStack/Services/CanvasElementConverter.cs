using System;
using IDStack.Core.Models.Elements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDStack.Services
{
    /// <summary>
    /// Polymorphic JSON converter: maps the stored ElementType discriminator back to
    /// the concrete CanvasElement subclass on deserialization, and serializes any
    /// CanvasElement with its discriminator included.
    /// </summary>
    public sealed class CanvasElementConverter : JsonConverter
    {
        /// <summary>Whether this converter can write JSON (true — includes the discriminator).</summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Determines whether the object type is handled by this converter.
        /// </summary>
        /// <param name="objectType">Type being (de)serialized.</param>
        /// <returns>True for CanvasElement and its subclasses.</returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(CanvasElement).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Reads a JSON object and constructs the matching concrete element.
        /// </summary>
        /// <param name="reader">JSON reader.</param>
        /// <param name="objectType">Declared type.</param>
        /// <param name="existingValue">Existing value (ignored).</param>
        /// <param name="serializer">Serializer for populating properties.</param>
        /// <returns>The deserialized element.</returns>
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var json = JObject.Load(reader);

            CanvasElement element;
            if (json.TryGetValue("ElementType", out var token) &&
                Enum.TryParse<ElementType>(token.Value<string>(), true, out var kind))
            {
                switch (kind)
                {
                    case ElementType.Text: element = new TextElement(); break;
                    case ElementType.Image: element = new ImageElement(); break;
                    case ElementType.Shape: element = new ShapeElement(); break;
                    case ElementType.Barcode: element = new BarcodeElement(); break;
                    case ElementType.Placeholder: element = new PlaceholderElement(); break;
                    default:
                        throw new JsonSerializationException(
                            "Unknown ElementType in template data: " + token.Value<string>());
                }
            }
            else
            {
                throw new JsonSerializationException(
                    "Template element is missing an ElementType discriminator.");
            }

            using (var subReader = json.CreateReader())
            {
                serializer.Populate(subReader, element);
            }
            return element;
        }

        /// <summary>
        /// Writes the element as a JSON object (its properties already include ElementType).
        /// </summary>
        /// <param name="writer">JSON writer.</param>
        /// <param name="value">Element to serialize.</param>
        /// <param name="serializer">Serializer to use.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Use a converter-free serializer for the inner object to avoid re-entering
            // this converter (which would recurse forever).
            var inner = new JsonSerializer
            {
                Formatting = serializer.Formatting,
                ContractResolver = serializer.ContractResolver,
                NullValueHandling = serializer.NullValueHandling,
                DefaultValueHandling = serializer.DefaultValueHandling
            };
            var json = JObject.FromObject(value, inner);
            json.WriteTo(writer);
        }
    }
}
