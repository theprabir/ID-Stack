using System;
using System.Windows.Input;

namespace IDCardSoftware.Commands
{
    /// <summary>
    /// Typed command delegating to lambdas, with explicit raise-can-execute support.
    /// </summary>
    /// <typeparam name="T">Command parameter type.</typeparam>
    public class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        /// <summary>
        /// Creates a typed delegate command.
        /// </summary>
        /// <param name="execute">Action to run.</param>
        /// <param name="canExecute">Optional enable/disable predicate.</param>
        public DelegateCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler CanExecuteChanged;

        /// <inheritdoc />
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(CastParameter(parameter));
        }

        /// <inheritdoc />
        public void Execute(object parameter)
        {
            _execute(CastParameter(parameter));
        }

        /// <summary>
        /// Raises <see cref="CanExecuteChanged"/> to refresh bound button states.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private static T CastParameter(object parameter)
        {
            if (parameter is T typed)
            {
                return typed;
            }

            return default(T);
        }
    }
}
