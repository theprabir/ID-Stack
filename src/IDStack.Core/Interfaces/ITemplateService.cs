using System;
using System.Threading.Tasks;
using IDStack.Core.Models.Template;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Creates, persists, and loads card templates (.idcard files).
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>Creates a new empty template with CR80 front and back sides.</summary>
        /// <param name="name">Template name.</param>
        /// <returns>The new template.</returns>
        CardTemplate CreateNew(string name);

        /// <summary>Loads a template from a .idcard file.</summary>
        /// <param name="filePath">Path to the file.</param>
        /// <returns>The deserialized template.</returns>
        Task<CardTemplate> LoadAsync(string filePath);

        /// <summary>Saves a template to a .idcard file atomically.</summary>
        /// <param name="template">Template to save.</param>
        /// <param name="filePath">Destination path.</param>
        /// <returns>A task completing when the write finishes.</returns>
        Task SaveAsync(CardTemplate template, string filePath);

        /// <summary>Validates a template and returns human-readable problems (empty when valid).</summary>
        /// <param name="template">Template to validate.</param>
        /// <returns>List of validation messages.</returns>
        System.Collections.Generic.List<string> Validate(CardTemplate template);
    }
}
