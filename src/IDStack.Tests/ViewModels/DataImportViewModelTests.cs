using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Import;
using IDStack.Core.Models.Template;
using IDStack.ViewModels.DataImport;
using IDStack.ViewModels.TemplateEditor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IDStack.Tests.ViewModels
{
    /// <summary>
    /// Tests for the Data Import view models (Excel, mapping, photos, orchestration).
    /// </summary>
    [TestClass]
    public class DataImportViewModelTests
    {
        private Mock<IExcelService> _excelService;
        private Mock<IPhotoService> _photoService;
        private Mock<IDataValidationService> _validationService;
        private TemplateEditorViewModel _editor;

        [TestInitialize]
        public void Setup()
        {
            _excelService = new Mock<IExcelService>();
            _photoService = new Mock<IPhotoService>(MockBehavior.Strict);
            _validationService = new Mock<IDataValidationService>();
            _editor = new TemplateEditorViewModel(
                new IDStack.Services.TemplateService(),
                new IDStack.Services.HistoryService<EditorState>(),
                new Mock<ILocalizationService>().Object);
        }

        private DataImportViewModel CreateViewModel()
        {
            return new DataImportViewModel(
                _excelService.Object,
                _photoService.Object,
                _validationService.Object,
                _editor);
        }

        private static ExcelData SampleData()
        {
            var data = new ExcelData
            {
                SourceFilePath = @"C:\data\people.xlsx",
                Format = "xlsx",
                ColumnNames = new List<string> { "Name", "ID" }
            };
            data.Rows.Add(new DataRow(new Dictionary<string, string> { ["Name"] = "Alice", ["ID"] = "1" }) { RowNumber = 1 });
            data.Rows.Add(new DataRow(new Dictionary<string, string> { ["Name"] = "Bob", ["ID"] = "2" }) { RowNumber = 2 });
            return data;
        }

        [TestMethod]
        public async Task LoadFileAsync_InvalidFileShowsError()
        {
            _excelService
                .Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny))
                .Callback(new ValidateCallback((string p, out string m) => m = "bad"))
                .Returns(false);
            var vm = CreateViewModel();

            await vm.Excel.LoadFileAsync(@"C:\nope.csv");

            StringAssert.Contains(vm.Excel.StatusText, "bad");
            Assert.IsNull(vm.Excel.Data);
        }

        [TestMethod]
        public async Task LoadFileAsync_SuccessPopulatesData()
        {
            var data = SampleData();
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>())).ReturnsAsync(data);
            var vm = CreateViewModel();

            await vm.Excel.LoadFileAsync(@"C:\data\people.xlsx");

            Assert.IsTrue(vm.Excel.HasData);
            Assert.AreEqual("people.xlsx", vm.Excel.FileName);
            StringAssert.Contains(vm.Excel.StatusText, "2 rows");
        }

        [TestMethod]
        public async Task LoadFileAsync_EmptyDataShowsMessage()
        {
            var empty = new ExcelData { ColumnNames = new List<string> { "A" } };
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>())).ReturnsAsync(empty);
            var vm = CreateViewModel();

            await vm.Excel.LoadFileAsync(@"C:\data\empty.xlsx");

            Assert.IsFalse(vm.Excel.HasData);
            StringAssert.Contains(vm.Excel.StatusText, "no data rows");
        }

        [TestMethod]
        public async Task LoadFileAsync_ServiceErrorIsCaught()
        {
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("corrupt file"));
            var vm = CreateViewModel();

            await vm.Excel.LoadFileAsync(@"C:\data\bad.xlsx");

            StringAssert.Contains(vm.Excel.StatusText, "corrupt file");
            Assert.IsFalse(vm.Excel.IsLoading);
        }

        [TestMethod]
        public async Task DataLoaded_BuildsMappingWithTemplatePlaceholders()
        {
            var data = SampleData();
            _excelService.Setup(e => e.ValidateExcelFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            _excelService.Setup(e => e.LoadExcelFileAsync(It.IsAny<string>())).ReturnsAsync(data);
            _editor.AddElement(new Core.Models.Elements.PlaceholderElement { ColumnName = "Name" });
            var vm = CreateViewModel();
            var mappingChanged = new List<string>();
            vm.Mapping.Mappings.CollectionChanged += (s, e) => mappingChanged.Add("x");

            await vm.Excel.LoadFileAsync(@"C:\data\people.xlsx");

            Assert.AreEqual(1, vm.Mapping.Mappings.Count);
            Assert.AreEqual("Name", vm.Mapping.Mappings[0].PlaceholderName);
            Assert.AreEqual("Name", vm.Mapping.Mappings[0].ColumnName); // auto-matched
            Assert.AreEqual("Alice", vm.Mapping.Mappings[0].SampleValue);
        }

        [TestMethod]
        public void RunValidation_PopulatesIssuesAndSwitchesStep()
        {
            var vm = CreateViewModel();
            vm.Excel.TestSetData(SampleData());
            vm.Mapping.BuildFrom(_editor.Template, vm.Excel.Data);
            _validationService
                .Setup(v => v.Validate(It.IsAny<ExcelData>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<List<PhotoRecord>>()))
                .Returns(new List<ValidationIssue>
                {
                    new ValidationIssue(ValidationIssue.SeverityLevel.Warning, 1, "test issue")
                });

            vm.RunValidation();

            Assert.AreEqual(1, vm.Issues.Count);
            Assert.AreEqual(3, vm.SelectedStep);
            StringAssert.Contains(vm.StatusText, "warning");
        }

        [TestMethod]
        public void RunValidation_CleanDataReportsAllValid()
        {
            var vm = CreateViewModel();
            vm.Excel.TestSetData(SampleData());
            vm.Mapping.BuildFrom(_editor.Template, vm.Excel.Data);
            _validationService
                .Setup(v => v.Validate(It.IsAny<ExcelData>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<List<PhotoRecord>>()))
                .Returns(new List<ValidationIssue>());

            vm.RunValidation();

            StringAssert.Contains(vm.StatusText, "All 2 rows are valid");
        }

        [TestMethod]
        public void SelectedStep_StepVisibilityFlagsTrack()
        {
            var vm = CreateViewModel();

            vm.SelectedStep = 2;
            Assert.IsTrue(vm.ShowPhotosStep);
            Assert.IsFalse(vm.ShowExcelStep);

            vm.SelectedStep = 1;
            Assert.IsTrue(vm.ShowMappingStep);
            Assert.AreEqual(1, vm.SelectedStep);
        }

        [TestMethod]
        public void CanGenerate_RequiresDataAndMappedPlaceholders()
        {
            var vm = CreateViewModel();
            Assert.IsFalse(vm.CanGenerate);

            vm.Excel.TestSetData(SampleData());
            vm.Mapping.BuildFrom(_editor.Template, vm.Excel.Data);
            Assert.IsTrue(vm.CanGenerate); // no placeholders → nothing required
        }

        [TestMethod]
        public void Constructor_NullArgumentsThrow()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => new DataImportViewModel(null, _photoService.Object, _validationService.Object, _editor));
            Assert.ThrowsException<ArgumentNullException>(
                () => new DataImportViewModel(_excelService.Object, null, _validationService.Object, _editor));
            Assert.ThrowsException<ArgumentNullException>(
                () => new DataImportViewModel(_excelService.Object, _photoService.Object, null, _editor));
            Assert.ThrowsException<ArgumentNullException>(
                () => new DataImportViewModel(_excelService.Object, _photoService.Object, _validationService.Object, null));
        }

        private delegate void ValidateCallback(string path, out string errorMessage);
    }
}
