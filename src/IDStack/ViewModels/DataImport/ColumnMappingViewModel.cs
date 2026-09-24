using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using IDStack.Core.Models.Import;
using IDStack.Core.Models.Template;
using IDStack.Core.Models.Elements;

namespace IDStack.ViewModels.DataImport
{
    /// <summary>
    /// Maps template placeholders to Excel columns and shows a live sample preview.
    /// </summary>
    public class ColumnMappingViewModel : ViewModelBase
    {
        private ObservableCollection<string> _availableColumns = new ObservableCollection<string>();
        private string _statusText;

        /// <summary>
        /// Creates the mapping view model.
        /// </summary>
        public ColumnMappingViewModel()
        {
        }

        /// <summary>One row per distinct placeholder in the template (both sides).</summary>
        public ObservableCollection<ColumnMappingItemViewModel> Mappings { get; } =
            new ObservableCollection<ColumnMappingItemViewModel>();

        /// <summary>Columns available from the loaded data file.</summary>
        public ObservableCollection<string> AvailableColumns
        {
            get { return _availableColumns; }
            private set { SetProperty(ref _availableColumns, value); }
        }

        /// <summary>Status line (counts of mapped/unmapped).</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        /// <summary>Whether every required placeholder is mapped.</summary>
        public bool AllRequiredMapped => Mappings.Where(m => m.IsRequired).All(m => m.IsMapped);

        /// <summary>
        /// Rebuilds the mapping rows from the template's placeholders and the loaded columns.
        /// </summary>
        /// <param name="template">Current template.</param>
        /// <param name="data">Loaded data, or null when none loaded yet.</param>
        public void BuildFrom(CardTemplate template, ExcelData data)
        {
            Mappings.Clear();
            AvailableColumns = new ObservableCollection<string>(data?.ColumnNames ?? new List<string>());

            if (template == null)
            {
                UpdateStatus();
                return;
            }

            var placeholderNames = template.FrontSide.Elements
                .Concat(template.BackSide.Elements)
                .OfType<PlaceholderElement>()
                .Select(p => (p.ColumnName ?? string.Empty).Trim())
                .Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in placeholderNames)
            {
                var item = new ColumnMappingItemViewModel(name, AvailableColumns, isRequired: true);
                var matchingColumn = AvailableColumns.FirstOrDefault(c =>
                    string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
                if (matchingColumn != null)
                {
                    item.ColumnName = matchingColumn;
                }

                Mappings.Add(item);
            }

            UpdateStatus();
            RefreshSamples(data);
        }

        /// <summary>Refreshes sample values after a new data file is loaded.</summary>
        /// <param name="data">Loaded data, or null.</param>
        public void RefreshSamples(ExcelData data)
        {
            foreach (var mapping in Mappings)
            {
                mapping.RefreshSample(data);
            }

            UpdateStatus();
        }

        /// <summary>Extracts the current bindings as model mappings.</summary>
        /// <returns>Mappings for all rows (unmapped rows have a null column).</returns>
        public List<ColumnMapping> GetMappings()
        {
            return Mappings.Select(m => m.ToMapping()).ToList();
        }

        /// <summary>Columns the user marked as required and mapped.</summary>
        /// <returns>Bound column names.</returns>
        public List<string> GetRequiredColumns()
        {
            return Mappings
                .Where(m => m.IsRequired && m.IsMapped)
                .Select(m => m.ColumnName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void UpdateStatus()
        {
            var mapped = Mappings.Count(m => m.IsMapped);
            StatusText = Mappings.Count == 0
                ? "No placeholders in the template."
                : mapped + " of " + Mappings.Count + " placeholders mapped.";
            OnPropertyChanged(nameof(AllRequiredMapped));
        }
    }
}
