using System.Text;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Core.Integrity;
using Xunit;
using IntegrityFile = Ofd.Core.Integrity.File;

namespace Ofd.Tests.BasicStructure;

/// <summary>完整性保护描述文件（GMT0099 D.1 / 防夹带）验证。</summary>
public class IntegrityTests
{
    [Fact]
    public void OFDEntries_round_trip()
    {
        var fileList = new FileList()
            .AddFile("f1", "Doc_0/Document.xml")
            .AddFile("f2", new ST_Loc("Doc_0/Pages/Page_0/Content.xml"))
            .AddFile(null);

        var entries = new OFDEntries()
            .SetId("entry-root")
            .SetCreatorName("某系统")
            .SetVersion("2.0")
            .SetCreationDate(new DateTime(2026, 10, 21, 0, 0, 0))
            .SetSignedValueLoc(new ST_Loc("OFDEntries.sig"))
            .SetFileList(fileList);

        // 根元素名为 DocEntries（上游构造即如此）
        Assert.Equal("DocEntries", entries.Proxy.Name);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(entries.Proxy));
        var parsed = new OFDEntries(DomParser.Parse(xml));

        Assert.Equal("entry-root", parsed.GetId());
        Assert.Equal("某系统", parsed.GetCreatorName());
        Assert.Equal("2.0", parsed.GetVersion());
        Assert.Equal(new DateTime(2026, 10, 21), parsed.GetCreationDate()); // 日期格式读写对称（上游 bug 已修）
        Assert.Equal("OFDEntries.sig", parsed.GetSignedValueLoc().ToString());

        var files = parsed.GetFileList().GetFiles();
        Assert.Equal(2, files.Count);
        Assert.Equal("f1", files[0].GetId());
        Assert.Equal("Doc_0/Document.xml", files[0].GetFileLoc().ToString());
        Assert.Equal("Doc_0/Pages/Page_0/Content.xml", files[1].GetFileLoc().ToString());
    }

    [Fact]
    public void OFDEntries_required_checks()
    {
        var entries = new OFDEntries();
        Assert.Throws<InvalidOperationException>(() => entries.GetSignedValueLoc());
        Assert.Throws<InvalidOperationException>(() => entries.GetFileList());
        Assert.Throws<ArgumentException>(() => entries.SetSignedValueLoc(null!));
        Assert.Throws<ArgumentException>(() => entries.SetFileList(null!));

        // 可选属性 null 删除
        entries.SetCreatorName("x").SetVersion("1").SetCreationDate(DateTime.Now);
        entries.SetCreatorName(null).SetVersion(null).SetCreationDate(null);
        Assert.Null(entries.Proxy.Attr("CreatorName"));
        Assert.Null(entries.Proxy.Attr("Version"));
        Assert.Null(entries.Proxy.Attr("CreationDate"));
    }

    [Fact]
    public void File_required_attributes()
    {
        Assert.Throws<InvalidOperationException>(() => new IntegrityFile().GetId());
        Assert.Throws<InvalidOperationException>(() => new IntegrityFile().GetFileLoc());
        Assert.Throws<ArgumentException>(() => new IntegrityFile().SetId(null!));

        var f = new IntegrityFile("id1", "a/b.xml");
        Assert.Equal("id1", f.GetId());
        Assert.Equal("a/b.xml", f.GetFileLoc().ToString());
        Assert.Throws<ArgumentException>(() => new IntegrityFile("id2", (ST_Loc)null!));
    }
}
