using System.Collections.Generic;
using System.Linq;
using IDStack.Core.Models.Import;
using IDStack.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDStack.Tests.Services
{
    /// <summary>
    /// Tests for DataValidationService: required columns, duplicate IDs, photo checks.
    /// </summary>
    [TestClass]
    public class DataValidationServiceTests
    {
        private DataValidationService _service = new DataValidationService();

        private ExcelData BuildData(params string[] nameValues)
        {
            var data = new ExcelData { ColumnNames = new List<string> { "Name", "ID" } };
            var names = nameValues.Length > 0 ? nameValues : new[] { "Alice", "Bob" };
            for (var i = 0; i < names.Length; i++)
            {
                var row = new DataRow(new Dictionary<string, string>
                {
                    ["Name"] = names[i],
                    ["ID"] = (100 + i).ToString()
                })
                { RowNumber = i + 1 };
                data.Rows.Add(row);
            }

            return data;
        }

        [TestMethod]
        public void Validate_CleanDataHasNoIssues()
        {
            var issues = _service.Validate(BuildData(), new[] { "Name" }, "ID", null);

            Assert.AreEqual(0, issues.Count);
        }

        [TestMethod]
        public void Validate_NullDataReportsError()
        {
            var issues = _service.Validate(null, null, null, null);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(ValidationIssue.SeverityLevel.Error, issues[0].Severity);
        }

        [TestMethod]
        public void Validate_EmptyRowsReportsError()
        {
            var data = new ExcelData { ColumnNames = new List<string> { "Name" } };

            var issues = _service.Validate(data, null, null, null);

            StringAssert.Contains(issues[0].Message, "no rows");
        }

        [TestMethod]
        public void Validate_MissingRequiredColumnReported()
        {
            var issues = _service.Validate(BuildData(), new[] { "Salary" }, null, null);

            Assert.AreEqual(1, issues.Count);
            StringAssert.Contains(issues[0].Message, "Salary");
        }

        [TestMethod]
        public void Validate_EmptyRequiredValueReportedPerRow()
        {
            var issues = _service.Validate(BuildData("Alice", ""), new[] { "Name" }, null, null);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(2, issues[0].RowNumber);
        }

        [TestMethod]
        public void Validate_DuplicateIdReportedAsWarning()
        {
            var data = BuildData();
            data.Rows[1].Values["ID"] = "100"; // same as row 1

            var issues = _service.Validate(data, null, "ID", null);

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(ValidationIssue.SeverityLevel.Warning, issues[0].Severity);
            StringAssert.Contains(issues[0].Message, "Duplicate");
        }

        [TestMethod]
        public void Validate_MissingPhotoFileReportedAsWarning()
        {
            var data = BuildData();
            data.ColumnNames.Add("Photo");
            foreach (var row in data.Rows)
            {
                row.Values["Photo"] = "missing.png";
            }

            var photos = new List<PhotoRecord>();
            var issues = _service.Validate(data, null, null, photos, "Photo");

            Assert.AreEqual(2, issues.Count);
            Assert.IsTrue(issues.All(i => i.Severity == ValidationIssue.SeverityLevel.Warning));
        }

        [TestMethod]
        public void Validate_ExistingPhotoNotReported()
        {
            var data = BuildData();
            data.ColumnNames.Add("Photo");
            foreach (var row in data.Rows)
            {
                row.Values["Photo"] = "found.png";
            }

            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "found.png");
            System.IO.File.WriteAllText(tempPath, "fake");
            try
            {
                var photos = new List<PhotoRecord> { new PhotoRecord(tempPath) };
                var issues = _service.Validate(data, null, null, photos, "Photo");
                Assert.AreEqual(0, issues.Count);
            }
            finally
            {
                System.IO.File.Delete(tempPath);
            }
        }

        [TestMethod]
        public void Validate_IssuesOrderedByRow()
        {
            var issues = _service.Validate(BuildData("", ""), new[] { "Name" }, null, null);

            Assert.IsTrue(issues[0].RowNumber <= issues[1].RowNumber);
        }
    }
}
