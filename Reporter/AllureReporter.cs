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
        private readonly string _outputDirectory;
        private readonly string _pathToResults;
        private readonly bool _keepHistory;

        public bool KeepReports { get; set; }
        public string? ReportsHistoryDir { get; set; }
        public string? ReportTitle { get; set; }
        public IDictionary<string, string>? EnvVars { get; set; }

        /// <summary>
        /// Initialize a new instance of the Reporter class.
        /// </summary>
        /// <param name="outputDirectory">The path to the directory to create the allure-report directory.</param>
        /// <param name="pathToResults">The path to the directory that contains the allure-results directory.</param>
        /// <param name="keepHistory">Wether to save the previous reports history to create a trendline in the report, default is true.</param>
        public Reporter(string outputDirectory, string pathToResults, bool keepHistory = true)
        {
            _outputDirectory = outputDirectory;
            _pathToResults = pathToResults;
            _keepHistory = keepHistory;
        }

        /// <summary>
        /// Generate a new allure-report.
        /// </summary>
        public void GenerateAllureReport()
        {
            if (_keepHistory)
            {
                AddPreviousHistoryToResults();
            }
            if (KeepReports)
            {
                SavePreviousReport();
            }
            if (EnvVars != null)
            {
                AddEnvironmentVariables();
            }

            ExecuteAllureCommand();

            if (ReportTitle != null)
            {
                SetReportTitle(ReportTitle);
            }
        }

        private void ExecuteAllureCommand()
        {
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
        }

        private void SavePreviousReport()
        {
            var prevRepDir = new DirectoryInfo(Path.Combine(_outputDirectory, "allure-report"));
            if (!prevRepDir.Exists)
            {
                // No report to save
                return;
            }

            string saveReportTo = ReportsHistoryDir == null
                ? Path.Combine(_outputDirectory, $"allure-report-{prevRepDir.CreationTime:yyyy-MM-dd_HH-mm-ss}")
                : Path.Combine(ReportsHistoryDir, $"allure-report-{prevRepDir.CreationTime:yyyy-MM-dd_HH-mm-ss}");

            CopyDirectory(prevRepDir, saveReportTo);
        }


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

        private void AddPreviousHistoryToResults()
        {
            string pathToPreviousReportHistory = Path.Combine(_outputDirectory, @"allure-report\history");
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

        private void AddEnvironmentVariables()
        {
            EnvVars reportEnvVars = new EnvVars();

            foreach (KeyValuePair<string, string> param in EnvVars!)
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

