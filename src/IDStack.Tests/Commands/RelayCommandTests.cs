using System;
using System.Threading.Tasks;
using System.Windows.Input;
using IDStack.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDStack.Tests.Commands
{
    /// <summary>
    /// Unit tests for the MVVM command implementations.
    /// </summary>
    [TestClass]
    public class RelayCommandTests
    {
        [TestMethod]
        public void Execute_RunsAction()
        {
            var called = false;
            var command = new RelayCommand(_ => called = true);

            command.Execute(null);

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void CanExecute_WithoutPredicate_ReturnsTrue()
        {
            ICommand command = new RelayCommand(_ => { });
            Assert.IsTrue(command.CanExecute(null));
        }

        [TestMethod]
        public void CanExecute_WithPredicate_UsesPredicateResult()
        {
            ICommand command = new RelayCommand(_ => { }, _ => false);
            Assert.IsFalse(command.CanExecute(null));
        }

        [TestMethod]
        public void Constructor_NullAction_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new RelayCommand(null));
        }
    }

    /// <summary>
    /// Unit tests for <see cref="AsyncRelayCommand"/>.
    /// </summary>
    [TestClass]
    public class AsyncRelayCommandTests
    {
        [TestMethod]
        public async Task Execute_CompletesTask()
        {
            var completed = false;
            var command = new AsyncRelayCommand(async _ =>
            {
                await Task.Yield();
                completed = true;
            });

            command.Execute(null);
            await Task.Delay(50);

            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void CanExecute_WhileExecuting_ReturnsFalse()
        {
            var release = new TaskCompletionSource<bool>();
            var command = new AsyncRelayCommand(_ => release.Task);

            command.Execute(null);
            var canExecute = command.CanExecute(null);

            Assert.IsFalse(canExecute);
            release.SetResult(true);
        }

        [TestMethod]
        public void Constructor_NullAction_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new AsyncRelayCommand(null));
        }
    }

    /// <summary>
    /// Unit tests for <see cref="DelegateCommand{T}"/>.
    /// </summary>
    [TestClass]
    public class DelegateCommandTests
    {
        [TestMethod]
        public void Execute_ReceivesTypedParameter()
        {
            var received = 0;
            var command = new DelegateCommand<int>(value => received = value);

            command.Execute(42);

            Assert.AreEqual(42, received);
        }

        [TestMethod]
        public void RaiseCanExecuteChanged_FiresEvent()
        {
            var command = new DelegateCommand<string>(_ => { });
            var fired = false;
            command.CanExecuteChanged += (s, e) => fired = true;

            command.RaiseCanExecuteChanged();

            Assert.IsTrue(fired);
        }
    }
}
