using System.Text;
using Ofd.Core;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Pkg;
using Ofd.Pkg.Container;
using Xunit;

namespace Ofd.Tests.Pkg;

/// <summary>VirtualContainer + ElemCup（文件级虚拟容器与序列化落盘）。</summary>
public class VirtualContainerTests : IDisposable
{
    private readonly string _root;

    public VirtualContainerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    [Fact]
    public void PutObj_flush_load_round_trip()
    {
        var container = new VirtualContainer(_root);
        var ofd = new OFD();
        ofd.AddDocBody(new Ofd.Core.BasicStructure.Ofd.DocBody()
            .SetDocRoot(new ST_Loc("Doc_0/Document.xml")));
        container.PutObj("OFD.xml", ofd);

        // 未 flush 前文件不存在（缓存在内存）
        Assert.False(File.Exists(Path.Combine(_root, "OFD.xml")));

        container.Flush();

        string file = Path.Combine(_root, "OFD.xml");
        Assert.True(File.Exists(file));
        string xml = File.ReadAllText(file, Encoding.UTF8);
        Assert.Contains("<ofd:OFD xmlns:ofd=\"http://www.ofdspec.org/2016\"", xml);
        Assert.Contains("DocRoot", xml);

        // 从文件加载（缓存已清空）
        var loaded = container.GetObjByFile("OFD.xml");
        Assert.Equal("OFD", loaded.Name);
        Assert.Equal("http://www.ofdspec.org/2016", loaded.Uri);
    }

    [Fact]
    public void Flush_only_writes_changed_files()
    {
        var container = new VirtualContainer(_root);
        var page = new PageObj();
        container.PutObj("Page.xml", page);
        container.Flush();

        string file = Path.Combine(_root, "Page.xml");
        var before = File.ReadAllBytes(file);
        var beforeTime = File.GetLastWriteTimeUtc(file);

        // 重新加载后不做修改 → 不应重写（摘要一致）
        Thread.Sleep(20);
        var obj = container.GetObjByFile("Page.xml");
        Assert.Equal("Page", obj.Name);
        container.Flush();
        Assert.Equal(before, File.ReadAllBytes(file));
        Assert.Equal(beforeTime, File.GetLastWriteTimeUtc(file));

        // 修改后 → 重写
        Thread.Sleep(20);
        var reloaded = container.GetObjByFile("Page.xml");
        reloaded.SetAttr("Custom", "1");
        container.Flush();
        Assert.True(File.GetLastWriteTimeUtc(file) >= beforeTime);
        Assert.Contains("Custom=\"1\"", File.ReadAllText(file, Encoding.UTF8));
    }

    [Fact]
    public void GetObj_by_abs_and_relative_loc()
    {
        var root = new VirtualContainer(_root);
        var docDir = root.ObtainContainer("Doc_0", p => new VirtualContainer(p));
        var pagesDir = docDir.ObtainContainer("Pages", p => new VirtualContainer(p));
        var pageDir = pagesDir.ObtainContainer("Page_0", p => new VirtualContainer(p));
        pageDir.PutObj("Content.xml", new PageObj());
        root.Flush();

        Assert.Equal("/Doc_0/Pages/Page_0", pageDir.GetAbsLoc().ToString());

        // 从根容器按绝对路径取（Content.xml 内的根元素是 Page）
        var fromRoot = root.GetObj(new ST_Loc("/Doc_0/Pages/Page_0/Content.xml"));
        Assert.NotNull(fromRoot);
        Assert.Equal("Page", fromRoot!.Name);

        // 从 pagesDir 按相对路径取
        var fromPages = pagesDir.GetObj(new ST_Loc("Page_0/Content.xml"));
        Assert.NotNull(fromPages);

        // 绝对路径前缀不符 → null
        Assert.Null(docDir.GetObj(new ST_Loc("/Other/Page_0/Content.xml")));
        Assert.Null(pagesDir.GetObj(new ST_Loc("NotExist/Content.xml")));
        Assert.Null(root.GetObj((ST_Loc?)null));
    }

    [Fact]
    public void PutFileWithPath_reuse_and_collision_prefix()
    {
        var container = new VirtualContainer(_root);
        string src = Path.Combine(_root, "..", "src_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        File.WriteAllText(src, "hello");

        var first = container.PutFileWithPath(src);
        Assert.NotNull(first);
        Assert.Equal("hello", File.ReadAllText(first!));

        // 同名同内容 → 复用
        var second = container.PutFileWithPath(src);
        Assert.Equal(first, second);

        // 同名不同内容 → 加时间前缀
        File.WriteAllText(src, "changed");
        var third = container.PutFileWithPath(src);
        Assert.NotNull(third);
        Assert.NotEqual(first, third);
        Assert.Contains("_", Path.GetFileName(third!));

        File.Delete(src);

        // 直接写字节 & 存在性判断
        container.PutFile("raw.bin", new byte[] { 1, 2, 3 });
        Assert.True(container.Exist("raw.bin"));
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(_root, "raw.bin")));
        container.DeleteFile("raw.bin");
        Assert.False(container.Exist("raw.bin"));
        Assert.False(container.Exist(null));
    }

    [Fact]
    public void ElemCup_upns_rewrites_namespace_for_all_elements()
    {
        // 非标准命名空间 + 无前缀的文档
        var dirty = DomParser.Parse("<Document xmlns=\"http://wrong.example.com\"><Pages><Page/></Pages></Document>");
        string target = Path.Combine(_root, "sub", "Document.xml");
        ElemCup.DumpUpNS(dirty, target);

        string xml = File.ReadAllText(target, Encoding.UTF8);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<ofd:Document xmlns:ofd=\"http://www.ofdspec.org/2016\">", xml);
        Assert.Contains("<ofd:Pages><ofd:Page/></ofd:Pages>", xml);
        Assert.DoesNotContain("wrong.example.com", xml);

        // Dump 字节与 DomSerializer 一致（紧凑模式）
        var clean = new OFD();
        Assert.Equal(DomSerializer.Dump(clean.GetProxy()), ElemCup.Dump(clean.GetProxy()));
    }
}
