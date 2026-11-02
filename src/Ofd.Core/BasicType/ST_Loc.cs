namespace Ofd.Core.BasicType;

/// <summary>
/// 包结构内文件的路径，“.”表示当前路径，“..”表示父路径。
/// 约定：“/”代表根节点；未显示指定代表当前路径；路径区分大小写。
/// 示例：/Pages/P1/Content.xml。——《GB/T 33190-2016》 表 2 基本数据类型
/// （Java org.ofdrw.core.basicType.ST_Loc）
/// </summary>
public class ST_Loc : STBase, IEquatable<ST_Loc>
{
    /// <summary>路径。</summary>
    public string Loc { get; private set; }

    public ST_Loc(string path)
    {
        Loc = path;
    }

    /// <summary>获取路径实例，空白或 null 返还 null。</summary>
    public static ST_Loc? GetInstance(string? loc)
    {
        if (loc == null || loc.Trim().Length == 0) return null;
        return new ST_Loc(loc.Trim());
    }

    public ST_Loc SetLoc(string loc)
    {
        Loc = loc;
        return this;
    }

    /// <summary>
    /// 路径分割。获取的路径中可能含有 "" 空字符串（Java String.split 正则语义：
    /// 尾部空串被移除、内部空串保留），如需获取路径各部分请使用 <see cref="Parts"/>。
    /// </summary>
    public string[] Split()
    {
        if (Loc == null) return Array.Empty<string>();
        // 复刻 Java split("/")：移除尾部的空字符串，中间空串保留
        var arr = Loc.Split('/');
        int end = arr.Length;
        while (end > 0 && arr[end - 1].Length == 0) end--;
        return arr[..end];
    }

    /// <summary>获取路径的各个部分（过滤空串）。</summary>
    public string[] Parts()
    {
        if (Loc == null) return Array.Empty<string>();
        return Loc.Split('/').Where(s => s.Trim().Length > 0).ToArray();
    }

    /// <summary>获取父母路径字符串（无 '/' 时返回 ""）。</summary>
    public string Parent()
    {
        int indexOf = Loc.LastIndexOf('/');
        return indexOf == -1 ? "" : Loc[..indexOf];
    }

    /// <summary>获取父母路径。</summary>
    public ST_Loc ParentLoc() => new(Parent());

    /// <summary>获取路径的文件名称（以 '/' 结尾时返回 ""）。</summary>
    public string GetFileName()
    {
        int indexOf = Loc.LastIndexOf('/');
        if (indexOf == -1) return Loc;
        if (indexOf == Loc.Length - 1) return "";
        return Loc[(indexOf + 1)..];
    }

    /// <summary>路径拼接（处理任一侧的 '/' 重复）。</summary>
    public ST_Loc Cat(string? p2)
    {
        if (p2 == null) return this;
        string part1 = Loc;
        if (part1.EndsWith('/')) part1 = part1[..^1];
        if (p2.StartsWith('/')) p2 = p2[1..];
        return new ST_Loc(part1 + "/" + p2);
    }

    public ST_Loc Cat(ST_Loc? loc) => loc == null ? this : Cat(loc.Loc);

    /// <summary>是否以指定字符结尾。</summary>
    public bool EndWith(string suffix) => Loc.EndsWith(suffix);

    /// <summary>是否以指定字符开头。</summary>
    public bool StartWith(string prefix) => Loc.StartsWith(prefix);

    /// <summary>是否为根路径（以"/"开头）。</summary>
    public bool IsRootPath() => Loc.StartsWith('/');

    /// <summary>是否为空或空串。</summary>
    public bool IsEmpty() => Loc == null || Loc.Trim().Length == 0;

    public override string ToString() => Loc;

    public bool Equals(ST_Loc? other) => other != null && Loc == other.Loc;

    // Java 版 equals 额外支持与任意对象按字符串比较；C# 侧收敛为
    // string / ST_Loc 两种（.NET 等值约定），其余返回 false
    public override bool Equals(object? obj)
    {
        if (obj is string s) return Loc == s;
        return obj is ST_Loc other && Equals(other);
    }

    public override int GetHashCode() => Loc?.GetHashCode() ?? 0;

    /// <summary>比较两个路径是否相等（null 与 null 不相等）。</summary>
    public static bool Equal(object? a, object? b)
    {
        if (a == null || b == null) return false;
        if (a is ST_Loc la && b is ST_Loc lb) return la.Loc == lb.Loc;
        return a.ToString().Equals(b.ToString());
    }
}
