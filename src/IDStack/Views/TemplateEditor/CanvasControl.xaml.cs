using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.ViewModels.TemplateEditor;

namespace IDStack.Views.TemplateEditor
{
    /// <summary>
    /// Interactive design surface with a dark surround, card, rulers, grid,
    /// selection handles with 8-way resize, drag-move, pan, and zoom-at-cursor.
    /// All drawing happens in one OnRender pass; hit testing is model-based.
    /// </summary>
    public class CanvasControl : FrameworkElement
    {
        private TemplateEditorViewModel _editor;
        private ISettingsService _settings;

        // Interaction state
        private Point _lastMouse;
        private bool _panning;
        private bool _draggingMove;
        private int _dragHandle = -1;   // -1 none, 0 move, 1..8 handles
        private CanvasElement _dragElement;
        private double _dragOrigX, _dragOrigY, _dragOrigW, _dragOrigH;
        private Point _dragStart;
        private bool _spaceDown;
        private System.Windows.Controls.Primitives.Popup _inlinePopup;
        private System.Windows.Controls.TextBox _inlineEditor;
        private Core.Models.Elements.CanvasElement _inlineElement;

        // View state
        private double _panX = 40;
        private double _panY = 40;
        private bool _centered;
        private Size _lastViewport;
        private bool _wheelZooming;

        // Colors
        private static readonly Color SurroundColor = Color.FromRgb(0x21, 0x25, 0x2B);
        private static readonly Color CardBorderColor = Color.FromRgb(0x0F, 0x17, 0x2A);
        private static readonly Color GridColor = Color.FromArgb(50, 0x64, 0x74, 0x8B);
        private static readonly Color GridMajorColor = Color.FromArgb(80, 0x64, 0x74, 0x8B);
        private static readonly Color RulerColor = Color.FromRgb(0x2D, 0x33, 0x3D);
        private static readonly Color RulerTextColor = Color.FromRgb(0x94, 0xA3, 0xB8);
        private static readonly Color RulerTickColor = Color.FromRgb(0x64, 0x74, 0x8B);
        private static readonly Color SelectionColor = Color.FromRgb(0x38, 0x8B, 0xFF);
        private static readonly Color HandleColor = Colors.White;
        private static readonly Color HandleBorderColor = Color.FromRgb(0x38, 0x8B, 0xFF);
        private const double RulerSize = 20;
        private const double HandleSize = 8;

        /// <summary>
        /// Creates the canvas control.
        /// </summary>
        public CanvasControl()
        {
            ClipToBounds = true;
            Focusable = true;
            Focus();

            MouseLeftButtonDown += OnMouseLeftDown;
            PreviewMouseLeftButtonDown += OnPreviewMouseDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftUp;
            MouseWheel += OnMouseWheel;
            MouseRightButtonDown += OnMouseRightDown;
            MouseRightButtonUp += OnMouseRightUp;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            LostKeyboardFocus += (_, __) => { _spaceDown = false; };
        }

        /// <summary>
        /// Attaches the control to the editor view model and settings.
        /// </summary>
        /// <param name="editor">Editor view model.</param>
        /// <param name="settings">Settings service for grid options.</param>
        public void Attach(TemplateEditorViewModel editor, ISettingsService settings)
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
            _settings = settings;
            _editor.PropertyChanged += OnEditorPropertyChanged;
            HookElements();

            InvalidateVisual();
        }

        private void HookElements()
        {
            if (_editor?.CurrentSide == null)
            {
                return;
            }
            _editor.CurrentSide.Elements.CollectionChanged -= OnElementsChanged;
            _editor.CurrentSide.Elements.CollectionChanged += OnElementsChanged;
            foreach (var element in _editor.CurrentSide.Elements)
            {
                element.PropertyChanged -= OnElementPropertyChanged;
                element.PropertyChanged += OnElementPropertyChanged;
            }
        }

        private void OnElementsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (CanvasElement item in e.OldItems)
                {
                    item.PropertyChanged -= OnElementPropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (CanvasElement item in e.NewItems)
                {
                    item.PropertyChanged += OnElementPropertyChanged;
                }
            }
            InvalidateVisual();
        }

        private void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InvalidateVisual();
        }

        private void OnEditorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TemplateEditorViewModel.CurrentSide))
            {
                HookElements();
                CenterCard();
                InvalidateVisual();
            }
            else if (e.PropertyName == nameof(TemplateEditorViewModel.SelectedElement))
            {
                InvalidateVisual();
            }
            else if (e.PropertyName == nameof(TemplateEditorViewModel.ZoomLevel) && !_wheelZooming)
            {
                // Toolbar / keyboard zoom: keep the card centered in the viewport
                // so the design never disappears off-screen.
                CenterCard();
                InvalidateVisual();
            }
        }

        /// <summary>Centers the card horizontally and vertically in the viewport.</summary>
        private void CenterCard()
        {
            CenterCard(ActualWidth, ActualHeight);
        }

        /// <summary>Centers the card using an explicit viewport size (during arrange).</summary>
        private void CenterCard(double viewportWidth, double viewportHeight)
        {
            var card = CardRect;
            _panX = (viewportWidth - card.Width) / 2.0;
            _panY = RulerSize + Math.Max(20, (viewportHeight - RulerSize - card.Height) / 2.0);
        }

        private double Scale => _editor?.ZoomLevel ?? 4.0;

        private bool ShowGrid => _settings?.Settings.ShowGrid ?? true;
        private bool SnapToGrid => _settings?.Settings.SnapToGrid ?? true;
        private double GridSizeMm => Math.Max(0.25, _settings?.Settings.GridSizeMm ?? 1.0);

        // Card rect in control coordinates
        private Rect CardRect => new Rect(
            _panX, _panY,
            (_editor?.CurrentSide?.CanvasWidth ?? 85.6) * Scale,
            (_editor?.CurrentSide?.CanvasHeight ?? 54.0) * Scale);

        // ---------- Coordinate helpers ----------

        private Point ControlToCardMm(Point p)
        {
            var card = CardRect;
            return new Point((p.X - card.X) / Scale, (p.Y - card.Y) / Scale);
        }

        private double Snap(double mm)
        {
            if (!SnapToGrid) { return mm; }
            var g = GridSizeMm;
            return Math.Round(mm / g) * g;
        }

        // ---------- Rendering ----------

        /// <inheritdoc />
        protected override void OnRender(DrawingContext dc)
        {
            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 0 || h <= 0 || _editor?.CurrentSide == null)
            {
                return;
            }

            // Surround
            dc.DrawRectangle(new SolidColorBrush(SurroundColor), null, new Rect(0, 0, w, h));

            var card = CardRect;
            var clip = new RectangleGeometry(new Rect(
                Math.Max(card.X, RulerSize), Math.Max(card.Y, RulerSize),
                Math.Max(0, w - Math.Max(card.X, RulerSize)),
                Math.Max(0, h - Math.Max(card.Y, RulerSize))));

            // Card shadow + card
            dc.DrawRoundedRectangle(
                new SolidColorBrush(Colors.Black), null,
                new Rect(card.X + 3, card.Y + 3, card.Width, card.Height), 2, 2);
            dc.DrawRoundedRectangle(
                new SolidColorBrush(ParseColor(_editor.CurrentSide.BackgroundColor, Colors.White)),
                new Pen(new SolidColorBrush(CardBorderColor), 1),
                card, 2, 2);

            dc.PushTransform(new TranslateTransform(card.X, card.Y));
            dc.PushClip(clip);
            DrawElements(dc);
            dc.Pop();
            dc.Pop();

            if (ShowGrid)
            {
                DrawGrid(dc, w, h, card);
            }

            DrawRulers(dc, w, h, card);

            if (_editor.SelectedElement != null)
            {
                DrawSelection(dc, _editor.SelectedElement);
            }
        }

        private void DrawElements(DrawingContext dc)
        {
            foreach (var element in _editor.CurrentSide.Elements)
            {
                if (!element.IsVisible)
                {
                    continue;
                }

                var x = element.X * Scale;
                var y = element.Y * Scale;
                var wd = element.Width * Scale;
                var ht = element.Height * Scale;
                var rect = new Rect(x, y, wd, ht);

                dc.PushTransform(new RotateTransform(
                    element.Rotation, x + wd / 2, y + ht / 2));
                dc.PushOpacity(element.Opacity);

                switch (element.ElementType)
                {
                    case ElementType.Text: DrawText(dc, (TextElement)element, rect); break;
                    case ElementType.Placeholder: DrawPlaceholder(dc, (PlaceholderElement)element, rect); break;
                    case ElementType.Image: DrawImage(dc, rect); break;
                    case ElementType.Shape: DrawShape(dc, (ShapeElement)element, rect); break;
                    case ElementType.Barcode: DrawBarcode(dc, (BarcodeElement)element, rect); break;
                }

                dc.Pop(); // opacity
                dc.Pop(); // rotate
            }
        }

        private void DrawText(DrawingContext dc, TextElement text, Rect rect)
        {
            var formatted = new FormattedText(
                text.Text ?? string.Empty,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily(text.FontFamily), ToFontStyle(text.IsItalic, text.IsUnderline), text.IsBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                text.FontSize * 96.0 / 72.0,
                new SolidColorBrush(ParseColor(text.TextColor, Colors.Black)),
                1.25);

            var tx = rect.X;
            if (text.Alignment == "Center") { tx = rect.X + (rect.Width - formatted.Width) / 2; }
            else if (text.Alignment == "Right") { tx = rect.X + rect.Width - formatted.Width; }

            dc.DrawText(formatted, new Point(tx, rect.Y));
        }

        private void DrawPlaceholder(DrawingContext dc, PlaceholderElement p, Rect rect)
        {
            var label = string.IsNullOrEmpty(p.ColumnName)
                ? (p.Text ?? "{{Column}}")
                : "{{" + p.ColumnName + "}}";

            var formatted = new FormattedText(
                label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily(p.FontFamily), FontStyles.Normal, p.IsBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                p.FontSize * 96.0 / 72.0,
                new SolidColorBrush(ParseColor(p.TextColor, Colors.Black)),
                1.25);

            var pen = new Pen(new SolidColorBrush(SelectionColor), 1)
            {
                DashStyle = DashStyles.Dash
            };
            dc.DrawRectangle(null, pen, rect);
            dc.DrawText(formatted, new Point(rect.X + 2, rect.Y));
        }

        private void DrawImage(DrawingContext dc, Rect rect)
        {
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                new Pen(new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)), 1),
                rect);
            var label = new FormattedText(
                "IMG",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                1.25);
            dc.DrawText(label, new Point(
                rect.X + (rect.Width - label.Width) / 2,
                rect.Y + (rect.Height - label.Height) / 2));
        }

        private void DrawShape(DrawingContext dc, ShapeElement shape, Rect rect)
        {
            var fill = ParseBrush(shape.FillColor, null);
            var stroke = new Pen(
                ParseBrush(shape.StrokeColor, Brushes.Gray),
                Math.Max(1, shape.StrokeWidth * Scale));

            switch (shape.ShapeKind)
            {
                case ShapeKind.Ellipse:
                    dc.DrawEllipse(fill, stroke, new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), rect.Width / 2, rect.Height / 2);
                    break;
                case ShapeKind.Line:
                    dc.DrawLine(new Pen(ParseBrush(shape.StrokeColor, Brushes.Gray), Math.Max(1, shape.StrokeWidth * Scale)),
                        new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                    break;
                default:
                    dc.DrawRoundedRectangle(fill, stroke, rect, shape.CornerRadius * Scale, shape.CornerRadius * Scale);
                    break;
            }
        }

        private void DrawBarcode(DrawingContext dc, BarcodeElement barcode, Rect rect)
        {
            dc.DrawRectangle(ParseBrush(barcode.BackgroundColor, Brushes.White),
                new Pen(Brushes.Gray, 1), rect);

            // Stylized barcode bars
            var barBrush = ParseBrush(barcode.ForegroundColor, Brushes.Black);
            var barCount = barcode.BarcodeType == BarcodeType.QR ? 9 : 24;
            var barWidth = rect.Width / (barCount * 2.0);
            var seed = Math.Abs((barcode.Data ?? barcode.BarcodeType.ToString()).GetHashCode());
            for (var i = 0; i < barCount; i++)
            {
                seed = seed * 1103515245 + 12345;
                if ((seed >> 16 & 3) == 0) { continue; }
                var bx = rect.X + i * barWidth * 2;
                dc.DrawRectangle(barBrush, null,
                    barcode.BarcodeType == BarcodeType.QR
                        ? new Rect(bx, rect.Y + (i % 3) * rect.Height / 9.0, barWidth, rect.Height / 3.0)
                        : new Rect(bx, rect.Y, barWidth, rect.Height));
            }
        }

        private void DrawGrid(DrawingContext dc, double w, double h, Rect card)
        {
            var gridPen = new Pen(new SolidColorBrush(GridColor), 1);
            gridPen.Freeze();
            var majorPen = new Pen(new SolidColorBrush(GridMajorColor), 1);
            majorPen.Freeze();

            var g = GridSizeMm * Scale;
            if (g < 4) { g *= 5; } // avoid sub-pixel noise at low zoom

            // Vertical lines across the visible band around the card
            var startX = card.X - Math.Ceiling(card.X / g) * g;
            for (var x = startX; x < w; x += g)
            {
                if (x < RulerSize) { continue; }
                var isMajor = Math.Abs((x - card.X) % (g * 5)) < 0.01;
                dc.DrawLine(isMajor ? majorPen : gridPen, new Point(x, Math.Max(card.Y, RulerSize)), new Point(x, h));
            }
            var startY = card.Y - Math.Ceiling(card.Y / g) * g;
            for (var y = startY; y < h; y += g)
            {
                if (y < RulerSize) { continue; }
                var isMajor = Math.Abs((y - card.Y) % (g * 5)) < 0.01;
                dc.DrawLine(isMajor ? majorPen : gridPen, new Point(Math.Max(card.X, RulerSize), y), new Point(w, y));
            }
        }

        private void DrawRulers(DrawingContext dc, double w, double h, Rect card)
        {
            var rulerBrush = new SolidColorBrush(RulerColor);
            var tickPen = new Pen(new SolidColorBrush(RulerTickColor), 1);
            var textBrush = new SolidColorBrush(RulerTextColor);
            var typeface = new Typeface("Segoe UI");

            dc.DrawRectangle(rulerBrush, null, new Rect(0, 0, w, RulerSize));
            dc.DrawRectangle(rulerBrush, null, new Rect(0, 0, RulerSize, h));

            var stepMm = Scale >= 8 ? 1 : Scale >= 4 ? 5 : Scale >= 2 ? 10 : 25;
            var originMmX = -_panX / Scale;
            var originMmY = -_panY / Scale;

            // Horizontal ruler ticks
            var firstTick = Math.Floor(originMmX / stepMm) * stepMm;
            for (var mm = firstTick; ; mm += stepMm)
            {
                var x = card.X + mm * Scale;
                if (x < RulerSize) { continue; }
                if (x > w) { break; }
                var isMajor = (int)Math.Round(mm) % (stepMm * 5) == 0;
                dc.DrawLine(tickPen, new Point(x, isMajor ? 6 : 13), new Point(x, RulerSize));
                if (isMajor)
                {
                    var ft = new FormattedText(mm.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, 8, textBrush, 1.25);
                    dc.DrawText(ft, new Point(x + 2, 1));
                }
            }

            // Vertical ruler ticks
            var firstTickY = Math.Floor(originMmY / stepMm) * stepMm;
            for (var mm = firstTickY; ; mm += stepMm)
            {
                var y = card.Y + mm * Scale;
                if (y < RulerSize) { continue; }
                if (y > h) { break; }
                var isMajor = (int)Math.Round(mm) % (stepMm * 5) == 0;
                dc.DrawLine(tickPen, new Point(isMajor ? 6 : 13, y), new Point(RulerSize, y));
                if (isMajor)
                {
                    var ft = new FormattedText(mm.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, 8, textBrush, 1.25);
                    dc.PushTransform(new RotateTransform(-90));
                    dc.DrawText(ft, new Point(-y, 2));
                    dc.Pop();
                }
            }
        }

        private void DrawSelection(DrawingContext dc, CanvasElement element)
        {
            var card = CardRect;
            var rect = new Rect(
                card.X + element.X * Scale,
                card.Y + element.Y * Scale,
                element.Width * Scale,
                element.Height * Scale);

            var selPen = new Pen(new SolidColorBrush(SelectionColor), 1)
            {
                DashStyle = DashStyles.Dash
            };
            dc.DrawRectangle(null, selPen, rect);

            // 8 handles: corners + mid-edges. 1=TL 2=TR 3=BL 4=BR 5=T 6=B 7=L 8=R
            for (var handle = 1; handle <= 8; handle++)
            {
                var hp = GetHandlePoint(rect, handle);
                var hr = new Rect(hp.X - HandleSize / 2, hp.Y - HandleSize / 2, HandleSize, HandleSize);
                dc.DrawRectangle(new SolidColorBrush(HandleColor),
                    new Pen(new SolidColorBrush(HandleBorderColor), 1.5), hr);
            }
        }

        private static Point GetHandlePoint(Rect rect, int handle)
        {
            switch (handle)
            {
                case 1: return new Point(rect.Left, rect.Top);
                case 2: return new Point(rect.Right, rect.Top);
                case 3: return new Point(rect.Left, rect.Bottom);
                case 4: return new Point(rect.Right, rect.Bottom);
                case 5: return new Point(rect.X + rect.Width / 2, rect.Top);
                case 6: return new Point(rect.X + rect.Width / 2, rect.Bottom);
                case 7: return new Point(rect.Left, rect.Y + rect.Height / 2);
                case 8: return new Point(rect.Right, rect.Y + rect.Height / 2);
                default: return new Point(rect.X, rect.Y);
            }
        }

        // ---------- Hit testing ----------

        private int HitHandle(Point p)
        {
            if (_editor.SelectedElement == null)
            {
                return -1;
            }

            var card = CardRect;
            var rect = new Rect(
                card.X + _editor.SelectedElement.X * Scale,
                card.Y + _editor.SelectedElement.Y * Scale,
                _editor.SelectedElement.Width * Scale,
                _editor.SelectedElement.Height * Scale);

            for (var handle = 1; handle <= 8; handle++)
            {
                var hp = GetHandlePoint(rect, handle);
                if (Math.Abs(p.X - hp.X) <= HandleSize / 2 + 2 && Math.Abs(p.Y - hp.Y) <= HandleSize / 2 + 2)
                {
                    return handle;
                }
            }
            return -1;
        }

        private CanvasElement HitElement(Point p)
        {
            if (_editor?.CurrentSide == null)
            {
                return null;
            }

            var card = CardRect;
            var local = new Point(p.X - card.X, p.Y - card.Y);

            for (var i = _editor.CurrentSide.Elements.Count - 1; i >= 0; i--)
            {
                var candidate = _editor.CurrentSide.Elements[i];
                if (!candidate.IsVisible)
                {
                    continue;
                }

                var rect = new Rect(
                    candidate.X * Scale,
                    candidate.Y * Scale,
                    Math.Max(candidate.Width * Scale, 8),
                    Math.Max(candidate.Height * Scale, 8));
                if (rect.Contains(local))
                {
                    return candidate;
                }
            }
            return null;
        }

        // ---------- Input ----------

        private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            var p = e.GetPosition(this);
            _lastMouse = p;

            if (_editor?.CurrentSide == null)
            {
                return;
            }

            // Ruler area: ignore.
            if (p.X < RulerSize || p.Y < RulerSize)
            {
                return;
            }

            var handle = HitHandle(p);
            if (handle > 0 && _editor.SelectedElement != null)
            {
                _dragHandle = handle;
                _dragElement = _editor.SelectedElement;
                _dragOrigX = _dragElement.X;
                _dragOrigY = _dragElement.Y;
                _dragOrigW = _dragElement.Width;
                _dragOrigH = _dragElement.Height;
                _dragStart = p;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            var hit = HitElement(p);
            _editor.SelectedElement = hit;

            if (hit != null && !hit.IsLocked)
            {
                _dragHandle = 0; // move
                _dragElement = hit;
                _dragOrigX = hit.X;
                _dragOrigY = hit.Y;
                _dragStart = p;
                CaptureMouse();
            }
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(this);
            var delta = p - _lastMouse;

            if (_panning)
            {
                _panX += delta.X;
                _panY += delta.Y;
                _lastMouse = p;
                InvalidateVisual();
                return;
            }

            if (_dragHandle < 0 || _dragElement == null)
            {
                return;
            }

            var dMmX = delta.X / Scale;
            var dMmY = delta.Y / Scale;

            if (_dragHandle == 0)
            {
                _dragElement.X = Math.Max(0, Math.Min(_editor.CurrentSide.CanvasWidth - _dragElement.Width, _dragOrigX + dMmX));
                _dragElement.Y = Math.Max(0, Math.Min(_editor.CurrentSide.CanvasHeight - _dragElement.Height, _dragOrigY + dMmY));
                if (SnapToGrid)
                {
                    _dragElement.X = Snap(_dragElement.X);
                    _dragElement.Y = Snap(_dragElement.Y);
                }
            }
            else
            {
                ResizeFromHandle(dMmX, dMmY);
            }

            _lastMouse = p;
            InvalidateVisual();
            e.Handled = true;
        }

        private void ResizeFromHandle(double dx, double dy)
        {
            var el = _dragElement;
            var x = _dragOrigX;
            var y = _dragOrigY;
            var w = _dragOrigW;
            var h = _dragOrigH;

            const double min = 1.0;
            switch (_dragHandle)
            {
                case 1: // TL
                    w -= dx; h -= dy; x += dx; y += dy;
                    break;
                case 2: // TR
                    w += dx; h -= dy; y += dy;
                    break;
                case 3: // BL
                    w -= dx; h += dy; x += dx;
                    break;
                case 4: // BR
                    w += dx; h += dy;
                    break;
                case 5: // T
                    h -= dy; y += dy;
                    break;
                case 6: // B
                    h += dy;
                    break;
                case 7: // L
                    w -= dx; x += dx;
                    break;
                case 8: // R
                    w += dx;
                    break;
            }

            if (w < min)
            {
                if (_dragHandle == 1 || _dragHandle == 3 || _dragHandle == 7) { x -= min - w; }
                w = min;
            }
            if (h < min)
            {
                if (_dragHandle == 1 || _dragHandle == 2 || _dragHandle == 5) { y -= min - h; }
                h = min;
            }

            el.X = Math.Max(0, Snap(x));
            el.Y = Math.Max(0, Snap(y));
            el.Width = Snap(w);
            el.Height = Snap(h);
        }

        private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragHandle >= 0)
            {
                _dragHandle = -1;
                _dragElement = null;
                ReleaseMouseCapture();
                _editor?.CommitChange();
                e.Handled = true;
            }
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OnMouseDoubleClick(sender, e);
            }
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_editor?.SelectedElement == null)
            {
                return;
            }

            var hit = HitElement(e.GetPosition(this));
            if (hit == null || hit != _editor.SelectedElement)
            {
                return;
            }

            if (hit is TextElement || hit is Core.Models.Elements.PlaceholderElement)
            {
                BeginInlineTextEdit(hit);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Opens a borderless TextBox over the element so its text can be edited
        /// directly on the canvas, Photoshop-style.
        /// </summary>
        private void BeginInlineTextEdit(Core.Models.Elements.CanvasElement element)
        {
            if (_inlineEditor != null)
            {
                CloseInlineTextEdit(false);
            }

            var card = CardRect;
            var rect = new Rect(
                card.X + element.X * Scale,
                card.Y + element.Y * Scale,
                Math.Max(element.Width * Scale, 60),
                Math.Max(element.Height * Scale, 22));

            _inlineElement = element;
            _inlineEditor = new System.Windows.Controls.TextBox
            {
                Text = (element as TextElement)?.Text
                       ?? (element as Core.Models.Elements.PlaceholderElement)?.Text
                       ?? string.Empty,
                FontSize = Math.Max(8, ((element as TextElement)?.FontSize ??
                                        (element as Core.Models.Elements.PlaceholderElement)?.FontSize ?? 10) * Scale * 0.75),
                BorderBrush = new SolidColorBrush(SelectionColor),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                Padding = new Thickness(1, 0, 1, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _inlineEditor.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    CloseInlineTextEdit(true);
                    args.Handled = true;
                }
                else if (args.Key == Key.Escape)
                {
                    CloseInlineTextEdit(false);
                    args.Handled = true;
                }
            };
            _inlineEditor.LostFocus += (s, args) => CloseInlineTextEdit(true);

            // Float a popup over the element — the robust way to overlay a control
            // on a custom-rendered FrameworkElement.
            _inlinePopup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = this,
                Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint,
                HorizontalOffset = rect.X,
                VerticalOffset = rect.Y,
                Width = rect.Width,
                Height = rect.Height,
                AllowsTransparency = true,
                StaysOpen = true,
                Child = _inlineEditor,
                IsOpen = true
            };
            _inlineEditor.Focus();
            _inlineEditor.SelectAll();
        }

        private void CloseInlineTextEdit(bool apply)
        {
            if (_inlinePopup == null)
            {
                return;
            }

            var editor = _inlineEditor;
            var element = _inlineElement;
            _inlinePopup.IsOpen = false;
            _inlinePopup = null;
            _inlineEditor = null;
            _inlineElement = null;

            if (apply && editor != null && element != null)
            {
                if (element is TextElement text)
                {
                    text.Text = editor.Text;
                }
                else if (element is Core.Models.Elements.PlaceholderElement placeholder)
                {
                    placeholder.Text = editor.Text;
                }
                _editor?.CommitChange();
            }
            InvalidateVisual();
        }

        private void OnMouseRightDown(object sender, MouseButtonEventArgs e)
        {
            _panning = true;
            _lastMouse = e.GetPosition(this);
            CaptureMouse();
            Cursor = Cursors.ScrollAll;
            e.Handled = true;
        }

        private void OnMouseRightUp(object sender, MouseButtonEventArgs e)
        {
            _panning = false;
            ReleaseMouseCapture();
            Cursor = Cursors.Cross;
            e.Handled = true;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_editor == null)
            {
                return;
            }

            // Zoom at cursor: keep the point under the mouse stationary.
            var p = e.GetPosition(this);
            var before = ControlToCardMm(p);
            _wheelZooming = true;
            try
            {
                _editor.ZoomLevel += e.Delta > 0 ? 0.8 : -0.8;
            }
            finally
            {
                _wheelZooming = false;
            }
            var card = CardRect;
            _panX = p.X - before.X * Scale;
            _panY = p.Y - before.Y * Scale;
            InvalidateVisual();
            e.Handled = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_editor == null) { return; }

            if (e.Key == Key.Space) { _spaceDown = true; }
            switch (e.Key)
            {
                case Key.Left: Nudge(-1, 0, e.KeyboardDevice.Modifiers); e.Handled = true; break;
                case Key.Right: Nudge(1, 0, e.KeyboardDevice.Modifiers); e.Handled = true; break;
                case Key.Up: Nudge(0, -1, e.KeyboardDevice.Modifiers); e.Handled = true; break;
                case Key.Down: Nudge(0, 1, e.KeyboardDevice.Modifiers); e.Handled = true; break;
                case Key.Delete: _editor.DeleteSelected(); e.Handled = true; break;
                case Key.D0:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) { _editor.ZoomLevel = 4.0; _panX = 40; _panY = 40; e.Handled = true; }
                    break;
                case Key.D1:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) { _editor.ZoomLevel = 3.7795; e.Handled = true; }
                    break;
                case Key.OemPlus:
                case Key.Add:
                    _editor.ZoomLevel += 0.8; e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    _editor.ZoomLevel -= 0.8; e.Handled = true;
                    break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) { _spaceDown = false; }
        }

        private void Nudge(double dx, double dy, ModifierKeys modifiers)
        {
            if (_editor?.SelectedElement == null) { return; }

            var step = modifiers == ModifierKeys.Shift ? 10 : 1;
            var el = _editor.SelectedElement;
            if (!el.IsLocked)
            {
                el.X = Math.Max(0, Math.Min(_editor.CurrentSide.CanvasWidth - el.Width, el.X + dx * step));
                el.Y = Math.Max(0, Math.Min(_editor.CurrentSide.CanvasHeight - el.Height, el.Y + dy * step));
                _editor.CommitChange();
            }
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch (FormatException)
            {
                return fallback;
            }
        }

        private static Brush ParseBrush(string hex, Brush fallback)
        {
            try
            {
                var brush = new SolidColorBrush(ParseColor(hex, Colors.Transparent));
                brush.Freeze();
                return brush;
            }
            catch (FormatException)
            {
                return fallback;
            }
        }

        private static FontStyle ToFontStyle(bool italic, bool underline)
        {
            return italic ? FontStyles.Italic : FontStyles.Normal;
        }

        /// <summary>
        /// Fill whatever space is offered — the control draws its own surround,
        /// rulers and pan; it is sized by its container, not by content.
        /// </summary>
        /// <param name="availableSize">Offered size.</param>
        /// <returns>The offered finite size, or zero for infinite constraints.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
            double height = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;
            return new Size(width, height);
 }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Keep the card centered whenever the viewport size changes
            // (first layout, window resize, panel resize).
            if (finalSize != _lastViewport)
            {
                _lastViewport = finalSize;
                if (finalSize.Width > 50 && finalSize.Height > 50)
                {
                    CenterCard(finalSize.Width, finalSize.Height);
                    _centered = true;
                }
                InvalidateVisual();
            }
            return finalSize;
        }
    }
}
