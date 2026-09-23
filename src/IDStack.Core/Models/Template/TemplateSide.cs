using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IDStack.Core.Models.Elements;

namespace IDStack.Core.Models.Template
{
    /// <summary>
    /// One side (front or back) of a card template with its canvas size and elements.
    /// </summary>
    public class TemplateSide
    {
        /// <summary>Creates an empty side of the given type with CR80 dimensions.</summary>
        public TemplateSide(SideType sideType)
        {
            SideType = sideType;
            CanvasWidth = 85.6;
            CanvasHeight = 54.0;
            BackgroundColor = "#FFFFFF";
            Elements = new ObservableCollection<CanvasElement>();
        }

        /// <summary>Which side this is.</summary>
        public SideType SideType { get; set; }

        /// <summary>Canvas width in millimeters.</summary>
        public double CanvasWidth { get; set; }

        /// <summary>Canvas height in millimeters.</summary>
        public double CanvasHeight { get; set; }

        /// <summary>Background color as hex string.</summary>
        public string BackgroundColor { get; set; }

        /// <summary>Optional background image path.</summary>
        public string BackgroundImage { get; set; }

        /// <summary>Elements placed on this side, in z-order.</summary>
        public ObservableCollection<CanvasElement> Elements { get; }

        /// <summary>Clones this side including a deep copy of all elements.</summary>
        /// <returns>An independent copy.</returns>
        public TemplateSide Clone()
        {
            var copy = new TemplateSide(SideType)
            {
                CanvasWidth = CanvasWidth,
                CanvasHeight = CanvasHeight,
                BackgroundColor = BackgroundColor,
                BackgroundImage = BackgroundImage
            };
            foreach (var element in Elements)
            {
                copy.Elements.Add(element.Clone());
            }
            return copy;
        }
    }
}
