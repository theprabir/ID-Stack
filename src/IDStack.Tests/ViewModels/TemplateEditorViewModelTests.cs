using System;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Template;
using IDStack.Services;
using IDStack.ViewModels.TemplateEditor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IDStack.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for the template editor view model.
    /// </summary>
    [TestClass]
    public class TemplateEditorViewModelTests
    {
        private TemplateEditorViewModel CreateEditor()
        {
            var localization = new Mock<ILocalizationService>();
            localization.Setup(l => l.GetString(It.IsAny<string>())).Returns<string>(k => k);

            return new TemplateEditorViewModel(
                new TemplateService(),
                new HistoryService<EditorState>(),
                localization.Object);
        }

        [TestMethod]
        public void NewEditor_HasFrontAndBackSides()
        {
            var editor = CreateEditor();

            Assert.AreEqual(SideType.Front, editor.SelectedSideType);
            Assert.IsNotNull(editor.CurrentSide);
        }

        [TestMethod]
        public void AddElement_CentersAndSelects()
        {
            var editor = CreateEditor();

            editor.AddElement(new TextElement());

            Assert.AreEqual(1, editor.Elements.Count);
            Assert.AreSame(editor.Elements[0], editor.SelectedElement);
            Assert.AreEqual(editor.CurrentSide.CanvasWidth / 2 - 10, editor.Elements[0].X, 0.01);
        }

        [TestMethod]
        public void DeleteSelected_RemovesElement()
        {
            var editor = CreateEditor();
            editor.AddElement(new TextElement());

            editor.DeleteSelected();

            Assert.AreEqual(0, editor.Elements.Count);
            Assert.IsNull(editor.SelectedElement);
        }

        [TestMethod]
        public void DeleteSelected_DoesNothingWhenLocked()
        {
            var editor = CreateEditor();
            var element = new TextElement { IsLocked = true };
            editor.Elements.Add(element);
            editor.SelectedElement = element;

            editor.DeleteSelected();

            Assert.AreEqual(1, editor.Elements.Count);
        }

        [TestMethod]
        public void DuplicateSelected_CopiesWithOffset()
        {
            var editor = CreateEditor();
            var element = new TextElement { X = 10, Y = 10 };
            editor.Elements.Add(element);
            editor.SelectedElement = element;

            editor.DuplicateSelected();

            Assert.AreEqual(2, editor.Elements.Count);
            var copy = editor.Elements[1];
            Assert.AreEqual(13, copy.X);
            Assert.AreEqual(13, copy.Y);
            Assert.AreNotEqual(element.Id, copy.Id);
        }

        [TestMethod]
        public void UndoAfterAdd_RemovesElement()
        {
            var editor = CreateEditor();
            editor.AddElement(new TextElement());
            Assert.AreEqual(1, editor.Elements.Count);

            editor.UndoCommand.Execute(null);

            Assert.AreEqual(0, editor.Elements.Count);
        }

        [TestMethod]
        public void UndoThenRedo_RestoresElement()
        {
            var editor = CreateEditor();
            editor.AddElement(new TextElement());
            editor.UndoCommand.Execute(null);

            editor.RedoCommand.Execute(null);

            Assert.AreEqual(1, editor.Elements.Count);
        }

        [TestMethod]
        public void CanUndoReflectsHistory()
        {
            var editor = CreateEditor();
            Assert.IsFalse(editor.UndoCommand.CanExecute(null));

            editor.AddElement(new TextElement());
            Assert.IsTrue(editor.UndoCommand.CanExecute(null));
        }

        [TestMethod]
        public void SwitchSide_ResetsSelectionAndHistory()
        {
            var editor = CreateEditor();
            editor.AddElement(new TextElement());

            editor.SelectedSideType = SideType.Back;

            Assert.IsNull(editor.SelectedElement);
            Assert.AreEqual(0, editor.Elements.Count);
            Assert.IsFalse(editor.UndoCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task SaveAndLoad_RoundTripsThroughEditor()
        {
            var editor = CreateEditor();
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                Guid.NewGuid().ToString("N") + ".idcard");
            try
            {
                editor.AddElement(new TextElement { Text = "Saved" });
                await editor.SaveToFileAsync(path);

                var editor2 = CreateEditor();
                await editor2.LoadFromFileAsync(path);

                var text = editor2.Elements.OfType<TextElement>().Single();
                Assert.AreEqual("Saved", text.Text);
                Assert.IsFalse(editor2.IsModified);
            }
            finally
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        [TestMethod]
        public void AddElement_MarksTemplateModified()
        {
            var editor = CreateEditor();
            Assert.IsFalse(editor.IsModified);

            editor.AddElement(new TextElement());

            Assert.IsTrue(editor.IsModified);
        }

        [TestMethod]
        public void Zoom_IsClamped()
        {
            var editor = CreateEditor();

            editor.ZoomLevel = 999;
            Assert.IsTrue(editor.ZoomLevel <= 16.0);

            editor.ZoomLevel = 0.01;
            Assert.IsTrue(editor.ZoomLevel >= 0.4);
        }
    }
}
