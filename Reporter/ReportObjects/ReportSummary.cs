namespace AllureReporterLibrary.ReportObjects
{
    public class ReportSummary
    {
        public string? reportName { get; set; }
        public List<object>? testRuns { get; set; }
        public Statistic? statistic { get; set; }
        public Time? time { get; set; }
    }
}
