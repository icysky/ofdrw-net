using Ofd.Core.Dom;
using Xunit;

namespace Ofd.Tests.Golden;

/// <summary>
/// DomSerializer 与 Java dom4j 输出的字节级对照（7 场景，golden 为 dom4j HEX）。
/// 场景覆盖：命名空间声明/改写（dumpUpNS 语义）、CRLF 规范化、转义、自闭合、CDATA/注释、属性插入序。
/// </summary>
public class XmlGoldenTests
{
    private const string Ns = "http://www.ofdspec.org/2016";

    private static string GoldenPath => Path.Combine(AppContext.BaseDirectory, "TestData", "xml-java.txt");

    public static IEnumerable<object[]> Scenarios()
    {
        yield return new object[] { "std-ofd", (Func<byte[]>)(() =>
        {
            var ofd = new DomElement("OFD", "ofd", Ns);
            ofd.SetAttr("DocType", "OFD").SetAttr("Version", "2.0");
            var body = ofd.Add(new DomElement("DocBody", "ofd", Ns));
            var info = body.Add(new DomElement("DocInfo", "ofd", Ns));
            info.Add(new DomElement("Title", "ofd", Ns)).AddText("测试文档<Title>&\"Quote\"");
            info.Add(new DomElement("Author", "ofd", Ns)).AddText("张三 O'Brien");
            info.Add(new DomElement("DocID", "ofd", Ns)).AddText("FFFF-1111");
            var cd = body.Add(new DomElement("CommonData", "ofd", Ns));
            var pg = cd.Add(new DomElement("PageSpace", "ofd", Ns));
            pg.SetAttr("PhysicalBox", "210 297").SetAttr("Apply", "1")
              .SetAttr("Note", "a&b<c>d\"e'f  g");
            cd.Add(new DomElement("VirtualBox", "ofd", Ns)).SetAttr("Container", "");
            cd.Add(new DomElement("Empty", "ofd", Ns));
            cd.Add(new DomElement("MultiLine", "ofd", Ns)).AddText("line1\nline2\tend");
            return DomSerializer.Dump(ofd);
        }) };
        yield return new object[] { "dirty-upns", (Func<byte[]>)(() =>
        {
            var d = new DomElement("Document");
            d.SetAttr("DocType", "OFD");
            var b = d.Add(new DomElement("DocBody"));
            b.Add(new DomElement("DocID")).AddText("X1");
            b.Add(new DomElement("Res")).SetAttr("BaseLoc", "Res/\u0440es.xml");
            UpNs(d);
            return DomSerializer.Dump(d);
        }) };
        yield return new object[] { "nsdefault-upns", (Func<byte[]>)(() =>
        {
            var d = new DomElement("OFD", "", Ns);
            d.Add(new DomElement("DocBody", "", Ns)).Add(new DomElement("DocID", "", Ns)).AddText("Y2");
            UpNs(d);
            return DomSerializer.Dump(d);
        }) };
        yield return new object[] { "wrongns-upns", (Func<byte[]>)(() =>
        {
            var d = new DomElement("OFD", "ofd", "http://wrong.example.com/ofd");
            d.Add(new DomElement("DocBody", "ofd", "http://wrong.example.com/ofd")).AddText("Z");
            UpNs(d);
            return DomSerializer.Dump(d);
        }) };
        yield return new object[] { "crlf-roundtrip", (Func<byte[]>)(() =>
        {
            var doc = new DomElement("Doc", "", Ns);
            doc.Add(new DomElement("T", "", Ns)).AddText("a\nb");
            doc.Add(new DomElement("U", "", Ns)).AddText("p\nq");
            return DomSerializer.Dump(doc);
        }) };
        yield return new object[] { "cdata-comment", (Func<byte[]>)(() =>
        {
            var c = new DomElement("C");
            c.Kids.Add(new DomCData("raw <&> data"));
            c.Kids.Add(new DomComment(" a comment "));
            c.Kids.Add(new DomCData(""));
            return DomSerializer.Dump(c);
        }) };
        yield return new object[] { "attr-order", (Func<byte[]>)(() =>
        {
            var o = new DomElement("O");
            o.SetAttr("Z", "1").SetAttr("A", "2").SetAttr("M", "3").SetAttr("B", "4");
            return DomSerializer.Dump(o);
        }) };
    }

    /// <summary>把整棵树的命名空间改写为 OFD 标准命名空间（对应 Java OFDNameSpaceModifier.dumpUpNS）。</summary>
    private static void UpNs(DomElement e)
    {
        e.Prefix = "ofd";
        e.Uri = Ns;
        foreach (var k in e.Kids)
            if (k is DomElement c)
                UpNs(c);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Serializer_output_matches_dom4j_byte_for_byte(string tag, Func<byte[]> make)
    {
        var javaHex = LoadGolden();
        Assert.True(javaHex.TryGetValue(tag, out var expected),
            $"golden 中缺少场景 {tag}（xml-java.txt 可能损坏）");

        byte[] net = make();
        string hex = Convert.ToHexString(net);

        if (hex != expected)
        {
            // 失败时给出首个差异字节，便于定位
            int i = 0;
            for (; i < Math.Min(hex.Length, expected.Length); i += 2)
                if (hex[i] != expected[i])
                    break;
            Assert.Fail(
                $"场景 {tag} 字节不一致: net={net.Length}B java={expected.Length / 2}B, " +
                $"首个差异 @{i / 2}: net=0x{hex.Substring(i, Math.Min(16, hex.Length - i))} " +
                $"java=0x{expected.Substring(i, Math.Min(16, expected.Length - i))}");
        }
    }

    private static Dictionary<string, string> LoadGolden()
    {
        Assert.True(File.Exists(GoldenPath), "缺少 golden 文件 xml-java.txt");
        var map = new Dictionary<string, string>();
        string? cur = null;
        foreach (var line in File.ReadLines(GoldenPath))
        {
            if (line.StartsWith("=== ")) cur = line.Substring(4).Split(' ')[0];
            else if (line.StartsWith("HEX:") && cur != null) map[cur] = line.Substring(4).Trim();
        }
        Assert.True(map.Count >= 7, $"golden 场景数异常: {map.Count}");
        return map;
    }
}
