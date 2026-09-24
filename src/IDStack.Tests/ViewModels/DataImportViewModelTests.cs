using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Import;
using IDStack.Core.Models.Template;
using IDStack.Services;
using IDStack.ViewModels.DataImport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IDStack.Tests.ViewModels
{
    /// <summary>
    /// Tests for the design-based Data Import flow: import design + Excel + photos,
    /// placeholder auto-detection, mapping, and validation.
    /// </summary>
    [TestClass]
    public class DataImportViewModelTests
    {
        private Mock<IExcelService> _excelService;
        private Mock<IPhotoService> _photoService;
        private Mock<IDataValidationService> _validationService;
        private DesignImportService _designService;
        private string _tempDir;

        [TestInitialize]
        public void Setup()
        {
            _excelService = new Mock<IExcelService>();
            _photoService = new Mock<IPhotoService>(MockBehavior.Strict);
            _validationService = new Mock<IDataValidationService>();
            _designService = new DesignImportService(
                new TemplateService(),
                new PsdDesignImporter(new Mock<ILogger>().Object),
                new Mock<ILogger>().Object);
            _tempDir = Path.Combine(Path.GetTempPath(), "IDStackDataImport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Best effort.
            }
        }

        private DataImportViewModel CreateViewModel()
        {
            return new DataImportViewModel(
                _designService,
                _excelService.Object,
                _photoService.Object,
                _validationService.Object);
        }

        private static ExcelData SampleData()
        {
            var data = new ExcelData
            {
                SourceFilePath = @"C:\data\people.xlsx",
                Format = "xlsx",
                ColumnNames = new List<string> { "Full Name", "ID" }
            };
            data.Rows.Add(new DataRow(new Dictionary<string, string> { ["Full Name"] = "Alice", ["ID"] = "1" }) { RowNumber = 1 });
            data.Rows.Add(new DataRow(new Dictionary<string, string> { ["Full Name"] = "Bob", ["ID"] = "2" }) { RowNumber = 2 });
            return data;
        }

        private string WriteDesign(string name, Action<CardTemplate> customize)
        {
            var template = new CardTemplate { Name = name };
            customize?.Invoke(template);
            var path = Path.Combine(_tempDir, name + ".idcard");
            new TemplateService().SaveAsync(template, path).Wait();
            return path;
        }

        [TestMethod]
        public async Task LoadDesignAsync_IdcardDetectsTextAndImagePlaceholders()
        {
            var path = WriteDesign("badge", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Name = "NameLayer", Text = "Full Name" });
                t.FrontSide.Elements.Add(new TextElement { Name = "IdLayer", Text = "ID" });
                t.FrontSide.Elements.Add(new ImageElement { Name = "Photo", Source = "x.png" });
                t.FrontSide.Elements.Add(new ImageElement { Name = "Design", Source = "bg.png" }); // background: not a placeholder
            });
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(path);

            Assert.IsTrue(vm.HasDesign);
            Assert.AreEqual(3, vm.Placeholders.Count);
            Assert.AreEqual(2, vm.Placeholders.Count(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text));
            Assert.AreEqual(1, vm.Placeholders.Count(p => p.Kind == DesignPlaceholder.PlaceholderKind.Image));
        }

        [TestMethod]
        public async Task LoadDesignAsync_PlaceholderNamesAreUnique()
        {
            var path = WriteDesign("dupes", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Name" });
                t.FrontSide.Elements.Add(new TextElement { Text = "Name" });
            });
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(path);

            Assert.AreEqual("Name", vm.Placeholders[0].Name);
            Assert.AreEqual("Name 2", vm.Placeholders[1].Name);
        }

        [TestMethod]
        public async Task LoadDesignAsync_UnsupportedExtensionShowsError()
        {
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(@"C:\designs\thing.txt");

            StringAssert.Contains(vm.StatusText, "Unsupported design type");
            Assert.IsFalse(vm.HasDesign);
        }

        [TestMethod]
        public async Task LoadDesignAsync_MissingFileShowsErrorNotThrow()
        {
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(Path.Combine(_tempDir, "nope.idcard"));

            StringAssert.Contains(vm.StatusText, "Could not load the design");
            Assert.IsFalse(vm.HasDesign);
        }

        [TestMethod]
        public async Task LoadDesignAsync_SampleTextFilledFromLayerText()
        {
            var path = WriteDesign("sample", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Full Name" });
            });
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(path);

            Assert.AreEqual("Full Name", vm.Placeholders[0].SampleText);
        }

        [TestMethod]
        public async Task ImportExcelFileAsync_PopulatesColumnsAndPreview()
        {
            var data = SampleData();
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>())).ReturnsAsync(data);
            var vm = CreateViewModel();

            await vm.ImportExcelFileAsync(@"C:\data\people.xlsx");

            Assert.IsTrue(vm.Excel.HasData);
            CollectionAssert.AreEqual(new[] { "Full Name", "ID" }, vm.AvailableColumns.ToArray());
            Assert.AreEqual(2, vm.PreviewTable.Count);
        }

        [TestMethod]
        public async Task AutoBind_MatchesPlaceholderNameToColumn()
        {
            var designPath = WriteDesign("auto", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Full Name" });
                t.FrontSide.Elements.Add(new TextElement { Text = "ID" });
            });
            var data = SampleData();
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>())).ReturnsAsync(data);
            var vm = CreateViewModel();
            await vm.LoadDesignAsync(designPath);

            await vm.ImportExcelFileAsync(@"C:\data\people.xlsx");

            Assert.AreEqual("Full Name", vm.Placeholders[0].BoundColumn);
            Assert.AreEqual("ID", vm.Placeholders[1].BoundColumn);
            Assert.IsTrue(vm.ReadyForProcessing);
        }

        [TestMethod]
        public void BindPlaceholder_UnbindsWithNull()
        {
            var vm = CreateViewModel();
            var placeholder = new DesignPlaceholder(
                DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "Name", SideType.Front)
            {
                BoundColumn = "Name"
            };

            vm.BindPlaceholder(placeholder, null);

            Assert.IsFalse(placeholder.IsBound);
            Assert.IsFalse(vm.ReadyForProcessing);
        }

        [TestMethod]
        public void RenamePlaceholder_RejectsDuplicateNames()
        {
            var vm = CreateViewModel();
            var a = new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "Name", SideType.Front);
            var b = new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "ID", SideType.Front);
            vm.Placeholders.Add(a);
            vm.Placeholders.Add(b);

            vm.RenamePlaceholder(b, "name");

            Assert.AreEqual("ID", b.Name); // rejected, "Name" taken (case-insensitive)
            StringAssert.Contains(vm.StatusText, "already used");
        }

        [TestMethod]
        public void RenamePlaceholder_AcceptsUniqueName()
        {
            var vm = CreateViewModel();
            var a = new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "Name", SideType.Front);
            vm.Placeholders.Add(a);

            vm.RenamePlaceholder(a, "Employee Name");

            Assert.AreEqual("Employee Name", a.Name);
        }

        [TestMethod]
        public void RunValidation_NoDataIsNoOp()
        {
            var vm = CreateViewModel();

            vm.RunValidation();

            Assert.AreEqual(0, vm.Issues.Count);
        }

        [TestMethod]
        public void RunValidation_CleanDataReportsReady()
        {
            var vm = CreateViewModel();
            var placeholder = new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "Full Name", SideType.Front)
            {
                BoundColumn = "Full Name"
            };
            vm.Placeholders.Add(placeholder);
            vm.Excel.TestSetData(SampleData());
            _validationService
                .Setup(v => v.Validate(
                    It.IsAny<ExcelData>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(),
                    It.IsAny<List<PhotoRecord>>(), It.IsAny<string>()))
                .Returns(new List<ValidationIssue>());

            vm.RunValidation();

            StringAssert.Contains(vm.StatusText, "Ready for processing");
            Assert.IsTrue(vm.ReadyForProcessing);
        }

        [TestMethod]
        public void StepVisibility_ThreeStepsTrack()
        {
            var vm = CreateViewModel();

            vm.SelectedStep = 1;
            Assert.IsTrue(vm.ShowMappingStep);
            Assert.IsFalse(vm.ShowImportStep);

            vm.SelectedStep = 2;
            Assert.IsTrue(vm.ShowPreviewStep);
        }

        [TestMethod]
        public void Constructor_NullDesignServiceThrows()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => new DataImportViewModel(null, _excelService.Object, _photoService.Object, _validationService.Object));
        }
    }
}
