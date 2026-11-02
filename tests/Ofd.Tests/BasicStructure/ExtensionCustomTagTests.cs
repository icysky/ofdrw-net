using System.Text;
using Ofd.Core.BasicType;
using Ofd.Core.CustomTags;
using Ofd.Core.Dom;
using Ofd.Core.Extensions;
using Xunit;

namespace Ofd.Tests.BasicStructure;

/// <summary>扩展信息（第 17 章）与自定义标引（第 16 章）验证。</summary>
public class ExtensionCustomTagTests
{
    [Fact]
    public void Extensions_with_properties_and_extend_data()
    {
        var extension = new CT_Extension()
            .SetAppName("MyApp")
            .SetCompany("某公司")
            .SetAppVersion("1.0.0")
            .SetDate(new DateOnly(2026, 10, 21))
            .SetRefId(ST_RefID.GetInstance(8))
            .AddProperty(new Property("key1", "value1", "string"))
            .AddProperty(new Property("key2", "value2"))
            .AddExtendData(new ST_Loc("Ext/Data1.xml"))
            .AddExtendData(new ST_Loc("Ext/Data2.xml"))
            .AddData(DomParser.Parse("<ofd:Data xmlns:ofd=\"http://www.ofdspec.org/2016\"><ofd:X>1</ofd:X></ofd:Data>"));

        var extensions = new Extensions().AddExtension(extension).AddExtension(null);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(extensions.Proxy));
        var parsed = new Extensions(DomParser.Parse(xml));
        var ext = Assert.Single(parsed.GetExtensions());

        Assert.Equal("MyApp", ext.GetAppName());
        Assert.Equal("某公司", ext.GetCompany());
        Assert.Equal("1.0.0", ext.GetAppVersion());
        Assert.Equal(new DateOnly(2026, 10, 21), ext.GetDate());
        Assert.Equal(8, ext.GetRefId()!.GetRefId()!.Id);

        var props = ext.GetPropertys();
        Assert.Equal(2, props.Count);
        Assert.Equal("key1", props[0].GetExtensionName());
        Assert.Equal("value1", props[0].GetValue());   // 上游读写不对称 bug 已修
        Assert.Equal("string", props[0].GetType());
        Assert.Null(props[1].GetType());

        Assert.Equal(2, ext.GetExtendDatas().Count);
        Assert.Equal("Ext/Data1.xml", ext.GetExtendDatas()[0]!.ToString());
        Assert.Single(ext.GetDatas()); // xs:anyType 原文保留
    }

    [Fact]
    public void Extension_required_and_null_removal()
    {
        var ext = new CT_Extension();
        Assert.Throws<InvalidOperationException>(() => ext.GetAppName());
        Assert.Throws<ArgumentException>(() => ext.SetAppName(" "));

        ext.SetCompany("c").SetDate(new DateOnly(2026, 1, 1)).SetRefId(ST_RefID.GetInstance(1));
        ext.SetCompany(null).SetDate(null).SetRefId(null);
        Assert.Null(ext.Proxy.Attr("Company"));
        Assert.Null(ext.Proxy.Attr("Date"));
        Assert.Null(ext.Proxy.Attr("RefId"));

        // Property 必选名称
        Assert.Throws<ArgumentException>(() => new Property("", "v"));
        Assert.Throws<InvalidOperationException>(() => new Property().GetExtensionName());
    }

    [Fact]
    public void CustomTags_round_trip()
    {
        var tag = new CustomTag()
            .SetTypeID("tag-1")
            .SetNameSpace("http://example.com/ns")
            .SetSchemaLoc(new ST_Loc("Tags/tag-1.xsd"))
            .SetFileLoc(new ST_Loc("Tags/tag-1.xml"));
        var tags = new CustomTags().AddCustomTag(tag).AddCustomTag(null);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(tags.Proxy));
        var parsed = new CustomTags(DomParser.Parse(xml));
        var p = Assert.Single(parsed.GetCustomTags());

        Assert.Equal("tag-1", p.GetTypeID());
        Assert.Equal("http://example.com/ns", p.GetNameSpace());
        Assert.Equal("Tags/tag-1.xsd", p.GetSchemaLoc()!.ToString());
        Assert.Equal("Tags/tag-1.xml", p.GetFileLoc().ToString());

        // 必选校验
        Assert.Throws<InvalidOperationException>(() => new CustomTag().GetTypeID());
        Assert.Throws<InvalidOperationException>(() => new CustomTag().GetFileLoc());
        Assert.Throws<ArgumentException>(() => new CustomTag().SetTypeID(" "));
        Assert.Throws<ArgumentException>(() => new CustomTag().SetFileLoc(null!));

        // null 删除可选元素
        p.SetSchemaLoc(null);
        Assert.Null(p.GetSchemaLoc());
    }
}
