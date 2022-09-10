namespace AllureReporterLibrary.ReportObjects
{
    public class Time
    {
        public long start { get; set; }
        public long stop { get; set; }
        public int duration { get; set; }
        public int minDuration { get; set; }
        public int maxDuration { get; set; }
        public int sumDuration { get; set; }
    }
}
