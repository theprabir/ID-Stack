using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IDStack.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDStack.Tests.Services
{
    /// <summary>
    /// Performance guardrails from the spec: importing 1000 rows must finish in under 5 seconds.
    /// </summary>
    [TestClass]
    public class ExcelServicePerformanceTests
    {
        [TestMethod]
        [Timeout(5000)]
        public async Task LoadCsv_1000Rows_Under5Seconds()
        {
            var path = Path.Combine(Path.GetTempPath(), "IDStackPerf_" + System.Guid.NewGuid().ToString("N") + ".csv");
            var sb = new StringBuilder();
            sb.AppendLine("Name,ID,Department,Email,Phone");
            for (var i = 1; i <= 1000; i++)
            {
                sb.AppendLine("Person " + i + "," + i + ",Engineering,user" + i + "@example.com,555-01" + i.ToString("00"));
            }

            File.WriteAllText(path, sb.ToString());
            try
            {
                var service = new ExcelService();
                var stopwatch = Stopwatch.StartNew();
                var data = await service.LoadExcelFileAsync(path);
                stopwatch.Stop();

                Assert.AreEqual(1000, data.RowCount);
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000,
                    "Import took " + stopwatch.ElapsedMilliseconds + "ms, limit is 5000ms.");
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
