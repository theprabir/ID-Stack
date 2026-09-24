using System.Collections.Generic;
using System.Threading.Tasks;
using IDStack.Core.Models.Import;
using IDStack.Core.Models.Template;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Imports card designs (.idcard or Photoshop .psd) and auto-detects the
    /// text and image placeholders inside them for data mapping.
    /// </summary>
    public interface IDesignImportService
    {
        /// <summary>Loads a .idcard design file.</summary>
        /// <param name="filePath">Path to the design.</param>
        /// <returns>The loaded template.</returns>
        Task<CardTemplate> LoadIdcardAsync(string filePath);

        /// <summary>Imports a Photoshop .psd design and converts it to a template.</summary>
        /// <param name="filePath">Path to the .psd file.</param>
        /// <returns>The converted template and a per-layer report (PsdImportResult lives in IDStack.Services).</returns>
        Task<object> ImportPsdAsync(string filePath);

        /// <summary>Detects mappable placeholders (text + image) inside a template.</summary>
        /// <param name="template">The design.</param>
        /// <returns>All detected placeholders, front side first.</returns>
        List<DesignPlaceholder> DetectPlaceholders(CardTemplate template);
    }
}
