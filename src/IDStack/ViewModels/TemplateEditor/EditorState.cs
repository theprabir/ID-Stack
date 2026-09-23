using System.Collections.Generic;
using System.Linq;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Template;

namespace IDStack.ViewModels.TemplateEditor
{
    /// <summary>
    /// Immutable snapshot of one template side, used for undo/redo history.
    /// </summary>
    public class EditorState
    {
        /// <summary>Creates a snapshot from a template side.</summary>
        public EditorState(TemplateSide side)
        {
            CanvasWidth = side.CanvasWidth;
            CanvasHeight = side.CanvasHeight;
            BackgroundColor = side.BackgroundColor;
            BackgroundImage = side.BackgroundImage;
            Elements = side.Elements.Select(e => e.Clone()).ToList();
        }

        /// <summary>Canvas width in millimeters.</summary>
        public double CanvasWidth { get; }

        /// <summary>Canvas height in millimeters.</summary>
        public double CanvasHeight { get; }

        /// <summary>Background color hex.</summary>
        public string BackgroundColor { get; }

        /// <summary>Background image path.</summary>
        public string BackgroundImage { get; }

        /// <summary>Cloned element list in z-order.</summary>
        public IReadOnlyList<CanvasElement> Elements { get; }

        /// <summary>Restores this snapshot into a template side.</summary>
        /// <param name="side">Target side to overwrite.</param>
        public void ApplyTo(TemplateSide side)
        {
            side.CanvasWidth = CanvasWidth;
            side.CanvasHeight = CanvasHeight;
            side.BackgroundColor = BackgroundColor;
            side.BackgroundImage = BackgroundImage;
            side.Elements.Clear();
            foreach (var element in Elements)
            {
                side.Elements.Add(element.Clone());
            }
        }
    }
}
