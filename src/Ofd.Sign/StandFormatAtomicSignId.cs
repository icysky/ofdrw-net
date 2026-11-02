using System.Globalization;

namespace Ofd.Sign;

/// <summary>标准格式自增签名 ID（Java StandFormatAtomicSignID）：格式 's001'。</summary>
public class StandFormatAtomicSignId : ISignIdProvider
{
    private int _provider;

    public StandFormatAtomicSignId() { }

    /// <summary>创建指定最大签名 ID 的提供器。</summary>
    public StandFormatAtomicSignId(string maxSignId) => _provider = Parse(maxSignId);

    public void SetCurrentMaxSignId(string? maxSignId) => _provider = Parse(maxSignId);

    public string IncrementAndGet() => "s" + (++_provider).ToString("D3", CultureInfo.InvariantCulture);

    public string Get() => "s" + _provider.ToString("D3", CultureInfo.InvariantCulture);

    /// <summary>解析电子签名 ID 数字。</summary>
    public int Parse(string? id) => SignIdParser.ParseIndex(id);
}
