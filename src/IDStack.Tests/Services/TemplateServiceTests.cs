using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Template;
using IDStack.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace IDStack.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="TemplateService"/> persistence and validation.
    /// </summary>
    [TestClass]
    public class TemplateServiceTests
    {
        private string _tempPath;

        [TestInitialize]
        public void Setup()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), "IDStackTests_" + Guid.NewGuid().ToString("N") + ".idcard");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempPath))
            {
                File.Delete(_tempPath);
            }
        }

        [TestMethod]
        public void CreateNew_ProducesTwoSidesWithCR80Size()
        {
            var service = new TemplateService();
            var template = service.CreateNew("Test");

            Assert.AreEqual("Test", template.Name);
            Assert.AreEqual(SideType.Front, template.FrontSide.SideType);
            Assert.AreEqual(SideType.Back, template.BackSide.SideType);
            Assert.AreEqual(85.6, template.FrontSide.CanvasWidth);
            Assert.AreEqual(54.0, template.FrontSide.CanvasHeight);
        }

        [TestMethod]
        public async Task SaveThenLoad_RoundTripsElements()
        {
            var service = new TemplateService();
            var template = service.CreateNew("RoundTrip");
            template.FrontSide.Elements.Add(new TextElement { Text = "Hello", X = 5, Y = 6 });
            template.FrontSide.Elements.Add(new ShapeElement { ShapeKind = ShapeKind.Ellipse });
            template.BackSide.Elements.Add(new PlaceholderElement { ColumnName = "Name" });

            await service.SaveAsync(template, _tempPath);
            var loaded = await service.LoadAsync(_tempPath);

            Assert.AreEqual("RoundTrip", loaded.Name);
            Assert.AreEqual(2, loaded.FrontSide.Elements.Count);
            var text = loaded.FrontSide.Elements.OfType<TextElement>().Single();
            Assert.AreEqual("Hello", text.Text);
            Assert.AreEqual(5, text.X);
            var placeholder = loaded.BackSide.Elements.OfType<PlaceholderElement>().Single();
            Assert.AreEqual("Name", placeholder.ColumnName);
        }

        [TestMethod]
        public async Task SaveAsync_WritesAtomically_NoTempLeftover()
        {
            var service = new TemplateService();
            var template = service.CreateNew("Atomic");

            await service.SaveAsync(template, _tempPath);
            await service.SaveAsync(template, _tempPath); // second save uses File.Replace

            Assert.IsTrue(File.Exists(_tempPath));
            Assert.IsFalse(File.Exists(_tempPath + ".tmp"));
        }

        [TestMethod]
        public async Task LoadAsync_InvalidFile_Throws()
        {
            File.WriteAllText(_tempPath, "not json at all {{{");
            var service = new TemplateService();
            await Assert.ThrowsExceptionAsync<JsonReaderException>(() => service.LoadAsync(_tempPath));
        }

        [TestMethod]
        public void Validate_FlagsMissingColumnAndInvalidSize()
        {
            var service = new TemplateService();
            var template = service.CreateNew("Validate");
            template.FrontSide.Elements.Add(new PlaceholderElement());
            template.FrontSide.Elements.Add(new TextElement { Width = 0, Height = 0 });

            var problems = service.Validate(template);

            Assert.AreEqual(2, problems.Count);
        }

        [TestMethod]
        public void Validate_CleanTemplate_ReturnsNoProblems()
        {
            var service = new TemplateService();
            var template = service.CreateNew("Clean");

            Assert.AreEqual(0, service.Validate(template).Count);
        }

        [TestMethod]
        public void Clone_Element_IsIndependent()
        {
            var original = new TextElement { Text = "A", X = 1 };
            var clone = (TextElement)original.Clone();

            clone.Text = "B";
            clone.X = 99;

            Assert.AreEqual("A", original.Text);
            Assert.AreEqual(1, original.X);
            Assert.AreNotEqual(original.Id, clone.Id);
        }
    }
}
