using System;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Generic undo/redo stack for editor snapshots.
    /// </summary>
    /// <typeparam name="T">Snapshot state type.</typeparam>
    public interface IHistoryService<T>
    {
        /// <summary>Raised when undo/redo availability changes.</summary>
        event EventHandler HistoryChanged;

        /// <summary>Current number of undo steps available.</summary>
        int UndoCount { get; }

        /// <summary>Current number of redo steps available.</summary>
        int RedoCount { get; }

        /// <summary>Whether an undo step is available.</summary>
        bool CanUndo { get; }

        /// <summary>Whether a redo step is available.</summary>
        bool CanRedo { get; }

        /// <summary>Clears all history and sets the initial state.</summary>
        /// <param name="initialState">Baseline state.</param>
        void Reset(T initialState);

        /// <summary>Pushes a new state after a user edit, discarding any redo stack.</summary>
        /// <param name="state">New current state.</param>
        void Push(T state);

        /// <summary>Reverts to the previous state.</summary>
        /// <returns>The restored state.</returns>
        T Undo();

        /// <summary>Re-applies the next state after an undo.</summary>
        /// <returns>The restored state.</returns>
        T Redo();
    }
}
