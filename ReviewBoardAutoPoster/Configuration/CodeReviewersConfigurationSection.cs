using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using System.Xml.Serialization;

namespace ReviewBoardTfsAutoMerger.Configuration
{
    public class CodeReviewersConfigurationSection: ConfigurationSection
    {
        public CodeReviewersReviewerInfoCollection Settings { get; set; } 

        protected override void DeserializeSection(XmlReader reader)
        {
            var serializer = new XmlSerializer(typeof(CodeReviewersReviewerInfoCollection));
            var result = (CodeReviewersReviewerInfoCollection)serializer.Deserialize(reader);
            Settings = result;
        }
    }
    
    [XmlRootAttribute(ElementName="codeReview")]
    public class CodeReviewersReviewerInfoCollection
    {
        [XmlElement("codeReviewerInfo")]
        public List<CodeReviewersReviewerInfo> CodeReviewersInfo { get; set; }
    }

    public class CodeReviewersReviewerInfo
    {
        public CodeReviewersReviewerInfo()
        {
            Reviewers = new List<string>();
            Groups = new List<string>();
        }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("reviewer")]
        public List<string> Reviewers { get; set; }

        [XmlElement("group")]
        public List<string> Groups { get; set; }

        [XmlElement("securityGroup")]
        public string SecurityGroup { get; set; }
    }
}