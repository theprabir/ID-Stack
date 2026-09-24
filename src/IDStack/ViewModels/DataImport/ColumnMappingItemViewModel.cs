using System.Collections.ObjectModel;
using System.Linq;
using IDStack.Core.Models.Import;

namespace IDStack.ViewModels.DataImport
{
    /// <summary>
    /// One row in the column-mapping grid: a placeholder, its bound column, and sample data.
    /// </summary>
    public class ColumnMappingItemViewModel : ViewModelBase
    {
        private string _columnName;
        private string _sampleValue;

        /// <summary>
        /// Creates a mapping row.
        /// </summary>
        /// <param name="placeholderName">Placeholder name without braces.</param>
        /// <param name="columns">Available Excel column names.</param>
        /// <param name="isRequired">Whether the placeholder must be mapped.</param>
        public ColumnMappingItemViewModel(string placeholderName, ObservableCollection<string> columns, bool isRequired)
        {
            PlaceholderName = placeholderName;
            IsRequired = isRequired;
            AvailableColumns = columns;
        }

        /// <summary>Placeholder name (no braces).</summary>
        public string PlaceholderName { get; }

        /// <summary>Columns offered in the dropdown.</summary>
        public ObservableCollection<string> AvailableColumns { get; }

        /// <summary>Whether the placeholder must be mapped before generation.</summary>
        public bool IsRequired { get; }

        /// <summary>Bound Excel column name, or null when unmapped.</summary>
        public string ColumnName
        {
            get { return _columnName; }
            set
            {
                if (SetProperty(ref _columnName, value))
                {
                    OnPropertyChanged(nameof(IsMapped));
                    OnPropertyChanged(nameof(StatusIcon));
                }
            }
        }

        /// <summary>First-row sample value from the bound column.</summary>
        public string SampleValue
        {
            get { return _sampleValue; }
            private set { SetProperty(ref _sampleValue, value); }
        }

        /// <summary>Whether the placeholder is mapped.</summary>
        public bool IsMapped => !string.IsNullOrWhiteSpace(ColumnName);

        /// <summary>Visual status: ✓ mapped, ⚠ unmapped-required, · optional-unmapped.</summary>
        public string StatusIcon => IsMapped ? "✓" : (IsRequired ? "⚠" : "·");

        /// <summary>Fills the sample value from the data using the current binding.</summary>
        /// <param name="data">Loaded data, or null.</param>
        public void RefreshSample(ExcelData data)
        {
            if (data == null || !IsMapped)
            {
                SampleValue = null;
                return;
            }

            var first = data.Rows.FirstOrDefault();
            SampleValue = first?.Get(ColumnName);
        }

        /// <summary>Converts this row into a model mapping.</summary>
        /// <returns>A ColumnMapping.</returns>
        public ColumnMapping ToMapping()
        {
            return new ColumnMapping(PlaceholderName, IsMapped ? ColumnName : null) { IsRequired = IsRequired };
        }
    }
}
