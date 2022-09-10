namespace AllureReporterLibrary.ReportObjects
{
    public class Statistic
    {
        public int failed { get; set; }
        public int broken { get; set; }
        public int skipped { get; set; }
        public int passed { get; set; }
        public int unknown { get; set; }
        public int total { get; set; }
    }
}
