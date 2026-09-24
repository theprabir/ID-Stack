using System;
using System.Collections.Generic;
using System.Windows;
using IDStack.Core.Constants;

namespace IDStack.Views.TemplateEditor
{
    /// <summary>
    /// Photoshop-style New Document dialog: preset picker with editable size and units.
    /// </summary>
    public partial class NewDocumentDialog : Window
    {
        private readonly Dictionary<string, double[]> _presets = new Dictionary<string, double[]>
        {
            ["Default (CR80 ID card)"] = new[] { 85.6, 54.0 },
            ["CR80 ID Card (portrait)"] = new[] { 54.0, 85.6 },
            ["A6"] = new[] { 105.0, 148.0 },
            ["A7"] = new[] { 74.0, 105.0 },
            ["Business Card 3.5×2 in"] = new[] { 88.9, 50.8 },
            ["Badge 4×3 in"] = new[] { 101.6, 76.2 },
            ["Custom"] = new[] { 85.6, 54.0 }
        };

        /// <summary>Width in millimeters after OK.</summary>
        public double WidthMm { get; private set; }

        /// <summary>Height in millimeters after OK.</summary>
        public double HeightMm { get; private set; }

        /// <summary>
        /// Creates the dialog and fills the preset list.
        /// </summary>
        public NewDocumentDialog()
        {
            InitializeComponent();
            foreach (var key in _presets.Keys)
            {
                PresetBox.Items.Add(key);
            }
            PresetBox.SelectedIndex = 0;
        }

        private void OnPresetChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PresetBox.SelectedItem == null)
            {
                return;
            }

            var name = PresetBox.SelectedItem.ToString();
            if (_presets.TryGetValue(name, out var size))
            {
                WidthBox.Text = size[0].ToString("0.##");
                HeightBox.Text = size[1].ToString("0.##");
            }
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            if (!TryParseSize(out var widthMm, out var heightMm))
            {
                MessageBox.Show("Width and height must be positive numbers.", "New Document",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            (WidthMm, HeightMm) = Landscape.IsChecked == true ? (widthMm, heightMm) : (heightMm, widthMm);
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private bool TryParseSize(out double widthMm, out double heightMm)
        {
            widthMm = heightMm = 0;

            if (!double.TryParse(WidthBox.Text, out var w) ||
                !double.TryParse(HeightBox.Text, out var h) ||
                w <= 0 || h <= 0)
            {
                return false;
            }

            var unit = (UnitBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            switch (unit)
            {
                case "cm":
                    widthMm = w * 10;
                    heightMm = h * 10;
                    break;
                case "in":
                    widthMm = w * UnitConstants.MillimetersPerInch;
                    heightMm = h * UnitConstants.MillimetersPerInch;
                    break;
                case "px":
                    widthMm = w * UnitConstants.MillimetersPerInch / UnitConstants.ScreenDpi;
                    heightMm = h * UnitConstants.MillimetersPerInch / UnitConstants.ScreenDpi;
                    break;
                default:
                    widthMm = w;
                    heightMm = h;
                    break;
            }

            return widthMm >= 5 && widthMm <= 2000 && heightMm >= 5 && heightMm <= 2000;
        }
    }
}
