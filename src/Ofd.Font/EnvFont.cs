using SkiaSharp;

namespace Ofd.Font;

/// <summary>
/// 环境字体（Java EnvFont）：从系统/目录加载字体并按名称缓存，提供默认字体与文本测量。
/// </summary>
public static class EnvFont
{
    private static volatile bool _isInitialized;

    /// <summary>字体缓存（键为小写字体名）。</summary>
    private static Dictionary<string, Font> _fMap = new();

    /// <summary>默认字体：宋体 → SimSun → 微软雅黑 → 其他 → 首个可用。</summary>
    private static Font? _defaultFont;

    private static readonly object InitLock = new();

    /// <summary>在当前环境中寻找指定名称的字体，不存在返回 null。</summary>
    public static Font? GetFont(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        Initialize();
        return _fMap.TryGetValue(name.ToLowerInvariant(), out var font) ? font : null;
    }

    /// <summary>寻找指定名称字体，不存在则用 family 兜底。</summary>
    public static Font? GetFont(string? name, string? family)
    {
        Font? res = null;
        if (name != null) res = GetFont(name);
        if (res != null) return res;
        if (family != null) res = GetFont(family);
        return res;
    }

    /// <summary>设置自定义字体映射。</summary>
    public static void SetMapping(string? name, Font font)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (InitLock)
        {
            Initialize();
            _fMap[name.ToLowerInvariant()] = font;
        }
    }

    /// <summary>
    /// 从目录加载字体（仅 .otf/.ttf），覆盖环境变量中的同名字体；加载失败忽略。
    /// </summary>
    public static void Load(string? dirPath)
    {
        if (dirPath == null || !Directory.Exists(dirPath)) return;

        Initialize();

        foreach (var path in Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(path).ToLowerInvariant();
            if (!fileName.EndsWith(".otf") && !fileName.EndsWith(".ttf")) continue;

            try
            {
                var font = new Font(Path.GetFileNameWithoutExtension(path), null, path);
                Register(font);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"加载字体文件失败：{path}，错误：{e.Message}");
            }
        }
    }

    /// <summary>
    /// 测量字符串在指定字号下所占空间；找不到字体时使用默认字体。
    /// </summary>
    public static TextBounds StrBounds(string? name, string? family, string str, double size)
    {
        var font = GetFont(name, family) ?? GetDefaultFont();
        var typeface = font?.GetFontObj() ?? SKTypeface.Default;

        using var skFont = new SKFont(typeface, (float)size);
        float width = skFont.MeasureText(str);

        var m = skFont.Metrics;
        double height = m.Descent - m.Ascent + m.Leading;

        return new TextBounds(width, height);
    }

    /// <summary>获取默认字体。</summary>
    public static Font? GetDefaultFont()
    {
        Initialize();
        return _defaultFont;
    }

    /// <summary>设置默认字体。</summary>
    public static void SetDefaultFont(Font font)
    {
        Initialize();
        _defaultFont = font;
    }

    /// <summary>通过字体文件设置默认字体。</summary>
    public static void SetDefaultFont(string? path)
    {
        if (path == null || !File.Exists(path)) return;

        Initialize();
        var typeface = SKTypeface.FromFile(path)
                       ?? throw new ArgumentException("字体文件格式错误: " + path);
        _defaultFont = new Font(typeface.FamilyName, typeface.FamilyName).SetTypeface(typeface);
    }

    /// <summary>初始化字体映射（仅首次执行）。</summary>
    private static void Initialize()
    {
        if (_isInitialized) return;

        lock (InitLock)
        {
            if (_isInitialized) return;

            _defaultFont = null;
            _fMap = new Dictionary<string, Font>();

            // 枚举系统字体族
            var families = SKFontManager.Default.FontFamilies ?? Array.Empty<string>();
            foreach (var family in families)
            {
                try
                {
                    var typeface = SKFontManager.Default.MatchFamily(family);
                    if (typeface == null) continue;

                    var font = new Font(typeface.FamilyName, family).SetTypeface(typeface);
                    Register(font);
                }
                catch
                {
                    // 忽略单个字体加载失败
                }
            }

            // 默认字体选择顺序与 Java 一致
            _defaultFont =
                GetFontNoInit("宋体")
                ?? GetFontNoInit("simsun")
                ?? GetFontNoInit("microsoftyahei")
                ?? GetFontNoInit("stheiti-light")
                ?? GetFontNoInit("times new roman")
                ?? GetFontNoInit("serif")
                ?? (_fMap.Count > 0 ? _fMap.Values.First() : null);

            _isInitialized = true;
        }
    }

    private static Font? GetFontNoInit(string name) =>
        _fMap.TryGetValue(name.ToLowerInvariant(), out var f) ? f : null;

    /// <summary>注册字体：按字体名与族名（族名不覆盖已有）写入缓存。</summary>
    private static void Register(Font font)
    {
        string? name = font.GetName();
        if (!string.IsNullOrEmpty(name))
            _fMap[name.ToLowerInvariant()] = font;

        string? family = font.GetFamilyName();
        if (!string.IsNullOrEmpty(family))
            _fMap.TryAdd(family.ToLowerInvariant(), font);
    }
}
