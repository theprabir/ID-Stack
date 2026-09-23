using System;
using IDStack.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDStack.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="HistoryService{T}"/> undo/redo semantics.
    /// </summary>
    [TestClass]
    public class HistoryServiceTests
    {
        [TestMethod]
        public void Reset_ClearsStacksAndSetsBaseline()
        {
            var history = new HistoryService<string>();
            history.Push("a");
            history.Reset("base");

            Assert.IsFalse(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
            Assert.AreEqual(0, history.UndoCount);
        }

        [TestMethod]
        public void PushThenUndo_RestoresPreviousState()
        {
            var history = new HistoryService<string>();
            history.Reset("base");
            history.Push("edit1");
            history.Push("edit2");

            Assert.AreEqual("edit1", history.Undo());
            Assert.AreEqual("base", history.Undo());
            Assert.AreEqual("base", history.Undo()); // pinned at baseline
        }

        [TestMethod]
        public void UndoThenRedo_RestoresRedoneState()
        {
            var history = new HistoryService<string>();
            history.Reset("base");
            history.Push("edit1");

            history.Undo();
            Assert.AreEqual("edit1", history.Redo());
        }

        [TestMethod]
        public void PushAfterUndo_ClearsRedoStack()
        {
            var history = new HistoryService<string>();
            history.Reset("base");
            history.Push("edit1");
            history.Undo();
            // After the undo the current state is "base"; pushing a new edit must
            // discard the redo branch ("edit1") entirely.
            history.Push("edit2");

            Assert.IsFalse(history.CanRedo);
            Assert.AreEqual("base", history.Undo());
        }

        [TestMethod]
        public void Capacity_IsBounded()
        {
            var history = new HistoryService<string>(3);
            history.Reset("base");
            for (var i = 1; i <= 10; i++)
            {
                history.Push("edit" + i);
            }

            Assert.IsTrue(history.UndoCount <= 3);
        }

        [TestMethod]
        public void HistoryChanged_RaisesOnPushAndUndo()
        {
            var history = new HistoryService<string>();
            var raises = 0;
            history.HistoryChanged += (s, e) => raises++;

            history.Reset("base");
            history.Push("a");
            history.Undo();

            Assert.AreEqual(3, raises);
        }
    }
}
