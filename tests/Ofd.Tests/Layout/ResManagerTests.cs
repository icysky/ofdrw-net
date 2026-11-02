using System.Text;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using SkiaSharp;
using Xunit;
using FontType = Ofd.Font.Font;

namespace Ofd.Tests.Layout;

/// <summary>资源管理器：绘制参数去重、公共/文档资源清单、字体与图片登记。</summary>
public class ResManagerTests : IDisposable
{
    private readonly string _root;

    public ResManagerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_res_" + Guid.NewGuid().ToString("N")[..8]);
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

    private static (OFDDir OfdDir, DocDir DocDir) NewDoc(string root)
    {
        var ofdDir = new OFDDir(root);
        var docDir = ofdDir.NewDoc();
        docDir.SetDocument(new Document()
            .SetCommonData(new CT_CommonData().SetMaxUnitID(100)));
        return (ofdDir, docDir);
    }

    [Fact]
    public void Graph_helper_builds_rect_path()
    {
        var data = GraphHelper.Rect(1, 2, 3, 4);
        var ops = data.ToList();

        Assert.Equal(5, ops.Count);
        Assert.Equal("M", ops[0].GetOpt());
        Assert.Equal(new double[] { 1, 2 }, ops[0].GetValues());
        Assert.Equal("L", ops[1].GetOpt());
        Assert.Equal(new double[] { 4, 2 }, ops[1].GetValues());
        Assert.Equal(new double[] { 4, 6 }, ops[2].GetValues());
        Assert.Equal(new double[] { 1, 6 }, ops[3].GetValues());
        Assert.Equal("C", ops[4].GetOpt());
    }

    [Fact]
    public void Draw_param_is_deduplicated_and_allocates_ids()
    {
        var (ofdDir, docDir) = NewDoc(Path.Combine(_root, "res1"));
        var res = new ResManager(ofdDir, docDir, new AtomicCounter(100));

        var id1 = res.AddDrawParam(new CT_DrawParam().SetLineWidth(2));
        Assert.NotNull(id1);
        Assert.Equal(101, id1!.Id);

        // 相同内容 → 复用同一 ID
        var id2 = res.AddDrawParam(new CT_DrawParam().SetLineWidth(2));
        Assert.Equal(id1.Id, id2!.Id);
        Assert.Single(res.GetNewResIds());

        // 不同内容 → 新 ID
        var id3 = res.AddDrawParam(new CT_DrawParam().SetLineWidth(3));
        Assert.Equal(102, id3!.Id);
        Assert.Equal(2, res.GetNewResIds().Count);

        // 资源已进入 DocumentRes 的 DrawParams
        var drawParams = res.DocRes().GetResources().OfType<DrawParams>().ToList();
        Assert.Single(drawParams);
        Assert.Equal(2, drawParams[0].GetDrawParams().Count);
    }

    [Fact]
    public void Public_and_document_res_are_created_and_linked()
    {
        var (ofdDir, docDir) = NewDoc(Path.Combine(_root, "res2"));
        var res = new ResManager(ofdDir, docDir, new AtomicCounter(100));

        var pub = res.PubRes();
        var doc = res.DocRes();

        Assert.Equal("Res", pub.GetBaseLoc()!.Loc);
        Assert.Equal("Res", doc.GetBaseLoc()!.Loc);

        var commonData = docDir.GetDocument().GetCommonData()!;
        Assert.Single(commonData.GetPublicResList());
        Assert.Single(commonData.GetDocumentResList());
        Assert.Equal("PublicRes.xml", commonData.GetPublicResList()[0]!.Loc);
        Assert.Equal("DocumentRes.xml", commonData.GetDocumentResList()[0]!.Loc);

        // 再次获取为同一对象
        Assert.Same(pub, res.PubRes());
        Assert.Same(doc, res.DocRes());
    }

    [Fact]
    public void Image_resource_is_registered_with_format()
    {
        var (ofdDir, docDir) = NewDoc(Path.Combine(_root, "res3"));
        var res = new ResManager(ofdDir, docDir, new AtomicCounter(100));

        string png = Path.Combine(_root, "pic.jpg");
        using (var bmp = new SKBitmap(10, 10))
        using (var img = SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        var id = res.AddImage(png);
        Assert.NotNull(id);

        var medias = res.DocRes().GetResources().OfType<MultiMedias>().ToList();
        Assert.Single(medias);
        var media = medias[0].GetMultiMedias()[0];
        Assert.Equal(MediaType.Image, media.GetType());
        Assert.Equal("JPEG", media.GetFormat()); // JPG 后缀映射为 JPEG
        Assert.EndsWith(".jpg", media.GetMediaFile()!.Loc);
    }

    [Fact]
    public void Font_resource_is_embedded_and_found_by_name()
    {
        string? ttf = FindSystemFont("arial.ttf") ?? FindAnySystemFont();
        if (ttf == null) return; // 无系统字体文件时跳过

        var (ofdDir, docDir) = NewDoc(Path.Combine(_root, "res4"));
        var res = new ResManager(ofdDir, docDir, new AtomicCounter(100));

        var font = new FontType("Arial", "Arial", ttf);
        var id = res.AddFont(font);
        Assert.NotNull(id);

        var fonts = res.PubRes().GetResources().OfType<Fonts>().ToList();
        Assert.Single(fonts);
        var ctFont = fonts[0].GetFonts()[0];
        Assert.Equal("Arial", ctFont.GetFontName());
        Assert.NotNull(ctFont.GetFontFile());
        Assert.EndsWith(".ttf", ctFont.GetFontFile()!.Loc);

        // 按名称（不区分大小写）查回
        var found = res.GetFont("arial");
        Assert.NotNull(found);
        Assert.NotNull(found!.AbsPath);
        Assert.EndsWith(".ttf", found.AbsPath!);

        Assert.Null(res.GetFont("不存在的字体"));
        Assert.Null(res.GetFont(null));
    }

    [Fact]
    public void Null_resource_is_ignored()
    {
        var (ofdDir, docDir) = NewDoc(Path.Combine(_root, "res5"));
        var res = new ResManager(ofdDir, docDir, new AtomicCounter(100));

        Assert.Null(res.AddDrawParam(null));
        Assert.Empty(res.GetNewResIds());
    }

    private static string? FindSystemFont(string fileName)
    {
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string path = Path.Combine(dir, fileName);
        return File.Exists(path) ? path : null;
    }

    private static string? FindAnySystemFont()
    {
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (!Directory.Exists(dir)) return null;
        return Directory.EnumerateFiles(dir, "*.ttf").FirstOrDefault();
    }
}
