namespace Ofd.Sign;

/// <summary>
/// 签名 ID 提供者（Java SignIDProvider）：标准推荐样式 'sNNN'（NNN 从 1 起）。
/// </summary>
public interface ISignIdProvider
{
    /// <summary>设置当前最大签名 ID（实现者需自行解析并设置计数器）。</summary>
    void SetCurrentMaxSignId(string? maxSignId);

    /// <summary>增长并获取签名 ID。</summary>
    string IncrementAndGet();

    /// <summary>获取当前签名 ID（不增长）。</summary>
    string Get();

    /// <summary>解析签名 ID 中的数字。</summary>
    int Parse(string? id);
}
