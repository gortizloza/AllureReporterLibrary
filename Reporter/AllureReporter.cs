using AllureReporterLibrary.ReportObjects;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Xml.Serialization;

namespace AllureReporterLibrary.Reporter
{
    /// <summary>
    /// Handles operations to generate and save Allure reports
    /// </summary>
    public class Reporter
    {
        // TODO: Include an option to save allure-reports to output directory instead of overwritting existing report
        private readonly string _outputDirectory;
        private readonly string _pathToResults;
        private readonly bool _keepReports;
        private readonly string? _reportsHistoryDir;

        public Reporter(string outputDirectory, string pathToResults, bool keepReports = false, string? reportsHistoryDir = null)
        {
            _outputDirectory = outputDirectory;
            _pathToResults = pathToResults;
            _keepReports = keepReports;
            _reportsHistoryDir = reportsHistoryDir;

        }


        /// <summary>
        /// Call external command "allure generate".
        /// </summary>
        /// <remarks>The default report title is Allure Report</remarks>
        /// <param name="reportTitle">A custom title for the HTML report</param>
        public void GenerateAllureReport(string? reportTitle = null)
        {
            if (_keepReports)
            {
                SavePreviousReport();
            }

            string allureCommand = "allure generate";
            string allureOptions = $"-o {_outputDirectory}/allure-report --clean";

            ProcessStartInfo allureProcessInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = $@"/c {allureCommand} {allureOptions} {_pathToResults}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var allureProcess = new Process
            {
                StartInfo = allureProcessInfo
            };

            allureProcess.Start();
            allureProcess.WaitForExit();
            allureProcess.Close();

            if (reportTitle != null)
            {
                SetReportTitle(reportTitle);
            }
        }

        /// <summary>
        /// Save a previous report, if found in the output directory, to a new timestamped directory.
        /// </summary>
        private void SavePreviousReport()
        {
            var prevRepDir = new DirectoryInfo(Path.Combine(_outputDirectory, "allure-report"));
            if (!prevRepDir.Exists)
            {
                // No report to save
                return;
            }

            string saveReportTo = _reportsHistoryDir == null
                ? Path.Combine(_outputDirectory, $"allure-report-{prevRepDir.CreationTime:yyyy-MM-dd_HH-mm-ss}")
                : Path.Combine(_reportsHistoryDir, $"allure-report-{prevRepDir.CreationTime:yyyy-MM-dd_HH-mm-ss}");



            CopyDirectory(prevRepDir, saveReportTo);

        }

        /// <summary>
        /// Copy a source directory and all its contents recursively to a destination directory.
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destinationDir"></param>
        private void CopyDirectory(DirectoryInfo sourceDir, string destinationDir)
        {
            // Cache directories
            DirectoryInfo[] dirs = sourceDir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (var file in sourceDir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // Recursively continue with subdirectories
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir, newDestDir);
            }
        }

        /// <summary>
        /// Get the history files from the previous report and adds them to allure-results/history, so
        /// they can be included in the next report.
        /// </summary>
        /// <remarks>If pathToReport is not given, searches for a previous report in the outputDirectory</remarks>
        /// <param name="pathToReport">The path to the previous report.</param>
        /// <exception cref="IOException"></exception>
        public void AddPreviousHistoryToResults(string? pathToReport = null)
        {
            string pathToPreviousReportHistory = pathToReport == null ?
                Path.Combine(_outputDirectory, @"allure-report\history") :
                Path.Combine(pathToReport, "history");
            string allureResultsHistoryPath = Path.Combine(_pathToResults, "history");

            // if previous report exists
            if (Directory.Exists(pathToPreviousReportHistory))
            {
                string[] historyFiles = Directory.GetFiles(pathToPreviousReportHistory);
                if (historyFiles.Length > 0)
                {
                    if (Directory.Exists(allureResultsHistoryPath))
                    {
                        Directory.Delete(allureResultsHistoryPath, true);
                    }
                    Directory.CreateDirectory(allureResultsHistoryPath);
                    foreach (var sourcePath in historyFiles)
                    {
                        string fileName = Path.GetFileName(sourcePath);
                        string destinationFile = Path.Combine(allureResultsHistoryPath, fileName);
                        File.Copy(sourcePath, destinationFile);
                    }
                }
                else
                {
                    throw new IOException("Previous test history data was not found");
                }
            }
        }

        /// <summary>
        /// Modify the summary.json file in the report to show a custom title, default is Allure Report
        /// </summary>
        /// <param name="title">t</param>
        private void SetReportTitle(string title)
        {
            string summaryFilePath = Path.Combine(_outputDirectory, @"allure-report\widgets\summary.json");
            JsonSerializer serializer = new();
            ReportSummary? report;

            using (StreamReader sr = new(summaryFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                report = serializer.Deserialize<ReportSummary>(reader);
                report!.reportName = title;
            }

            using (StreamWriter sw = new(summaryFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, report);
            }
        }

        /// <summary>
        /// Include the environment variables in the report.
        /// </summary>
        /// <param name="envVars">A dictionary of strings containing key value pairs of environment variables.</param>
        public void AddEnvironmentVariables(IDictionary<string, string> envVars)
        {
            EnvVars reportEnvVars = new EnvVars();

            foreach (KeyValuePair<string, string> param in envVars)
            {
                reportEnvVars.Parameters
                    .Add(new EnvVarParam { Key = param.Key, Value = param.Value });
            }

            XmlSerializer serializer = new XmlSerializer(typeof(EnvVars));
            TextWriter writer = new StreamWriter(Path.Combine(_pathToResults, "environment.xml"));
            serializer.Serialize(writer, reportEnvVars);

            writer.Close();
        }

    }
}

