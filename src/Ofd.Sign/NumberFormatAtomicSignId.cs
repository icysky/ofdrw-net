using System.Globalization;

namespace Ofd.Sign;

/// <summary>
/// 数字格式自增签名 ID（Java NumberFormatAtomicSignID）：
/// 启用零前缀为 "001"，关闭为 "1"（默认）。
/// </summary>
public class NumberFormatAtomicSignId : ISignIdProvider
{
    private int _provider;
    private bool _enableZeroPrefix;

    public NumberFormatAtomicSignId() { }

    /// <summary>创建数字构造器，可指定是否启用 0 前缀（格式 '00N'）。</summary>
    public NumberFormatAtomicSignId(bool enableZeroPrefix) => _enableZeroPrefix = enableZeroPrefix;

    /// <summary>创建指定最大签名 ID 的提供器。</summary>
    public NumberFormatAtomicSignId(string maxSignId) => _provider = Parse(maxSignId);

    public void SetCurrentMaxSignId(string? maxSignId) => _provider = Parse(maxSignId);

    public string IncrementAndGet()
    {
        int newSignId = ++_provider;
        return _enableZeroPrefix ? newSignId.ToString("D3", CultureInfo.InvariantCulture) : newSignId.ToString(CultureInfo.InvariantCulture);
    }

    public string Get()
    {
        return _enableZeroPrefix ? _provider.ToString("D3", CultureInfo.InvariantCulture) : _provider.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>解析签名 ID 数字（自动过滤非数字字符，支持 "s001"/"001"/"1"）。</summary>
    public int Parse(string? id) => SignIdParser.ParseIndex(id);

    /// <summary>开关是否启用 0 前缀。</summary>
    public void SetEnableZeroPrefix(bool enableZeroPrefix) => _enableZeroPrefix = enableZeroPrefix;

    /// <summary>是否启用 0 前缀。</summary>
    public bool IsEnableZeroPrefix() => _enableZeroPrefix;
}
