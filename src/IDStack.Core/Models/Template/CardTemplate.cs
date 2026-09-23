using System;
using System.Collections.Generic;

namespace IDStack.Core.Models.Template
{
    /// <summary>
    /// Root model for a card template: two sides plus metadata, serialized to .idcard files.
    /// </summary>
    public class CardTemplate
    {
        /// <summary>Creates an empty template with default CR80 front and back sides.</summary>
        public CardTemplate()
        {
            Id = Guid.NewGuid();
            Name = "Untitled Template";
            Version = "1.0";
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            FrontSide = new TemplateSide(SideType.Front);
            BackSide = new TemplateSide(SideType.Back);
            Metadata = new Dictionary<string, string>();
        }

        /// <summary>Unique template identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Display name of the template.</summary>
        public string Name { get; set; }

        /// <summary>Template schema version.</summary>
        public string Version { get; set; }

        /// <summary>Creation timestamp.</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>Last modification timestamp.</summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>Front side of the card.</summary>
        public TemplateSide FrontSide { get; set; }

        /// <summary>Back side of the card.</summary>
        public TemplateSide BackSide { get; set; }

        /// <summary>Free-form metadata (author, category, etc.).</summary>
        public Dictionary<string, string> Metadata { get; set; }

        /// <summary>Returns the side matching the given type.</summary>
        /// <param name="sideType">Front or Back.</param>
        /// <returns>The matching side.</returns>
        public TemplateSide GetSide(SideType sideType)
        {
            return sideType == SideType.Front ? FrontSide : BackSide;
        }
    }
}
