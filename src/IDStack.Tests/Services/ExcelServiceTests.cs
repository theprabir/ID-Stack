using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDStack.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDStack.Tests.Services
{
    /// <summary>
    /// Tests for ExcelService: CSV parsing, validation, preview, and row loading.
    /// </summary>
    [TestClass]
    public class ExcelServiceTests
    {
        private ExcelService _service;
        private string _tempDir;

        [TestInitialize]
        public void Setup()
        {
            _service = new ExcelService();
            _tempDir = Path.Combine(Path.GetTempPath(), "IDStackExcelTests_" + Guid.NewGuid().ToString("N"));
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
                // Best effort cleanup.
            }
        }

        private string WriteCsv(string name, params string[] lines)
        {
            var path = Path.Combine(_tempDir, name);
            File.WriteAllLines(path, lines, Encoding.UTF8);
            return path;
        }

        [TestMethod]
        public async Task LoadCsv_ParsesColumnsAndRows()
        {
            var path = WriteCsv("people.csv",
                "Name,ID,Department",
                "Alice,101,Engineering",
                "Bob,102,Sales");

            var data = await _service.LoadExcelFileAsync(path);

            CollectionAssert.AreEqual(new[] { "Name", "ID", "Department" }, data.ColumnNames);
            Assert.AreEqual(2, data.RowCount);
            Assert.AreEqual("Alice", data.Rows[0].Get("Name"));
            Assert.AreEqual("102", data.Rows[1].Get("id"));
            Assert.AreEqual("csv", data.Format);
        }

        [TestMethod]
        public async Task LoadCsv_MissingCellsBecomeEmpty()
        {
            var path = WriteCsv("ragged.csv", "A,B", "one", "x,y");

            var data = await _service.LoadExcelFileAsync(path);

            Assert.AreEqual("", data.Rows[0].Get("B"));
            Assert.AreEqual("y", data.Rows[1].Get("B"));
        }

        [TestMethod]
        public async Task LoadCsv_QuotedValuesWithCommas()
        {
            var path = WriteCsv("quoted.csv", "Name,Note", "\"Doe, John\",\"Hello, world\"");

            var data = await _service.LoadExcelFileAsync(path);

            Assert.AreEqual("Doe, John", data.Rows[0].Get("Name"));
            Assert.AreEqual("Hello, world", data.Rows[0].Get("Note"));
        }

        [TestMethod]
        public async Task LoadCsv_BlankLinesSkipped()
        {
            var path = WriteCsv("blanks.csv", "A", "one", "", "two");

            var data = await _service.LoadExcelFileAsync(path);

            Assert.AreEqual(2, data.RowCount);
        }

        [TestMethod]
        public async Task GetPreview_ReturnsOnlyRequestedRows()
        {
            var path = WriteCsv("many.csv", "N",
                "1", "2", "3", "4", "5");

            var preview = await _service.GetPreviewAsync(path, 2);

            Assert.AreEqual(2, preview.RowCount);
        }

        [TestMethod]
        public async Task GetAllRows_ReturnsAllRows()
        {
            var path = WriteCsv("many.csv", "N", "1", "2", "3", "4", "5");

            var rows = await _service.GetAllRowsAsync(path);

            Assert.AreEqual(5, rows.Count);
        }

        [TestMethod]
        public async Task GetColumnNames_ReturnsHeadersOnly()
        {
            var path = WriteCsv("headers.csv", "X,Y,Z", "1,2,3");

            var columns = await _service.GetColumnNamesAsync(path);

            CollectionAssert.AreEqual(new[] { "X", "Y", "Z" }, columns);
        }

        [TestMethod]
        public void ValidateExcelFile_MissingFileFails()
        {
            var ok = _service.ValidateExcelFile(
                Path.Combine(_tempDir, "nope.csv"), out var error);

            Assert.IsFalse(ok);
            Assert.IsNotNull(error);
        }

        [TestMethod]
        public void ValidateExcelFile_UnsupportedExtensionFails()
        {
            var path = Path.Combine(_tempDir, "data.txt");
            File.WriteAllText(path, "A,B");
            var ok = _service.ValidateExcelFile(path, out var error);

            Assert.IsFalse(ok);
            StringAssert.Contains(error, "xlsx");
        }

        [TestMethod]
        public void ValidateExcelFile_NullPathFails()
        {
            Assert.IsFalse(_service.ValidateExcelFile(null, out var error));
            Assert.IsNotNull(error);
        }

        [TestMethod]
        public async Task LoadFile_UnsupportedExtensionThrows()
        {
            var path = Path.Combine(_tempDir, "data.txt");
            File.WriteAllText(path, "A,B");

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.LoadExcelFileAsync(path));
        }

        [TestMethod]
        public async Task LoadCsv_EmptyFileThrows()
        {
            var path = WriteCsv("empty.csv", "");

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.LoadExcelFileAsync(path));
        }

        [TestMethod]
        public async Task LoadCsv_EmptyHeaderGetsAutoName()
        {
            var path = WriteCsv("anon.csv", "A,,C", "1,2,3");

            var data = await _service.LoadExcelFileAsync(path);

            Assert.AreEqual("Column2", data.ColumnNames[1]);
            Assert.AreEqual("2", data.Rows[0].Get("Column2"));
        }
    }
}
