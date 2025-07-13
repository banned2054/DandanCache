using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace GetBangumiInfo.Models.Danmaku;

public class ScraperDanmakuText : IXmlSerializable
{
    public long Id { get; set; } //弹幕dmID

    /// <summary>
    /// 出现时间(单位ms)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 弹幕类型 1 2 3:普通弹幕 4:底部弹幕 5:顶部弹幕 6:逆向弹幕 7:高级弹幕 8:代码弹幕 9:BAS弹幕(pool必须为2)
    /// </summary>
    public int Mode { get; set; }

    public int FontSize { get; set; } = 25; //文字大小

    /// <summary>
    /// 弹幕颜色，默认白色
    /// </summary>
    public uint Color { get; set; } = 16777215;

    public string MidHash  { get; set; } //发送者UID的HASH
    public string Content  { get; set; } //弹幕内容
    public long   SendTime { get; set; } //发送时间

    public int Weight { get; set; } = 1; //权重

    //public string Action { get; set; }    //动作？
    public int Pool { get; set; } //弹幕池

    public XmlSchema? GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        // bilibili弹幕格式：
        // <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
        // time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
        var time = (Convert.ToDouble(Progress) / 1000).ToString("F05");
        var attr = $"{time},{Mode},{FontSize},{Color},{SendTime},{Pool},{MidHash},{Id},{Weight}";
        writer.WriteAttributeString("p", attr);
        writer.WriteString(IsValidXmlString(Content) ? Content : RemoveInvalidXmlChars(Content));
    }

    private static string RemoveInvalidXmlChars(string text)
    {
        var validXmlChars = text.Where(XmlConvert.IsXmlChar).ToArray();
        return new string(validXmlChars);
    }

    private static bool IsValidXmlString(string text)
    {
        try
        {
            XmlConvert.VerifyXmlChars(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
