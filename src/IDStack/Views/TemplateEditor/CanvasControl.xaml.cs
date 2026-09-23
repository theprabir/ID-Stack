using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IDStack.Core.Models.Elements;
using IDStack.ViewModels.TemplateEditor;

namespace IDStack.Views.TemplateEditor
{
    /// <summary>
    /// Interactive canvas surface: renders the current side's elements in millimeter
    /// scale, supports selection and mouse dragging, and draws a subtle grid.
    /// </summary>
    public class CanvasControl : FrameworkElement
    {
        private readonly VisualCollection _visuals;
        private readonly Canvas _surface;
        private TemplateEditorViewModel _editor;
        private CanvasElement _dragElement;
        private Point _dragOrigin;
        private bool _dragging;

        /// <summary>
        /// Creates the canvas control and wires rendering and input.
        /// </summary>
        public CanvasControl()
        {
            _visuals = new VisualCollection(this);
            _surface = new Canvas
            {
                Background = Brushes.White,
                AllowDrop = false
            };
            _visuals.Add(_surface);
            ClipToBounds = true;

            MouseLeftButtonDown += OnMouseButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseButtonUp;
            SizeChanged += (_, __) => RenderSurface();
        }

        /// <summary>Attaches the canvas to an editor view model.</summary>
        /// <param name="editor">Editor view model.</param>
        public void Attach(TemplateEditorViewModel editor)
        {
            if (_editor != null)
            {
                _editor.PropertyChanged -= OnEditorPropertyChanged;
                if (_editor.CurrentSide != null)
                {
                    _editor.CurrentSide.Elements.CollectionChanged -= OnElementsChanged;
                }
            }

            _editor = editor;
            _editor.PropertyChanged += OnEditorPropertyChanged;
            if (_editor.CurrentSide != null)
            {
                _editor.CurrentSide.Elements.CollectionChanged += OnElementsChanged;
            }
            RenderSurface();
        }

        private void OnEditorPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TemplateEditorViewModel.CurrentSide) ||
                e.PropertyName == nameof(TemplateEditorViewModel.ZoomLevel))
            {
                if (_editor?.CurrentSide != null)
                {
                    _editor.CurrentSide.Elements.CollectionChanged -= OnElementsChanged;
                    _editor.CurrentSide.Elements.CollectionChanged += OnElementsChanged;
                }
                RenderSurface();
            }
            else if (e.PropertyName == nameof(TemplateEditorViewModel.SelectedElement))
            {
                RenderSurface();
            }
        }

        private void OnElementsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RenderSurface();
        }

        private void RenderSurface()
        {
            if (_editor == null || _editor.CurrentSide == null)
            {
                return;
            }

            _surface.Children.Clear();

            var scale = _editor.ZoomLevel;
            var side = _editor.CurrentSide;

            _surface.Width = side.CanvasWidth * scale;
            _surface.Height = side.CanvasHeight * scale;

            foreach (var element in side.Elements)
            {
                var visual = CanvasElementRenderer.CreateVisual(element, scale);
                if (visual == null)
                {
                    continue;
                }

                if (element == _editor.SelectedElement)
                {
                    var highlight = new Border
                    {
                        BorderBrush = Brushes.Orange,
                        BorderThickness = new Thickness(1),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(highlight, element.X * scale - 2);
                    Canvas.SetTop(highlight, element.Y * scale - 2);
                    highlight.Width = element.Width * scale + 4;
                    highlight.Height = element.Height * scale + 4;
                    _surface.Children.Add(highlight);
                }

                visual.Tag = element;
                _surface.Children.Add(visual);
            }

            InvalidateVisual();
        }

        private void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_editor == null)
            {
                return;
            }

            var position = e.GetPosition(_surface);
            var hit = HitTestElement(position);

            _editor.SelectedElement = hit;
            if (hit != null && !hit.IsLocked)
            {
                _dragElement = hit;
                _dragOrigin = position;
                _dragging = true;
                CaptureMouse();
            }
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || _dragElement == null || _editor == null)
            {
                return;
            }

            var position = e.GetPosition(_surface);
            var scale = _editor.ZoomLevel;
            var dx = (position.X - _dragOrigin.X) / scale;
            var dy = (position.Y - _dragOrigin.Y) / scale;

            _dragElement.X = Math.Max(0, Math.Min(_editor.CurrentSide.CanvasWidth - _dragElement.Width, _dragElement.X + dx));
            _dragElement.Y = Math.Max(0, Math.Min(_editor.CurrentSide.CanvasHeight - _dragElement.Height, _dragElement.Y + dy));

            _dragOrigin = position;
            RenderSurface();
        }

        private void OnMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragging)
            {
                _dragging = false;
                _dragElement = null;
                ReleaseMouseCapture();
                _editor?.CommitChange();
            }
        }

        private CanvasElement HitTestElement(Point surfacePosition)
        {
            if (_editor?.CurrentSide == null)
            {
                return null;
            }

            var scale = _editor.ZoomLevel;
            CanvasElement hit = null;

            // Iterate front-to-back (end of list is topmost).
            for (var i = _editor.CurrentSide.Elements.Count - 1; i >= 0; i--)
            {
                var candidate = _editor.CurrentSide.Elements[i];
                if (!candidate.IsVisible)
                {
                    continue;
                }

                var x = candidate.X * scale;
                var y = candidate.Y * scale;
                var w = Math.Max(candidate.Width * scale, 6);
                var h = Math.Max(candidate.Height * scale, 6);

                if (surfacePosition.X >= x - 2 && surfacePosition.X <= x + w + 2 &&
                    surfacePosition.Y >= y - 2 && surfacePosition.Y <= y + h + 2)
                {
                    hit = candidate;
                    break;
                }
            }

            return hit;
        }

        /// <inheritdoc />
        protected override int VisualChildrenCount => _visuals.Count;

        /// <inheritdoc />
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _visuals.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _visuals[index];
        }
    }
}
