using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDStack.Helpers
{
    /// <summary>
    /// Lightweight observable base for model objects bound in the UI.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the given property.
        /// </summary>
        /// <param name="propertyName">Property name; defaults to the caller.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Updates a field and raises <see cref="PropertyChanged"/> only when the value changed.
        /// </summary>
        /// <typeparam name="T">Property type.</typeparam>
        /// <param name="field">Backing field reference.</param>
        /// <param name="value">New value.</param>
        /// <param name="propertyName">Property name; defaults to the caller.</param>
        /// <returns>True when the value changed.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
