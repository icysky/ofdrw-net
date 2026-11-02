namespace Ofd.Font;

/// <summary>
/// 随包分发的内置字体（位于 <c>Assets/FangSong</c>，构建时复制到输出目录）。
/// <para>
/// 目的：让生成端**不依赖运行环境是否安装中文字体**（Linux 容器通常没有宋体/仿宋），
/// 保证跨平台排版结果一致。
/// </para>
/// <para>
/// 当前内置：<c>FangSong_GB2312.ttf</c>（仿宋）。该字体覆盖 GB2312 常用字，
/// 若遇到扩展区生僻字显示为空，请换用覆盖更全的字体（如 Noto Sans SC 完整包）。
/// </para>
/// <para>
/// 注意：字体版权归原厂商所有，随包分发前请确认已获得授权。
/// </para>
/// </summary>
public static class BuiltinFonts
{
    /// <summary>字体所在子目录（相对输出目录）。</summary>
    public const string RelativeDir = "Assets/FangSong";

    /// <summary>仿宋字体文件名。</summary>
    public const string FangSongFileName = "FangSong_GB2312.ttf";

    /// <summary>仿宋字体族名（写入 OFD 的 FontName）。</summary>
    public const string FangSongFamily = "仿宋";

    /// <summary>Noto Serif SC 所在子目录（相对输出目录）。</summary>
    public const string SerifRelativeDir = "Assets/NotoSerif";

    private static Font? _fangSong;
    private static readonly Dictionary<string, Font> SerifCache = new(StringComparer.Ordinal);
    private static readonly object Sync = new();

    /// <summary>Noto Serif SC（Regular / Bold）文件路径；缺失返回 null。</summary>
    public static string? NotoSerifPath(bool bold = false)
    {
        string file = bold ? "NotoSerifSC-Bold.ttf" : "NotoSerifSC-Regular.ttf";
        string path = Path.Combine(AppContext.BaseDirectory, SerifRelativeDir, file);
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// 加载 Noto Serif SC（缓存单例）；缺失返回 null。
    /// <para>拉丁字符为比例字宽，适合「数字 + 字母」混排（如税号、编号）。</para>
    /// </summary>
    public static Font? NotoSerif(bool bold = false)
    {
        string key = bold ? "bold" : "regular";
        lock (Sync)
        {
            if (SerifCache.TryGetValue(key, out var cached)) return cached;

            string? path = NotoSerifPath(bold);
            if (path == null) return null;

            var font = Font.FromFile(bold ? "Noto Serif SC Bold" : "Noto Serif SC", path);
            SerifCache[key] = font;
            return font;
        }
    }

    /// <summary>仿宋字体文件绝对路径；文件缺失返回 null。</summary>
    public static string? FangSongPath
    {
        get
        {
            string path = Path.Combine(AppContext.BaseDirectory, RelativeDir, FangSongFileName);
            return File.Exists(path) ? path : null;
        }
    }

    /// <summary>
    /// 加载仿宋字体（缓存单例）；内置文件缺失返回 null。
    /// </summary>
    public static Font? FangSong()
    {
        if (_fangSong != null) return _fangSong;

        lock (Sync)
        {
            if (_fangSong != null) return _fangSong;

            string? path = FangSongPath;
            if (path == null) return null;

            _fangSong = Font.FromFile(FangSongFamily, path);
            return _fangSong;
        }
    }
}
