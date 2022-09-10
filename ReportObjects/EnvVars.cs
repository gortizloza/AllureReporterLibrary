using System.Xml.Serialization;

namespace AllureReporterLibrary.ReportObjects
{

    [XmlRoot(ElementName = "environment")]
    public class EnvVars
    {
        public EnvVars()
        {
            Parameters = new List<EnvVarParam>();
        }

        [XmlElement(ElementName = "parameter")]
        public List<EnvVarParam> Parameters { get; }
    }

}
