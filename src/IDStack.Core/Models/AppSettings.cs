using System;
using System.Collections.Generic;
using IDStack.Core.Interfaces;

namespace IDStack.Core.Models
{
    /// <summary>
    /// Serializable application settings persisted by <see cref="ISettingsService"/>.
    /// </summary>
    public class AppSettings
    {
        /// <summary>UI language culture code (e.g. "en-US").</summary>
        public string Language { get; set; } = "en-US";

        /// <summary>Whether to show the grid on the editor canvas by default.</summary>
        public bool ShowGrid { get; set; } = true;

        /// <summary>Whether snapping to the grid is enabled by default.</summary>
        public bool SnapToGrid { get; set; } = true;

        /// <summary>Default grid size in millimeters.</summary>
        public double GridSizeMm { get; set; } = 1.0;

        /// <summary>Measurement unit used in the UI ("mm" or "inch").</summary>
        public string MeasurementUnit { get; set; } = "mm";

        /// <summary>Most recently opened template files (most recent first).</summary>
        public List<string> RecentFiles { get; } = new List<string>();

        /// <summary>Window left position (double.NaN lets WPF center the window).</summary>
        public double WindowLeft { get; set; } = double.NaN;

        /// <summary>Window top position.</summary>
        public double WindowTop { get; set; } = double.NaN;

        /// <summary>Window width.</summary>
        public double WindowWidth { get; set; } = 1280;

        /// <summary>Window height.</summary>
        public double WindowHeight { get; set; } = 800;

        /// <summary>Whether the window is maximized.</summary>
        public bool WindowMaximized { get; set; }
    }
}
