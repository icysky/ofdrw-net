namespace Ofd.Sign;

/// <summary>签名 ID 解析器（Java SignIdParser）。</summary>
public static class SignIdParser
{
    /// <summary>解析签名 ID 中的数字：去除所有非数字字符，无数字返回 0。</summary>
    public static int ParseIndex(string? id)
    {
        if (string.IsNullOrEmpty(id)) return 0;

        string numStr = new(id.Where(char.IsDigit).ToArray());
        if (numStr.Length == 0) return 0;

        return int.TryParse(numStr, out int v) ? v : 0;
    }
}
