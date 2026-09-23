using System;
using System.Collections.Generic;
using IDStack.Core.Interfaces;

namespace IDStack.Services
{
    /// <summary>
    /// Snapshot-based undo/redo history with a bounded stack.
    /// </summary>
    /// <typeparam name="T">Snapshot state type.</typeparam>
    public class HistoryService<T> : IHistoryService<T>
    {
        private readonly int _capacity;
        private readonly List<T> _undoStack = new List<T>();
        private readonly List<T> _redoStack = new List<T>();
        private T _current;

        /// <summary>
        /// Creates a history service.
        /// </summary>
        /// <param name="capacity">Maximum undo steps retained (default 100).</param>
        public HistoryService(int capacity = 100)
        {
            _capacity = capacity > 0 ? capacity : 100;
        }

        /// <inheritdoc />
        public event EventHandler HistoryChanged;

        /// <inheritdoc />
        public int UndoCount => _undoStack.Count;

        /// <inheritdoc />
        public int RedoCount => _redoStack.Count;

        /// <inheritdoc />
        public bool CanUndo => _undoStack.Count > 0;

        /// <inheritdoc />
        public bool CanRedo => _redoStack.Count > 0;

        /// <inheritdoc />
        public void Reset(T initialState)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _current = initialState;
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Push(T state)
        {
            _undoStack.Add(_current);
            if (_undoStack.Count > _capacity)
            {
                _undoStack.RemoveAt(0);
            }
            _redoStack.Clear();
            _current = state;
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public T Undo()
        {
            if (_undoStack.Count == 0)
            {
                return _current;
            }

            _redoStack.Add(_current);
            _current = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            return _current;
        }

        /// <inheritdoc />
        public T Redo()
        {
            if (_redoStack.Count == 0)
            {
                return _current;
            }

            _undoStack.Add(_current);
            _current = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            return _current;
        }
    }
}
