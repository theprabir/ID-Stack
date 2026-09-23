using System;
using IDStack.Commands;
using IDStack.Core.Interfaces;

namespace IDStack.ViewModels.TemplateEditor
{
    /// <summary>
    /// View model for the tools panel: one button per element type.
    /// </summary>
    public class ToolsPanelViewModel : ViewModelBase
    {
        /// <summary>
        /// Creates the tools panel and binds it to the editor's add commands.
        /// </summary>
        /// <param name="editor">Editor view model receiving add requests.</param>
        /// <param name="localization">Localization service.</param>
        public ToolsPanelViewModel(TemplateEditorViewModel editor, ILocalizationService localization)
        {
            if (editor == null)
            {
                throw new ArgumentNullException(nameof(editor));
            }

            AddTextCommand = editor.AddTextCommand;
            AddImageCommand = editor.AddImageCommand;
            AddRectangleCommand = editor.AddRectangleCommand;
            AddEllipseCommand = editor.AddEllipseCommand;
            AddLineCommand = editor.AddLineCommand;
            AddBarcodeCommand = editor.AddBarcodeCommand;
            AddPlaceholderCommand = editor.AddPlaceholderCommand;
            DeleteSelectedCommand = editor.DeleteSelectedCommand;
            DuplicateSelectedCommand = editor.DuplicateSelectedCommand;
        }

        /// <summary>Adds a text element.</summary>
        public RelayCommand AddTextCommand { get; }

        /// <summary>Adds an image element.</summary>
        public RelayCommand AddImageCommand { get; }

        /// <summary>Adds a rectangle.</summary>
        public RelayCommand AddRectangleCommand { get; }

        /// <summary>Adds an ellipse.</summary>
        public RelayCommand AddEllipseCommand { get; }

        /// <summary>Adds a line.</summary>
        public RelayCommand AddLineCommand { get; }

        /// <summary>Adds a barcode.</summary>
        public RelayCommand AddBarcodeCommand { get; }

        /// <summary>Adds a placeholder.</summary>
        public RelayCommand AddPlaceholderCommand { get; }

        /// <summary>Deletes the selection.</summary>
        public RelayCommand DeleteSelectedCommand { get; }

        /// <summary>Duplicates the selection.</summary>
        public RelayCommand DuplicateSelectedCommand { get; }
    }
}
