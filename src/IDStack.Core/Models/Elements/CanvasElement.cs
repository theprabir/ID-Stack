using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDStack.Core.Models.Elements
{
    /// <summary>
    /// Abstract base for all elements placed on a template canvas.
    /// Coordinates and sizes are in millimeters. Raises property-change
    /// notifications so editor surfaces can update live.
    /// </summary>
    public abstract class CanvasElement : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Raises the property-change event.</summary>
        /// <param name="propertyName">Property name.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>Sets a field and raises a change notification when the value differs.</summary>
        /// <typeparam name="T">Property type.</typeparam>
        /// <param name="field">Backing field.</param>
        /// <param name="value">New value.</param>
        /// <param name="propertyName">Property name.</param>
        /// <returns>True when changed.</returns>
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) { return false; }
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

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

        private double _x;
        private double _y;
        private double _width;
        private double _height;
        private double _rotation;
        private double _opacity = 1.0;
        private bool _isVisible = true;
        private bool _isLocked;

        /// <summary>X position in millimeters.</summary>
        public double X { get => _x; set => Set(ref _x, value); }

        /// <summary>Y position in millimeters.</summary>
        public double Y { get => _y; set => Set(ref _y, value); }

        /// <summary>Width in millimeters.</summary>
        public double Width { get => _width; set => Set(ref _width, value); }

        /// <summary>Height in millimeters.</summary>
        public double Height { get => _height; set => Set(ref _height, value); }

        /// <summary>Rotation in degrees (clockwise).</summary>
        public double Rotation { get => _rotation; set => Set(ref _rotation, value); }

        /// <summary>Opacity from 0.0 (transparent) to 1.0 (opaque).</summary>
        public double Opacity { get => _opacity; set => Set(ref _opacity, value); }

        /// <summary>Whether the element is rendered.</summary>
        public bool IsVisible { get => _isVisible; set => Set(ref _isVisible, value); }

        /// <summary>Whether the element can be moved or edited on the canvas.</summary>
        public bool IsLocked { get => _isLocked; set => Set(ref _isLocked, value); }

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
