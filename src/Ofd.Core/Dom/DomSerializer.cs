using System.Text;

namespace Ofd.Core.Dom;

/// <summary>
/// dom4j 兼容序列化器——输出字节与 dom4j Writer 写出的字节一致
/// （转义规则、自闭合、属性插入序、命名空间只在根声明、"\n" 分隔声明行）。
/// golden 基准：tests/Ofd.Tests/TestData/xml-java.txt（7 场景字节级一致）。
/// .NET 的 XmlWriter 不可替代（小写 "utf-8"、"\n"→"\r\n"、转义差异），勿改用。
/// </summary>
public static class DomSerializer
{
    public static string EscapeText(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public static string EscapeAttr(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>序列化为字节（UTF-8 无 BOM），首行为 XML 声明 + "\n"。</summary>
    public static byte[] Dump(DomElement root)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        Ser(root, sb, isRoot: true);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void Ser(DomElement e, StringBuilder sb, bool isRoot)
    {
        sb.Append('<');
        if (e.Prefix.Length > 0) sb.Append(e.Prefix).Append(':');
        sb.Append(e.Name);
        if (isRoot && e.Uri.Length > 0)
        {
            sb.Append(" xmlns");
            if (e.Prefix.Length > 0) sb.Append(':').Append(e.Prefix);
            sb.Append("=\"").Append(e.Uri).Append('"');
        }
        foreach (var (k, v) in e.Attrs)
            sb.Append(' ').Append(k).Append("=\"").Append(EscapeAttr(v)).Append('"');
        if (e.Kids.Count == 0)
        {
            sb.Append("/>");
            return;
        }
        sb.Append('>');
        foreach (var k in e.Kids)
        {
            switch (k)
            {
                case DomElement c: Ser(c, sb, false); break;
                case DomText t: sb.Append(EscapeText(t.Text)); break;
                case DomCData cd: sb.Append("<![CDATA[").Append(cd.Text).Append("]]>"); break;
                case DomComment cm: sb.Append("<!--").Append(cm.Text).Append("-->"); break;
            }
        }
        sb.Append("</");
        if (e.Prefix.Length > 0) sb.Append(e.Prefix).Append(':');
        sb.Append(e.Name).Append('>');
    }
}
