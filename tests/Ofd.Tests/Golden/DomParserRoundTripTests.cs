using System.Text;
using Ofd.Core.Dom;
using Xunit;

namespace Ofd.Tests.Golden;

/// <summary>
/// DomParser 解析 → Dump 往返字节一致（对 dom4j 产生的 golden 序列化输出做解析回环）。
/// 这验证解析器保留了序列化所需的全部信息：属性插入序、前缀、空白文本、CDATA、注释、自闭合。
/// </summary>
public class DomParserRoundTripTests
{
    private static string GoldenPath => Path.Combine(AppContext.BaseDirectory, "TestData", "xml-java.txt");

    public static IEnumerable<object[]> GoldenScenarios()
    {
        Assert.True(File.Exists(GoldenPath), "缺少 golden 文件 xml-java.txt");
        string? cur = null;
        foreach (var line in File.ReadLines(GoldenPath))
        {
            if (line.StartsWith("=== ")) cur = line.Substring(4).Split(' ')[0];
            else if (line.StartsWith("HEX:") && cur != null)
                yield return new object[] { cur, line.Substring(4).Trim() };
        }
    }

    [Theory]
    [MemberData(nameof(GoldenScenarios))]
    public void Parse_then_Dump_is_byte_identical(string tag, string javaHex)
    {
        string xml = Encoding.UTF8.GetString(Convert.FromHexString(javaHex));
        DomElement root = DomParser.Parse(xml);
        byte[] roundTrip = DomSerializer.Dump(root);
        string hex = Convert.ToHexString(roundTrip);

        Assert.True(hex == javaHex,
            $"场景 {tag} 往返不一致:\n原始: {xml}\n往返: {Encoding.UTF8.GetString(roundTrip)}");
    }

    [Fact]
    public void Parse_preserves_element_access_semantics()
    {
        const string xml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<ofd:OFD xmlns:ofd=\"http://www.ofdspec.org/2016\" DocType=\"OFD\" Version=\"2.0\">" +
            "<ofd:DocBody><ofd:DocInfo><ofd:Title>测试</ofd:Title><ofd:DocID>1</ofd:DocID></ofd:DocInfo></ofd:DocBody></ofd:OFD>";

        var root = DomParser.Parse(xml);
        Assert.Equal("OFD", root.Name);
        Assert.Equal("ofd", root.Prefix);
        Assert.Equal("http://www.ofdspec.org/2016", root.Uri);
        Assert.Equal("OFD", root.Attr("DocType"));
        Assert.Null(root.Attr("xmlns:ofd")); // xmlns 声明不进属性表

        // dom4j element(name) 语义：仅匹配直接子元素
        Assert.Null(root.GetElement("Title")); // Title 是孙子层级，直接子查找为 null
        var info = root.GetElement("DocBody")!.GetElement("DocInfo")!;
        Assert.Equal("测试", info.GetElementText("Title"));
        Assert.Single(info.GetElements("DocID"));
        Assert.Null(root.GetElementText("NotExist"));
    }

    [Fact]
    public void Parse_rejects_dtd_declaration()
    {
        // 与 Java SAXReaderFactory 的 disallow-doctype-decl=true 一致：含 DOCTYPE 直接拒绝（杜绝 XXE）
        const string xml = "<?xml version=\"1.0\"?><!DOCTYPE a SYSTEM \"http://evil/x.dtd\"><a><b>1</b></a>";
        Assert.Throws<System.Xml.XmlException>(() => DomParser.Parse(xml));
    }
}
