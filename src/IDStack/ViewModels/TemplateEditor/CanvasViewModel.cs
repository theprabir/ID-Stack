using System;
using IDStack.Core.Constants;
using IDStack.Core.Interfaces;

namespace IDStack.ViewModels.TemplateEditor
{
    /// <summary>
    /// View model for the canvas surface: zoom, fit, and scale conversion.
    /// </summary>
    public class CanvasViewModel : ViewModelBase
    {
        private readonly TemplateEditorViewModel _editor;

        /// <summary>
        /// Creates the canvas view model.
        /// </summary>
        /// <param name="editor">Owning editor view model.</param>
        public CanvasViewModel(TemplateEditorViewModel editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>The owning editor.</summary>
        public TemplateEditorViewModel Editor => _editor;

        /// <summary>Screen pixels per millimeter at the current zoom.</summary>
        public double Scale => _editor.ZoomLevel;

        /// <summary>Converts millimeters to on-screen pixels.</summary>
        /// <param name="millimeters">Value in mm.</param>
        /// <returns>Value in device-independent pixels.</returns>
        public double ToPixels(double millimeters)
        {
            return millimeters * Scale;
        }

        /// <summary>Converts on-screen pixels to millimeters.</summary>
        /// <param name="pixels">Value in pixels.</param>
        /// <returns>Value in millimeters.</returns>
        public double ToMillimeters(double pixels)
        {
            return pixels / Scale;
        }

        /// <summary>DPI note for the canvas (default WPF DPI).</summary>
        public double ScreenDpi => UnitConstants.ScreenDpi;
    }
}
