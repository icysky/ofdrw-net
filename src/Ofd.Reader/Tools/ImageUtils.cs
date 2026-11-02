using SkiaSharp;

namespace Ofd.Reader.Tools;

/// <summary>
/// 图片处理工具（Java ImageUtils）：编解码、蒙版抠图、白底清除。
/// <para>
/// 适配说明：Java 使用 AWT BufferedImage + pdfbox JBIG2 解码；C# 侧统一用 SkiaSharp。
/// SkiaSharp 不支持 JBIG2 解码，<see cref="ReadJb2"/> 会抛出 <see cref="NotSupportedException"/>。
/// </para>
/// </summary>
public static class ImageUtils
{
    /// <summary>把位图编码为指定格式的字节数组。</summary>
    public static byte[] ToBytes(SKBitmap bitmap, SKEncodedImageFormat type, int quality = 100)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(type, quality);
        return data.ToArray();
    }

    /// <summary>
    /// 读取 JBIG2 图片。
    /// <para>SkiaSharp 不内置 JBIG2 解码能力，此方法仅保留接口兼容。</para>
    /// </summary>
    /// <exception cref="NotSupportedException">当前运行时无 JBIG2 解码器</exception>
    public static SKBitmap ReadJb2(Stream input) =>
        throw new NotSupportedException("SkiaSharp 不支持 JBIG2 解码，请使用专用 JBIG2 解码库");

    /// <summary>
    /// 蒙版抠图：蒙版中接近白色的像素（RGB 均值 &gt; 244）在原图中被抠成透明。
    /// <para>尺寸不一致时原样返回。</para>
    /// </summary>
    public static SKBitmap RenderMask(SKBitmap image, SKBitmap mask)
    {
        if (image.Width != mask.Width || image.Height != mask.Height) return image;

        return RenderMask(image, mask, (r, g, b) => (r + g + b) / 3 > 244);
    }

    /// <summary>
    /// 按像素过滤器做蒙版抠图。
    /// <para>过滤器返回 true 的像素保留原图内容，返回 false 的位置为透明（与 Java 语义一致）。</para>
    /// </summary>
    private static SKBitmap RenderMask(SKBitmap image, SKBitmap mask, Func<int, int, int, bool> filter)
    {
        var output = new SKBitmap(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var m = mask.GetPixel(x, y);
                if (filter(m.Red, m.Green, m.Blue))
                {
                    output.SetPixel(x, y, image.GetPixel(x, y)); // 保留
                }
                else
                {
                    output.SetPixel(x, y, SKColors.Transparent); // 抠掉
                }
            }
        }

        return output;
    }

    /// <summary>灰度值（ITU-R BT.601 定点权重）。</summary>
    public static int Gray(int r, int g, int b) => (r * 19595 + g * 38469 + b * 7472) >> 16;

    /// <summary>创建图片（透明填充或白色填充）。</summary>
    public static SKBitmap CreateImage(int width, int height, bool isTransparent)
    {
        var image = new SKBitmap(width, height, SKColorType.Bgra8888,
            isTransparent ? SKAlphaType.Premul : SKAlphaType.Opaque);

        using (var canvas = new SKCanvas(image))
        {
            canvas.Clear(isTransparent ? SKColors.Transparent : SKColors.White);
        }

        return image;
    }

    /// <summary>清除白色背景：灰度值大于阈值的像素变为透明。</summary>
    public static SKBitmap ClearWhiteBackground(SKBitmap input, int gray) =>
        RenderMask(input, input, (r, g, b) => Gray(r, g, b) > gray);
}
