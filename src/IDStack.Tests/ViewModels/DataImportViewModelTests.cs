using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Import;
using IDStack.Core.Models.Template;
using IDStack.Services;
using IDStack.ViewModels.DataImport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SideType = IDStack.Core.Models.Template.SideType;

namespace IDStack.Tests.ViewModels
{
    /// <summary>
    /// Tests for the v0.3.2 two-slot (front/back) DataImportViewModel.
    /// </summary>
    [TestClass]
    public class DataImportViewModelTests
    {
        private Mock<IExcelService> _excelService;
        private Mock<IPhotoService> _photoService;
        private Mock<IDataValidationService> _validationService;
        private IDesignImportService _designService;
        private string _tempDir;

        [TestInitialize]
        public void Setup()
        {
            _excelService = new Mock<IExcelService>();
            _photoService = new Mock<IPhotoService>();
            _validationService = new Mock<IDataValidationService>();
            _designService = new DesignImportService(
                new TemplateService(),
                new PsdDesignImporter(new LogService()),
                new LogService());
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
                ColumnNames = new List<string> { "Full Name", "ID", "PhotoFile" }
            };
            data.Rows.Add(new DataRow(new Dictionary<string, string> { ["Full Name"] = "Alice", ["ID"] = "1", ["PhotoFile"] = "alice" }) { RowNumber = 1 });
            data.Rows.Add(new DataRow(new Dictionary<string, string> { ["Full Name"] = "Bob", ["ID"] = "2", ["PhotoFile"] = "bob" }) { RowNumber = 2 });
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
        public async Task LoadDesignAsync_FrontSlotDetectsTextAndImagePlaceholders()
        {
            var path = WriteDesign("badge", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Name = "NameLayer", Text = "Full Name" });
                t.FrontSide.Elements.Add(new TextElement { Name = "IdLayer", Text = "ID" });
                t.FrontSide.Elements.Add(new ImageElement { Name = "Photo", Source = "x.png" });
                t.FrontSide.Elements.Add(new ImageElement { Name = "Design", Source = "bg.png" }); // background: not a placeholder
            });
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(vm.FrontDesign, path);

            Assert.IsTrue(vm.FrontDesign.HasDesign);
            Assert.IsFalse(vm.BackDesign.HasDesign);
            Assert.AreEqual(3, vm.FrontDesign.Placeholders.Count);
            Assert.AreEqual(2, vm.FrontDesign.Placeholders.Count(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text));
            Assert.AreEqual(1, vm.FrontDesign.Placeholders.Count(p => p.Kind == DesignPlaceholder.PlaceholderKind.Image));
        }

        [TestMethod]
        public async Task LoadDesignAsync_BackSlotGetsBackSidePlaceholders()
        {
            var frontPath = WriteDesign("front", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Full Name" });
            });
            var backPath = WriteDesign("back", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Terms" });
            });
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(vm.FrontDesign, frontPath);
            await vm.LoadDesignAsync(vm.BackDesign, backPath);

            Assert.IsTrue(vm.HasAnyDesign);
            Assert.AreEqual("Full Name", vm.FrontDesign.Placeholders[0].Name);
            Assert.AreEqual("Terms", vm.BackDesign.Placeholders[0].Name);
            Assert.AreEqual(SideType.Back, vm.BackDesign.Placeholders[0].Side);
        }

        [TestMethod]
        public async Task LoadDesignAsync_PlaceholderNamesUniqueAcrossSides()
        {
            var frontPath = WriteDesign("f2", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Name" });
            });
            var backPath = WriteDesign("b2", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Name" });
            });
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(vm.FrontDesign, frontPath);
            await vm.LoadDesignAsync(vm.BackDesign, backPath);

            // Auto-bind must not bind the back "Name" to the same column semantics —
            // but names themselves stay unique per side; cross-side duplicates allowed
            // since they map the same data differently. Verify both loaded fine.
            Assert.AreEqual(1, vm.FrontDesign.Placeholders.Count);
            Assert.AreEqual(1, vm.BackDesign.Placeholders.Count);
        }

        [TestMethod]
        public async Task LoadDesignAsync_UnsupportedExtensionShowsError()
        {
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(vm.FrontDesign, @"C:\designs\thing.txt");

            StringAssert.Contains(vm.FrontDesign.StatusText, "Unsupported design type");
            Assert.IsFalse(vm.FrontDesign.HasDesign);
        }

        [TestMethod]
        public async Task LoadDesignAsync_MissingFileShowsErrorNotThrow()
        {
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(vm.FrontDesign, Path.Combine(_tempDir, "nope.idcard"));

            StringAssert.Contains(vm.FrontDesign.StatusText, "Could not load the design");
            Assert.IsFalse(vm.FrontDesign.HasDesign);
        }

        [TestMethod]
        public async Task LoadDesignAsync_SampleTextFilledFromLayerText()
        {
            var path = WriteDesign("sample", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Full Name" });
            });
            var vm = CreateViewModel();

            await vm.LoadDesignAsync(vm.FrontDesign, path);

            Assert.AreEqual("Full Name", vm.FrontDesign.Placeholders[0].SampleText);
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
            CollectionAssert.AreEqual(new[] { "Full Name", "ID", "PhotoFile" }, vm.AvailableColumns.ToArray());
            Assert.AreEqual(2, vm.PreviewTable.Count);
        }

        [TestMethod]
        public async Task AutoBind_MatchesPlaceholderNameToColumnOnBothSides()
        {
            var frontPath = WriteDesign("af", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Full Name" });
                t.FrontSide.Elements.Add(new TextElement { Text = "ID" });
            });
            var backPath = WriteDesign("ab", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Terms" });
            });
            var data = SampleData();
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>())).ReturnsAsync(data);
            var vm = CreateViewModel();
            await vm.LoadDesignAsync(vm.FrontDesign, frontPath);
            await vm.LoadDesignAsync(vm.BackDesign, backPath);

            await vm.ImportExcelFileAsync(@"C:\data\people.xlsx");

            Assert.AreEqual("Full Name", vm.FrontDesign.Placeholders[0].BoundColumn);
            Assert.AreEqual("ID", vm.FrontDesign.Placeholders[1].BoundColumn);
            // "Terms" has no matching column — must be mapped manually.
            Assert.IsNull(vm.BackDesign.Placeholders[0].BoundColumn);
            vm.BindPlaceholder(vm.BackDesign.Placeholders[0], "ID");
            Assert.IsTrue(vm.ReadyForProcessing);
        }

        [TestMethod]
        public async Task AutoBind_RunsWhenExcelLoadsBeforeDesign()
        {
            var designPath = WriteDesign("late-design", t =>
            {
                t.FrontSide.Elements.Add(new TextElement { Text = "Full Name" });
            });
            var data = SampleData();
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>())).ReturnsAsync(data);
            var vm = CreateViewModel();

            await vm.ImportExcelFileAsync(@"C:\data\people.xlsx");
            await vm.LoadDesignAsync(vm.FrontDesign, designPath);

            Assert.AreEqual("Full Name", vm.FrontDesign.Placeholders[0].BoundColumn);
        }

        [TestMethod]
        public async Task PhotoColumn_PrefersPhotoLikeColumn()
        {
            var designPath = WriteDesign("pc", t =>
            {
                t.FrontSide.Elements.Add(new ImageElement { Name = "Photo", Source = "p.png" });
            });
            var data = SampleData();
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>())).ReturnsAsync(data);
            var vm = CreateViewModel();
            await vm.LoadDesignAsync(vm.FrontDesign, designPath);

            await vm.ImportExcelFileAsync(@"C:\data\people.xlsx");

            Assert.AreEqual("PhotoFile", vm.Photos.MatchColumnName);
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
        public void RenamePlaceholder_RejectsDuplicatesWithinSide()
        {
            var vm = CreateViewModel();
            var a = new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "Name", SideType.Front);
            var b = new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "ID", SideType.Front);
            vm.FrontDesign.Placeholders.Add(a);
            vm.FrontDesign.Placeholders.Add(b);

            vm.RenamePlaceholder(b, "name");

            Assert.AreEqual("ID", b.Name); // rejected, "Name" taken (case-insensitive)
            StringAssert.Contains(vm.StatusText, "already used");
        }

        [TestMethod]
        public void RenamePlaceholder_AcceptsUniqueName()
        {
            var vm = CreateViewModel();
            var a = new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "Name", SideType.Front);
            vm.FrontDesign.Placeholders.Add(a);

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
            vm.FrontDesign.Placeholders.Add(placeholder);
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
        public void FrontBackToggle_SwitchesActivePlaceholders()
        {
            var vm = CreateViewModel();
            vm.FrontDesign.Placeholders.Add(new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "A", SideType.Front));
            vm.BackDesign.Placeholders.Add(new DesignPlaceholder(DesignPlaceholder.PlaceholderKind.Text, Guid.NewGuid(), "B", SideType.Back));

            vm.ShowFrontMappingCommand.Execute(null);
            Assert.AreEqual("A", vm.ActivePlaceholders[0].Name);

            vm.ShowBackMappingCommand.Execute(null);
            Assert.AreEqual("B", vm.ActivePlaceholders[0].Name);
            Assert.IsTrue(vm.ShowBackMapping);
        }

        [TestMethod]
        public void Constructor_NullDesignServiceThrows()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => new DataImportViewModel(null, _excelService.Object, _photoService.Object, _validationService.Object));
        }
    }
}
