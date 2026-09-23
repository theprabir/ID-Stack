using System;

namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Abstract base for all elements placed on a template canvas.
    /// Coordinates and sizes are in millimeters.
    /// </summary>
    public abstract class CanvasElement
    {
        /// <summary>Creates an element with a fresh identifier and default CR80-scale geometry.</summary>
        protected CanvasElement()
        {
            Id = Guid.NewGuid();
            Name = string.Empty;
            X = 10;
            Y = 10;
            Width = 20;
            Height = 10;
            Rotation = 0;
            Opacity = 1.0;
            IsVisible = true;
            IsLocked = false;
        }

        /// <summary>Unique element identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>User-visible element name.</summary>
        public string Name { get; set; }

        /// <summary>Element kind discriminator.</summary>
        public abstract ElementType ElementType { get; }

        /// <summary>X position in millimeters.</summary>
        public double X { get; set; }

        /// <summary>Y position in millimeters.</summary>
        public double Y { get; set; }

        /// <summary>Width in millimeters.</summary>
        public double Width { get; set; }

        /// <summary>Height in millimeters.</summary>
        public double Height { get; set; }

        /// <summary>Rotation in degrees (clockwise).</summary>
        public double Rotation { get; set; }

        /// <summary>Opacity from 0.0 (transparent) to 1.0 (opaque).</summary>
        public double Opacity { get; set; }

        /// <summary>Whether the element is rendered.</summary>
        public bool IsVisible { get; set; }

        /// <summary>Whether the element can be moved or edited on the canvas.</summary>
        public bool IsLocked { get; set; }

        /// <summary>Creates a deep copy of this element.</summary>
        /// <returns>An independent copy with the same geometry and style.</returns>
        public abstract CanvasElement Clone();

        /// <summary>Copies base geometry to a clone.</summary>
        /// <param name="copy">The clone to populate.</param>
        /// <typeparam name="T">Concrete element type.</typeparam>
        /// <returns>The populated clone.</returns>
        protected T CopyBase<T>(T copy) where T : CanvasElement
        {
            copy.Id = Guid.NewGuid();
            copy.Name = Name;
            copy.X = X;
            copy.Y = Y;
            copy.Width = Width;
            copy.Height = Height;
            copy.Rotation = Rotation;
            copy.Opacity = Opacity;
            copy.IsVisible = IsVisible;
            copy.IsLocked = IsLocked;
            return copy;
        }
    }
}
