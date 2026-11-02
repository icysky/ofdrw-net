using System.IO.Compression;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Core.Signatures;
using Ofd.Pkg.Container;
using Xunit;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;

namespace Ofd.Tests.Pkg;

/// <summary>OFD 包目录结构（Pkg 层）：OFDDir/DocDir/PagesDir/PageDir/SignsDir/TempsDir/AnnotsDir。</summary>
public class PackageDirTests : IDisposable
{
    private readonly string _root;

    public PackageDirTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_pkg_" + Guid.NewGuid().ToString("N")[..8]);
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

    /// <summary>构建一个最小 OFD 结构（OFD.xml + Doc_0 + Page_0）。</summary>
    private static OFDDir BuildMinimalOfd(string root)
    {
        var ofdDir = new OFDDir(root);
        var doc = ofdDir.NewDoc(); // Doc_0

        doc.SetDocument(new Document()
            .SetCommonData(new CT_CommonData().SetMaxUnitID(100))
            .SetPages(new Pages().AddPage(
                new PageNode().SetID(new ST_ID(1)).SetBaseLoc(new ST_Loc("Pages/Page_0/Content.xml")))));

        doc.ObtainPages().NewPageDir().SetContent(new PageObjType());
        ofdDir.SetOfd(new OFD().AddDocBody(new DocBody().SetDocRoot(new ST_Loc("Doc_0/Document.xml"))));
        return ofdDir;
    }

    [Fact]
    public void OFDDir_build_flush_and_reopen()
    {
        string root = Path.Combine(_root, "pkg");
        var ofdDir = BuildMinimalOfd(root);

        Assert.Equal(0, ofdDir.GetDocByIndex(0).GetIndex());
        ofdDir.Flush();

        // 磁盘结构
        Assert.True(File.Exists(Path.Combine(root, "OFD.xml")));
        Assert.True(File.Exists(Path.Combine(root, "Doc_0", "Document.xml")));
        Assert.True(File.Exists(Path.Combine(root, "Doc_0", "Pages", "Page_0", "Content.xml")));

        // 重新打开（读模式）
        var reopened = new OFDDir(root);
        var ofd = reopened.GetOfd();
        Assert.Single(ofd.GetDocBodies());
        Assert.Equal("Doc_0/Document.xml", ofd.GetDocBodies()[0].GetDocRoot()!.ToString());

        // 默认文档：OFD.xml 最后一个 DocBody 的 DocRoot
        var defaultDoc = reopened.ObtainDocDefault();
        Assert.Equal(0, defaultDoc.GetIndex());

        var document = defaultDoc.GetDocument();
        var pages = document.GetPages()!;
        Assert.Single(pages.GetPages());
        Assert.Equal(1, pages.GetPages()[0].GetID()!.Id);

        var content = defaultDoc.GetPages().GetByIndex(0).GetContent();
        Assert.Equal("Page", content.Proxy.Name);
    }

    [Fact]
    public void Doc_index_continues_after_reopen()
    {
        string root = Path.Combine(_root, "idx");
        var d = new OFDDir(root);
        Assert.Equal(0, d.NewDoc().GetIndex());
        Assert.Equal(1, d.NewDoc().GetIndex());
        d.Flush();

        var reopened = new OFDDir(root);
        Assert.Equal(2, reopened.NewDoc().GetIndex()); // 扫描 Doc_N 目录后续号
        Assert.Throws<FileNotFoundException>(() => reopened.GetDocByIndex(9));
        Assert.Throws<ArgumentException>(() => new DocDir(Path.Combine(root, "Bad_Name")));
    }

    [Fact]
    public void ObtainDocDefault_uses_last_doc_body()
    {
        string root = Path.Combine(_root, "default");
        var d = new OFDDir(root);
        d.NewDoc();
        d.NewDoc(); // Doc_0 / Doc_1
        d.SetOfd(new OFD()
            .AddDocBody(new DocBody().SetDocRoot(new ST_Loc("Doc_0/Document.xml")))
            .AddDocBody(new DocBody().SetDocRoot(new ST_Loc("Doc_1/Document.xml"))));
        d.Flush();

        var reopened = new OFDDir(root);
        Assert.Equal(1, reopened.ObtainDocDefault().GetIndex()); // 取最后一个 DocBody
    }

    [Fact]
    public void PageDir_annot_files_auto_numbering()
    {
        string root = Path.Combine(_root, "annot");
        var d = new OFDDir(root);
        var pageDir = d.NewDoc().ObtainPages().NewPageDir();

        var a0 = pageDir.AddAnnot(new Ofd.Core.Annotation.PageAnnot.PageAnnot());
        var a1 = pageDir.AddAnnot(new Ofd.Core.Annotation.PageAnnot.PageAnnot());
        Assert.Equal("/Doc_0/Pages/Page_0/Annot_0.xml", a0!.ToString());
        Assert.Equal("/Doc_0/Pages/Page_0/Annot_1.xml", a1!.ToString());
        d.Flush();

        // 重开后索引续号
        var reopened = new OFDDir(root);
        var pageDir2 = reopened.GetDocByIndex(0).GetPages().GetByIndex(0);
        Assert.Equal(1, pageDir2.GetMaxAnnotFileIndex());
        var a2 = pageDir2.AddAnnot(new Ofd.Core.Annotation.PageAnnot.PageAnnot());
        Assert.Equal("/Doc_0/Pages/Page_0/Annot_2.xml", a2!.ToString());

        // 容器内注释文件扫描
        reopened.Flush();
        var annots = reopened.GetDocByIndex(0).GetPages().GetByIndex(0).GetPageAnnots();
        Assert.Equal(3, annots.Count);
        Assert.Contains("Annot_0.xml", annots.Keys);
    }

    [Fact]
    public void SignsDir_and_SignDir()
    {
        string root = Path.Combine(_root, "sign");
        var d = new OFDDir(root);
        var signs = d.NewDoc().ObtainSigns();
        signs.SetSignatures(new Signatures().SetMaxSignId("s1"));

        var s0 = signs.NewSignDir();
        var s1 = signs.NewSignDir();
        Assert.Equal(0, s0.GetIndex());
        Assert.Equal("0", s0.GetIndexStr());
        Assert.Equal(1, s1.GetIndex());
        s0.SetSignature(new Ofd.Core.Signatures.Sig.Signature());
        d.Flush();

        // 上游语义：GetByIndex 要求 index > 0
        Assert.Throws<FormatException>(() => signs.GetByIndex(0));
        Assert.Equal(1, signs.GetByIndex(1).GetIndex());

        var reopened = new OFDDir(root);
        var signs2 = reopened.GetDocByIndex(0).GetSigns();
        Assert.Equal("s1", signs2.GetSignatures().GetMaxSignId());
        Assert.Equal("Signature", signs2.GetSignDir("Sign_0").GetSignature().Proxy.Name);
        Assert.Equal(2, signs2.NewSignDir().GetIndex()); // 扫描 Sign_N 后续号
    }

    [Fact]
    public void TempsDir_auto_numbering_and_read_back()
    {
        string root = Path.Combine(_root, "temps");
        var d = new OFDDir(root);
        var temps = d.NewDoc().ObtainTemps();

        Assert.Equal("/Doc_0/Temps/Temp_0.xml", temps.Add(new PageObjType())!.ToString());
        Assert.Equal("/Doc_0/Temps/Temp_1.xml", temps.Add(new PageObjType())!.ToString());
        Assert.Null(temps.Add((PageObjType?)null));
        d.Flush();

        var reopened = new OFDDir(root);
        var temps2 = reopened.GetDocByIndex(0).GetTemps();
        Assert.Equal(1, temps2.GetMaxTempIndex());
        Assert.Equal("Page", temps2.Get("Temp_0.xml").Proxy.Name);
        Assert.Equal("/Doc_0/Temps/Temp_2.xml", temps2.Add(new PageObjType())!.ToString());
    }

    [Fact]
    public void PutFileAbs_and_traversal_guard()
    {
        string root = Path.Combine(_root, "abs");
        string src = Path.Combine(_root, "a.bin");
        File.WriteAllBytes(src, new byte[] { 1, 2, 3 });

        var d = new OFDDir(root);
        var loc = d.PutFileAbs("/Doc_0/Res/a.bin", src);
        Assert.Equal("/Doc_0/Res/a.bin", loc.ToString());
        Assert.True(File.Exists(Path.Combine(d.GetSysAbsPath(), "Doc_0", "Res", "a.bin")));
        Assert.True(File.Exists(d.GetFileAbs("/Doc_0/Res/a.bin")));

        // 路径越界防护
        Assert.Throws<ArgumentException>(() => d.PutFileAbs("../evil.bin", src));
        Assert.Throws<ArgumentException>(() => d.GetFileAbs("../../evil.bin"));
        Assert.Throws<ArgumentException>(() => d.PutFileAbs(" ", src));
        Assert.Throws<ArgumentException>(() => d.PutFileAbs("/x.bin", null!));
    }

    [Fact]
    public void Jar_produces_valid_zip_matching_container_bytes()
    {
        string root = Path.Combine(_root, "jar");
        var d = BuildMinimalOfd(root);
        string ofdFile = Path.Combine(_root, "out.ofd");

        d.Jar(ofdFile);
        Assert.True(File.Exists(ofdFile));

        using (var zip = ZipFile.OpenRead(ofdFile))
        {
            var names = zip.Entries.Select(e => e.FullName).ToList();
            Assert.Contains("OFD.xml", names);
            Assert.Contains("Doc_0/", names);
            Assert.Contains("Doc_0/Document.xml", names);
            Assert.Contains("Doc_0/Pages/Page_0/Content.xml", names);
        }

        // 解压后与工作目录逐文件字节一致（容器内容一致性检测点）
        string extracted = Path.Combine(_root, "extracted");
        ZipFile.ExtractToDirectory(ofdFile, extracted);
        foreach (var f in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, f);
            Assert.Equal(File.ReadAllBytes(f), File.ReadAllBytes(Path.Combine(extracted, rel)));
        }

        // 流式打包
        using var ms = new MemoryStream();
        d.Jar(ms);
        ms.Position = 0;
        using var zip2 = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Contains("OFD.xml", zip2.Entries.Select(e => e.FullName));
    }

    [Fact]
    public void Walk_visits_all_files_with_pkg_abs_path()
    {
        string root = Path.Combine(_root, "walk");
        var d = BuildMinimalOfd(root);
        d.Flush();

        var visited = new List<string>();
        d.Walk(new DelegateIterator((pkgPath, _) =>
        {
            visited.Add(pkgPath);
            return true;
        }));

        Assert.Contains("/OFD.xml", visited);
        Assert.Contains("/Doc_0/Document.xml", visited);
        Assert.Contains("/Doc_0/Pages/Page_0/Content.xml", visited);
        Assert.Equal(3, visited.Count);

        // 返回 false 中断遍历
        int count = 0;
        d.Walk(new DelegateIterator((_, _) => ++count < 1));
        Assert.Equal(1, count);

        Assert.Throws<ArgumentException>(() => d.Walk(null!));
    }

    [Fact]
    public void DocDir_resource_add_with_path()
    {
        string root = Path.Combine(_root, "res");
        var d = new OFDDir(root);
        var doc = d.NewDoc();

        string res = Path.Combine(_root, "r1.png");
        File.WriteAllText(res, "content-v1");
        var p1 = doc.AddResourceWithPath(res);
        Assert.NotNull(p1);

        File.WriteAllText(res, "content-v2");
        var p2 = doc.AddResourceWithPath(res);
        Assert.NotNull(p2);
        Assert.NotEqual(p1, p2); // 同名不同内容 → 加时间前缀
        Assert.True(File.Exists(p1));
        Assert.True(File.Exists(p2));
        Assert.Equal("content-v1", File.ReadAllText(p1!));
        Assert.Equal("content-v2", File.ReadAllText(p2!));
    }

    private sealed class DelegateIterator : IOFDPackageFileIterator
    {
        private readonly Func<string, string, bool> _f;

        public DelegateIterator(Func<string, string, bool> f) => _f = f;

        public bool Visit(string pkgAbsPath, string path) => _f(pkgAbsPath, path);
    }
}
