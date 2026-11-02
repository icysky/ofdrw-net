using System.IO.Compression;
using System.Text;
using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.BasicType;
using Ofd.Layout;
using Ofd.Layout.Edit;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>注释：属性构建、默认值、注释结构落位与绘制内容。</summary>
public class AnnotationTests : IDisposable
{
    private readonly string _root;

    public AnnotationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_annot_" + Guid.NewGuid().ToString("N")[..8]);
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
            // ignore
        }
    }

    private static Dictionary<string, string> ReadEntries(string ofdPath)
    {
        var res = new Dictionary<string, string>(StringComparer.Ordinal);
        using var zip = ZipFile.OpenRead(ofdPath);
        foreach (var e in zip.Entries)
        {
            using var ms = new MemoryStream();
            using var s = e.Open();
            s.CopyTo(ms);
            res[e.FullName] = Encoding.UTF8.GetString(ms.ToArray());
        }
        return res;
    }

    /// <summary>Line 自身即为绘制器（CanvasBase 自绑定）。</summary>
    private static IDrawer LineDrawer(double w = 40, double h = 20) =>
        new Line(w, h).SetBeginPoint(1, 1).SetEndPoint(30, 15);

    private string BuildBaseDoc(string name)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        doc.Add(new Paragraph("base", 4));
        return path;
    }

    [Fact]
    public void Annotation_builds_ofd_object_with_defaults()
    {
        var annotation = new Annotation(10, 20, 50, 30, AnnotType.Watermark, LineDrawer())
            .SetRemark("备注")
            .AddParameter("k1", "v1")
            .AddParameter("k2", "v2");

        var annot = annotation.Build();

        Assert.Equal(AnnotType.Watermark, annot.GetType());
        Assert.Equal("OFD R&W", annot.GetCreator());
        Assert.Equal("备注", annot.GetRemark());
        Assert.Equal(2, annot.GetParameters().Count);
        Assert.Equal("v1", annot.GetParameters()["k1"]);

        // 外观边界
        var appearance = annot.GetAppearance()!;
        Assert.Equal(50d, appearance.GetBoundary()!.Width, 6);
        Assert.Equal(30d, appearance.GetBoundary()!.Height, 6);

        // 默认值
        Assert.True(annotation.GetVisible());
        Assert.True(annotation.GetPrint());
        Assert.False(annotation.GetNoZoom());
        Assert.False(annotation.GetNoRotate());
        Assert.True(annotation.GetReadOnly());
        Assert.Empty(new Annotation(0, 0, 1, 1, AnnotType.Path, LineDrawer()).GetParameters());
    }

    [Fact]
    public void Annotation_validates_arguments()
    {
        Assert.Throws<ArgumentException>(() => new Annotation(null!, AnnotType.Path, LineDrawer()));
        Assert.Throws<ArgumentException>(() => new Annotation(0, 0, 1, 1, AnnotType.Path, null!));
    }

    [Fact]
    public void Annotation_parameter_overwrites_same_key()
    {
        var a = new Annotation(0, 0, 10, 10, AnnotType.Path, LineDrawer())
            .AddParameter("k", "1")
            .AddParameter("k", "2")
            .AddParameter(null!, "ignored");

        var p = a.GetParameters();
        Assert.Single(p);
        Assert.Equal("2", p["k"]);
    }

    [Fact]
    public void Doc_add_annotation_creates_annots_structure()
    {
        string baseDoc = BuildBaseDoc("base.ofd");
        string outPath = Path.Combine(_root, "annotated.ofd");

        var annotation = new Annotation(new ST_Box(20, 30, 60, 40), AnnotType.Watermark, LineDrawer())
            .SetRemark("watermark-like");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            doc.AddAnnotation(1, annotation);
        }

        var entries = ReadEntries(outPath);

        // 注释列表文件
        Assert.Contains("Doc_0/Annots/Annotations.xml", entries.Keys);
        string annotationsXml = entries["Doc_0/Annots/Annotations.xml"];
        Assert.Contains("Page", annotationsXml);
        Assert.Contains("FileLoc", annotationsXml);

        // 分页注释文件
        Assert.Contains(entries.Keys, k => k.StartsWith("Doc_0/Annots/") && k.Contains("Annot_"));
        string pageAnnot = entries.Where(kv => kv.Key.Contains("Annot_")).Select(kv => kv.Value).First();
        Assert.Contains("Watermark", pageAnnot);
        Assert.Contains("Appearance", pageAnnot);
        Assert.Contains("Boundary=\"20 30 60 40\"", pageAnnot);

        // Document.xml 中登记注释入口
        string document = entries["Doc_0/Document.xml"];
        Assert.Contains("Annotations", document);
    }

    [Fact]
    public void Multiple_annotations_on_same_page_share_page_annot_file()
    {
        string baseDoc = BuildBaseDoc("base2.ofd");
        string outPath = Path.Combine(_root, "multi.ofd");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            doc.AddAnnotation(1, new Annotation(0, 0, 20, 20, AnnotType.Path, LineDrawer()));
            doc.AddAnnotation(1, new Annotation(10, 10, 20, 20, AnnotType.Highlight, LineDrawer()));
        }

        var entries = ReadEntries(outPath);
        var annotFiles = entries.Keys.Where(k => k.StartsWith("Doc_0/Annots/") && k.Contains("Annot_")).ToList();
        Assert.Single(annotFiles);

        string xml = entries[annotFiles[0]];
        int pathCount = xml.Split("<ofd:Annot ").Length - 1 + xml.Split("<Annot ").Length - 1;
        Assert.Equal(2, pathCount);
        Assert.Contains("Path", xml);
        Assert.Contains("Highlight", xml);
    }

    [Fact]
    public void Add_annotation_requires_modify_mode()
    {
        string outPath = Path.Combine(_root, "new.ofd");
        using var doc = new OFDDoc(outPath);
        doc.Add(new Paragraph("x", 4));

        Assert.Throws<InvalidOperationException>(() =>
            doc.AddAnnotation(1, new Annotation(0, 0, 10, 10, AnnotType.Path, LineDrawer())));
    }

    [Fact]
    public void Null_annotation_is_ignored()
    {
        string baseDoc = BuildBaseDoc("base3.ofd");
        string outPath = Path.Combine(_root, "null.ofd");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            Assert.Same(doc, doc.AddAnnotation(1, null));
        }
    }
}
