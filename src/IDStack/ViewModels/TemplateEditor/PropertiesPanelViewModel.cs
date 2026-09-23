using System;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;

namespace IDStack.ViewModels.TemplateEditor
{
    /// <summary>
    /// View model for the context-sensitive properties panel.
    /// Edits flow directly into the selected element model; every change is
    /// committed to editor history in bulk on panel focus loss via Commit.
    /// </summary>
    public class PropertiesPanelViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localization;
        private TemplateEditorViewModel _editor;
        private CanvasElement _element;

        /// <summary>
        /// Creates the properties panel view model.
        /// </summary>
        /// <param name="localization">Localization service.</param>
        public PropertiesPanelViewModel(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        /// <summary>Attaches the panel to an editor instance.</summary>
        /// <param name="editor">Editor view model.</param>
        public void Attach(TemplateEditorViewModel editor)
        {
            _editor = editor;
            _editor.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TemplateEditorViewModel.SelectedElement) ||
                    e.PropertyName == nameof(TemplateEditorViewModel.CurrentSide))
                {
                    Element = _editor.SelectedElement;
                }
            };
            Element = _editor.SelectedElement;
        }

        /// <summary>The selected element being edited (null when nothing selected).</summary>
        public CanvasElement Element
        {
            get { return _element; }
            private set
            {
                if (SetProperty(ref _element, value))
                {
                    OnPropertyChanged(nameof(HasElement));
                    OnPropertyChanged(nameof(X));
                    OnPropertyChanged(nameof(Y));
                    OnPropertyChanged(nameof(Width));
                    OnPropertyChanged(nameof(Height));
                    OnPropertyChanged(nameof(Rotation));
                    OnPropertyChanged(nameof(OpacityPercent));
                    OnPropertyChanged(nameof(IsTextElement));
                    OnPropertyChanged(nameof(IsShapeElement));
                    OnPropertyChanged(nameof(IsPlaceholderElement));
                }
            }
        }

        /// <summary>Whether an element is selected for editing.</summary>
        public bool HasElement => Element != null;

        /// <summary>X position in millimeters.</summary>
        public double X
        {
            get { return Element?.X ?? 0; }
            set { if (Element != null && Element.X != value) { Element.X = value; Commit(); } }
        }

        /// <summary>Y position in millimeters.</summary>
        public double Y
        {
            get { return Element?.Y ?? 0; }
            set { if (Element != null && Element.Y != value) { Element.Y = value; Commit(); } }
        }

        /// <summary>Width in millimeters.</summary>
        public double Width
        {
            get { return Element?.Width ?? 0; }
            set { if (Element != null && Element.Width != value) { Element.Width = Math.Max(0.5, value); Commit(); } }
        }

        /// <summary>Height in millimeters.</summary>
        public double Height
        {
            get { return Element?.Height ?? 0; }
            set { if (Element != null && Element.Height != value) { Element.Height = Math.Max(0.5, value); Commit(); } }
        }

        /// <summary>Rotation in degrees.</summary>
        public double Rotation
        {
            get { return Element?.Rotation ?? 0; }
            set { if (Element != null && Element.Rotation != value) { Element.Rotation = value; Commit(); } }
        }

        /// <summary>Opacity as a percentage (0-100).</summary>
        public double OpacityPercent
        {
            get { return Math.Round((Element?.Opacity ?? 1.0) * 100.0); }
            set { if (Element != null) { Element.Opacity = Math.Max(0, Math.Min(100, value)) / 100.0; Commit(); } }
        }

        /// <summary>Whether the selected element is a text-like element.</summary>
        public bool IsTextElement => Element is TextElement || Element is PlaceholderElement;

        /// <summary>Whether the selected element is a shape.</summary>
        public bool IsShapeElement => Element is ShapeElement;

        /// <summary>Whether the selected element is a placeholder.</summary>
        public bool IsPlaceholderElement => Element is PlaceholderElement;

        /// <summary>Text content for text/placeholder elements.</summary>
        public string Text
        {
            get { return (Element as TextElement)?.Text ?? (Element as PlaceholderElement)?.Text; }
            set
            {
                if (Element is TextElement text) { text.Text = value; Commit(); }
                else if (Element is PlaceholderElement placeholder) { placeholder.Text = value; Commit(); }
            }
        }

        /// <summary>Font size for text-like elements.</summary>
        public double FontSize
        {
            get { return (Element as TextElement)?.FontSize ?? (Element as PlaceholderElement)?.FontSize ?? 10; }
            set
            {
                if (Element is TextElement text) { text.FontSize = value; Commit(); }
                else if (Element is PlaceholderElement placeholder) { placeholder.FontSize = value; Commit(); }
            }
        }

        /// <summary>Fill color for shape elements (hex).</summary>
        public string FillColor
        {
            get { return (Element as ShapeElement)?.FillColor; }
            set { if (Element is ShapeElement shape && shape.FillColor != value) { shape.FillColor = value; Commit(); } }
        }

        /// <summary>Data column for placeholder elements.</summary>
        public string ColumnName
        {
            get { return (Element as PlaceholderElement)?.ColumnName; }
            set { if (Element is PlaceholderElement p && p.ColumnName != value) { p.ColumnName = value; Commit(); } }
        }

        private void Commit()
        {
            _editor?.CommitChange();
        }
    }
}
