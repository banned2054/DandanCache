using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace GetBangumiInfo.Models.Danmaku;

[XmlRoot("i")]
public class ScraperDanmaku
{
    [XmlElement("chatid")]
    public long ChatId { get; set; }

    [XmlElement("chatserver")]
    public string ChatServer { get; set; } = "chat.bilibili.com";

    [XmlElement("mission")]
    public long Mission { get; set; }

    [XmlElement("maxlimit")]
    public long MaxLimit { get; set; } = 3000;

    [XmlElement("state")]
    public int State { get; set; }

    [XmlElement("real_name")]
    public int RealName { get; set; }

    [XmlElement("source")]
    public string Source { get; set; } = "k-v";

    [XmlElement("d")]
    public List<ScraperDanmakuText> Items { get; set; } = [];

    public byte[] ToXml()
    {
        var enc = new UTF8Encoding(); // Remove utf-8 BOM

        using var ms = new MemoryStream();
        var xmlWriterSettings = new XmlWriterSettings()
        {
            // If set to true XmlWriter would close MemoryStream automatically and using would then do double dispose
            // Code analysis does not understand that. That's why there is a suppress message.
            CloseOutput        = false,
            Encoding           = enc,
            OmitXmlDeclaration = false,
            Indent             = false
        };
        using (var xw = XmlWriter.Create(ms, xmlWriterSettings))
        {
            var xmlSerializer = new XmlSerializer(typeof(ScraperDanmaku));
            var ns            = new XmlSerializerNamespaces();
            ns.Add("", "");
            xmlSerializer.Serialize(xw, this, ns);
        }

        return ms.ToArray();
    }
}
