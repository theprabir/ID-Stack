namespace IDStack.Core.Models.Import
{
    /// <summary>
    /// A text or image spot in a saved .idcard design that can receive imported data.
    /// Detected automatically when the design is loaded; the user renames each one
    /// so mapping to Excel columns is unambiguous.
    /// </summary>
    public class DesignPlaceholder : System.ComponentModel.INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void Notify([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        /// <summary>Kind of placeholder.</summary>
        public enum PlaceholderKind
        {
            /// <summary>A text layer receiving column text.</summary>
            Text,

            /// <summary>An image layer receiving a matched photo.</summary>
            Image
        }

        /// <summary>
        /// Creates a placeholder referencing an element on a template side.
        /// </summary>
        /// <param name="kind">Text or Image.</param>
        /// <param name="elementId">Element ID inside the design.</param>
        /// <param name="suggestedName">Name suggested from the design layer.</param>
        /// <param name="side">Front or Back.</param>
        public DesignPlaceholder(PlaceholderKind kind, System.Guid elementId, string suggestedName, Core.Models.Template.SideType side)
        {
            Kind = kind;
            ElementId = elementId;
            Name = suggestedName;
            SuggestedName = suggestedName;
            Side = side;
        }

        /// <summary>Text or Image.</summary>
        public PlaceholderKind Kind { get; }

        /// <summary>Element ID inside the design.</summary>
        public System.Guid ElementId { get; }

        /// <summary>Front or Back.</summary>
        public Core.Models.Template.SideType Side { get; }

        /// <summary>User-facing name (renameable, must be unique).</summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; Notify(); }
        }
        private string _name;

        /// <summary>Name suggested from the design layer text or layer name.</summary>
        public string SuggestedName { get; }

        /// <summary>Current text content of a text placeholder (sample preview).</summary>
        public string SampleText
        {
            get { return _sampleText; }
            set { _sampleText = value; Notify(); }
        }
        private string _sampleText;

        /// <summary>Excel column bound to this placeholder, or null.</summary>
        public string BoundColumn
        {
            get { return _boundColumn; }
            set
            {
                _boundColumn = value;
                Notify();
                Notify(nameof(IsBound));
            }
        }
        private string _boundColumn;

        /// <summary>Whether a column/photo is bound.</summary>
        public bool IsBound => !string.IsNullOrEmpty(BoundColumn);

        /// <summary>Returns the placeholder name for display.</summary>
        /// <returns>The name.</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
