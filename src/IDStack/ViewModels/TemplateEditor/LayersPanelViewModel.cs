using System;
using System.Linq;
using IDStack.Commands;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;

namespace IDStack.ViewModels.TemplateEditor
{
    /// <summary>
    /// View model for the layers panel: reorders, hides, locks, and renames elements.
    /// </summary>
    public class LayersPanelViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localization;
        private TemplateEditorViewModel _editor;

        /// <summary>
        /// Creates the layers panel view model.
        /// </summary>
        /// <param name="localization">Localization service.</param>
        public LayersPanelViewModel(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            MoveUpCommand = new RelayCommand(_ => MoveSelected(1), _ => CanMove(1));
            MoveDownCommand = new RelayCommand(_ => MoveSelected(-1), _ => CanMove(-1));
            ToggleVisibilityCommand = new RelayCommand(param => ToggleVisibility(param as CanvasElement));
            ToggleLockCommand = new RelayCommand(param => ToggleLock(param as CanvasElement));
        }

        /// <summary>Attaches the panel to an editor instance.</summary>
        /// <param name="editor">Editor view model.</param>
        public void Attach(TemplateEditorViewModel editor)
        {
            _editor = editor;
            OnPropertyChanged(nameof(Elements));
            OnPropertyChanged(nameof(HasElements));
        }

        /// <summary>Elements of the current side (topmost first for display).</summary>
        public System.Collections.Generic.IEnumerable<CanvasElement> Elements =>
            _editor?.Elements?.Reverse() ?? Enumerable.Empty<CanvasElement>();

        /// <summary>Whether the current side has any elements.</summary>
        public bool HasElements => _editor?.Elements != null && _editor.Elements.Count > 0;

        /// <summary>Raises z-order of the selected element.</summary>
        public RelayCommand MoveUpCommand { get; }

        /// <summary>Lowers z-order of the selected element.</summary>
        public RelayCommand MoveDownCommand { get; }

        /// <summary>Toggles element visibility (parameter: element).</summary>
        public RelayCommand ToggleVisibilityCommand { get; }

        /// <summary>Toggles element lock (parameter: element).</summary>
        public RelayCommand ToggleLockCommand { get; }

        private void MoveSelected(int direction)
        {
            if (_editor?.SelectedElement == null)
            {
                return;
            }

            var elements = _editor.Elements;
            var index = elements.IndexOf(_editor.SelectedElement);
            var target = index + direction;
            if (index < 0 || target < 0 || target >= elements.Count)
            {
                return;
            }

            elements.Move(index, target);
            _editor.CommitChange();
            Refresh();
        }

        private bool CanMove(int direction)
        {
            if (_editor?.SelectedElement == null || _editor.Elements == null)
            {
                return false;
            }

            var index = _editor.Elements.IndexOf(_editor.SelectedElement);
            var target = index + direction;
            return index >= 0 && target >= 0 && target < _editor.Elements.Count;
        }

        private void ToggleVisibility(CanvasElement element)
        {
            if (element == null)
            {
                return;
            }

            element.IsVisible = !element.IsVisible;
            _editor?.CommitChange();
            Refresh();
        }

        private void ToggleLock(CanvasElement element)
        {
            if (element == null)
            {
                return;
            }

            element.IsLocked = !element.IsLocked;
            _editor?.CommitChange();
            Refresh();
        }

        private void Refresh()
        {
            OnPropertyChanged(nameof(Elements));
            OnPropertyChanged(nameof(HasElements));
        }
    }
}
