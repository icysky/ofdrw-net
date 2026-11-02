namespace Ofd.Core.BasicStructure.Res;

/// <summary>多媒体格式（7.9 图 21 表 19）。</summary>
public enum MediaType
{
    /// <summary>位图图像</summary>
    Image,

    /// <summary>音频</summary>
    Audio,

    /// <summary>视频</summary>
    Video,
}

public static class MediaTypeExtensions
{
    /// <summary>获取多媒体格式，未知类型抛异常（与 Java 一致）。</summary>
    public static MediaType GetInstance(string? str)
    {
        str = (str ?? "").Trim();
        if (str.Equals("Image", StringComparison.OrdinalIgnoreCase)) return MediaType.Image;
        if (str.Equals("Audio", StringComparison.OrdinalIgnoreCase)) return MediaType.Audio;
        if (str.Equals("Video", StringComparison.OrdinalIgnoreCase)) return MediaType.Video;
        throw new ArgumentException("多媒体格式不支持：" + str);
    }
}
