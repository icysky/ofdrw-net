using SkiaSharp;

namespace Ofd.Layout.Element;

/// <summary>
/// 图片对象（Java Img）：继承 Div，图片对象不可拆分。
/// </summary>
public class Img : Div
{
    /// <summary>图片文件路径。</summary>
    private string? _src;

    private Img()
    {
        SetIntegrity(true); // 图片不可拆分
    }

    /// <summary>构造图片对象（不解析图片尺寸）。</summary>
    public Img(double width, double height, string src) : this()
    {
        if (src == null || !File.Exists(src))
            throw new ArgumentException("图片文件为空或不存在");

        _src = src;
        SetWidth(width);
        SetHeight(height);
    }

    /// <summary>
    /// 构造图片对象并解析尺寸（按 5px/mm 换算，不推荐）。
    /// </summary>
    [Obsolete("建议使用 Img(width, height, src) 手动指定尺寸")]
    public Img(string src) : this()
    {
        if (src == null || !File.Exists(src))
            throw new ArgumentException("图片文件为空或不存在");

        _src = src;
        ParseImg();
    }

    /// <summary>解析路径上的图片并按 5px/mm 设置宽高。</summary>
    private void ParseImg()
    {
        var img = ReadImage(_src!);
        if (img != null)
        {
            SetWidth((double)img.Width / 5);
            SetHeight((double)img.Height / 5);
            img.Dispose();
        }
    }

    /// <summary>
    /// 读取图片为 SKBitmap（失败返回 null）。
    /// <para>注：Java 侧的 CMYK→RGB 转换在 SkiaSharp 解码时由底层自动处理。</para>
    /// </summary>
    public static SKBitmap? ReadImage(string imageFile)
    {
        try
        {
            using var stream = File.OpenRead(imageFile);
            return SKBitmap.Decode(stream);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public string? GetSrc() => _src;

    public Img SetSrc(string src)
    {
        _src = src;
        return this;
    }

    /// <summary>图片对象不支持分割。</summary>
    public override Div[] Split(double sHeight) =>
        throw new InvalidOperationException("图片对象不支持分割操作");

    /// <summary>元素类型。</summary>
    public override string ElementType() => "Img";
}
