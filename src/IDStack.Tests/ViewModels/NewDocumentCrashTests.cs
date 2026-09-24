using System;
using IDStack.Core.Models.Elements;
using IDStack.Core.Interfaces;
using IDStack.Services;
using IDStack.ViewModels.TemplateEditor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IDStack.Tests.ViewModels
{
    /// <summary>
    /// Reproduction test for the NullReferenceException thrown by NewDocument.
    /// </summary>
    [TestClass]
    public class NewDocumentCrashTests
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
        public void NewDocument_AfterAddingElements_DoesNotThrow()
        {
            var editor = CreateEditor();
            editor.AddElement(new TextElement());
            editor.AddElement(new ShapeElement { ShapeKind = ShapeKind.Rectangle });
            editor.AddElement(new BarcodeElement());

            editor.NewDocument(100, 70);

            Assert.AreEqual(0, editor.Elements.Count);
            Assert.AreEqual(100, editor.CurrentSide.CanvasWidth, 0.01);
        }

        [TestMethod]
        public void NewDocument_UndoAfterNew_DoesNotThrow()
        {
            var editor = CreateEditor();
            editor.AddElement(new TextElement());

            editor.NewDocument(100, 70);
            editor.UndoCommand.Execute(null);

            Assert.IsNotNull(editor.CurrentSide);
        }
    }
}
