using AngleSharp.Parser.Html;
using AngleSharp.Dom.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<status>
  <birth_date>19200709</birth_date>
  <data>
    <date>201008200628</date>
    <keydata>62.15</keydata>
    <model>01000023</model>
    <tag>6021</tag>
  </data>
  <data>
    <date>201008200628</date>
    <keydata>13.00</keydata>
    <model>01000023</model>
    <tag>6022</tag>
  </data>
  <data>
    <date>201008200443</date>
    <keydata>20</keydata>
    <model>00000000</model>
    <tag>6021</tag>
  </data>
  <data>
    <date>201008200443</date>
    <keydata>20</keydata>
    <model>00000000</model>
    <tag>6022</tag>
  </data>
  <height>170</height>
  <sex>male</sex>
</status>
";

            var serializer = new XmlSerializer(typeof(Status));

            using (var reader = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                Status status = (Status)serializer.Deserialize(reader);

                return;
            }
        }
    }

    [XmlRoot("status")]
    public class Status 
    {
        [XmlElement("birth_date")]
        public string BirthDate { get; set; }

        [XmlElement("height")]
        public string Height { get; set; }

        [XmlElement("sex")]
        public string Sex { get; set; }

        [XmlElement("data")]
        public List<Data> Data { get; set; }
    }

    public class Data
    {
        [XmlElement("date")]
        public string Date { get; set; }

        [XmlElement("keydata")]
        public string KeyData { get; set; }

        [XmlElement("model")]
        public string Model { get; set; }

        [XmlElement("tag")]
        public string Tag { get; set; }
    }
}
