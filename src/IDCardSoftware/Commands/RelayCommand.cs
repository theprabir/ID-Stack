using System;
using System.Windows.Input;

namespace IDCardSoftware.Commands
{
    /// <summary>
    /// Synchronous command implementation of <see cref="ICommand"/> for MVVM.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// Creates a relay command.
        /// </summary>
        /// <param name="execute">Action to run.</param>
        /// <param name="canExecute">Optional enable/disable predicate.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <inheritdoc />
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <inheritdoc />
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
