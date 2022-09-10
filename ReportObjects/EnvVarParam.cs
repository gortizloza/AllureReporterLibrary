using System.Xml.Serialization;

namespace AllureReporterLibrary.ReportObjects
{
    [XmlRoot(ElementName = "parameter")]
    public class EnvVarParam
    {
        [XmlElement(ElementName = "key")]
        public string Key { get; set; }

        [XmlElement(ElementName = "value")]
        public string Value { get; set; }
    }
}
