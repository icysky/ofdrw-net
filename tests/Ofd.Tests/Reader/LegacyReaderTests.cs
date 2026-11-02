#pragma warning disable CS0618 // 测试旧版（已过时）读取器

using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Reader;

/// <summary>旧版读取器：SealOFDReader（公共资源/页面 VO）与 DLOFDReader（文档概要）。</summary>
public class LegacyReaderTests : IDisposable
{
    private readonly string _root;

    public LegacyReaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_legacy_" + Guid.NewGuid().ToString("N")[..8]);
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

    private string BuildDoc(string name, params string[] texts)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        foreach (var t in texts) doc.Add(new Paragraph(t, 5));
        return path;
    }

    [Fact]
    public void Seal_reader_exposes_pages_and_public_res()
    {
        string path = BuildDoc("seal.ofd", "第一页", "第二页");

        using var reader = new SealOFDReader(path);

        using var plain = new OFDReader(path);
        Assert.Equal(plain.GetNumberOfPages(), reader.GetOFDPageVO().Count);
        Assert.True(reader.GetOFDPageVO().Count > 0);

        // 布局产出的文档会嵌入字体（宋体），公共资源应至少有一个字体
        var fonts = reader.GetPublicResFonts();
        Assert.NotEmpty(fonts);

        // 绘制参数可能为空（无描边参数时），调用不应抛异常
        _ = reader.GetPublicResDrawParam();
    }

    [Fact]
    public void Dl_reader_builds_document_vo()
    {
        string path = BuildDoc("dl.ofd", "内容");

        using var reader = new DLOFDReader(path);
        var vo = reader.GetOFDDocumentVo();

        Assert.NotNull(vo);
        Assert.Equal(210d, vo.GetPageWidth(), 3);
        Assert.Equal(297d, vo.GetPageHeight(), 3);
        Assert.Single(vo.GetOfdPageVoList());
        Assert.Empty(vo.GetStampAnnotVos());
        Assert.Empty(vo.GetAnnotations());
    }

    [Fact]
    public void Dl_reader_accepts_stream()
    {
        string path = BuildDoc("dl2.ofd", "流式");

        using var fs = File.OpenRead(path);
        using var reader = new DLOFDReader(fs);

        Assert.Single(reader.GetOFDDocumentVo().GetOfdPageVoList());
    }
}
