using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IDCardSoftware.Commands
{
    /// <summary>
    /// Async command that prevents re-entrant execution while running.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Predicate<object> _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Creates an async relay command.
        /// </summary>
        /// <param name="execute">Async action to run.</param>
        /// <param name="canExecute">Optional enable/disable predicate.</param>
        public AsyncRelayCommand(Func<object, Task> execute, Predicate<object> canExecute = null)
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
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        /// <inheritdoc />
        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter);
        }

        /// <summary>
        /// Runs the command body and suppresses re-entrant execution until it completes.
        /// </summary>
        /// <param name="parameter">Command parameter.</param>
        /// <returns>The task representing execution.</returns>
        protected async Task ExecuteAsync(object parameter)
        {
            if (_isExecuting)
            {
                return;
            }

            _isExecuting = true;
            try
            {
                await _execute(parameter).ConfigureAwait(true);
            }
            finally
            {
                _isExecuting = false;
            }
        }
    }
}
