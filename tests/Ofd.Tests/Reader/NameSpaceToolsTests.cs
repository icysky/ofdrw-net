using System.Text;
using Ofd.Core;
using Ofd.Core.Dom;
using Ofd.Reader.Tools;
using Xunit;

namespace Ofd.Tests.Reader;

/// <summary>Reader.tools 命名空间工具（清理/变更）验证。</summary>
public class NameSpaceToolsTests
{
    [Fact]
    public void NameSpaceCleaner_removes_all_namespaces()
    {
        var doc = DomParser.Parse("<ofd:OFD xmlns:ofd=\"http://www.ofdspec.org/2016\"><ofd:DocBody/></ofd:OFD>");
        Assert.Equal("ofd", doc.Prefix);

        NameSpaceCleaner.Clean(doc);

        Assert.Equal("", doc.Prefix);
        Assert.Equal("", doc.Uri);
        Assert.Equal("", doc.GetElement("DocBody")!.Prefix);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(doc));
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<OFD>", xml);
    }

    [Fact]
    public void NameSpaceModifier_sets_expected_namespace()
    {
        var doc = DomParser.Parse("<Document><Pages/></Document>");
        NameSpaceModifier.Modify(doc);

        Assert.Equal(Const.OFD_PREFIX, doc.Prefix);
        Assert.Equal(Const.OFD_NAMESPACE, doc.Uri);
        Assert.Equal("ofd", doc.GetElement("Pages")!.Prefix);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(doc));
        Assert.Contains("xmlns:ofd=\"http://www.ofdspec.org/2016\"", xml);
        Assert.Contains("<ofd:Pages/>", xml);

        // 自定义命名空间
        var doc2 = DomParser.Parse("<Document/>");
        NameSpaceModifier.Modify(doc2, "x", "urn:custom");
        Assert.Equal("x", doc2.Prefix);
        Assert.Equal("urn:custom", doc2.Uri);
    }
}
