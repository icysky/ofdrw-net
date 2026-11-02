using System.Globalization;

namespace Ofd.Core.BasicType;

/// <summary>
/// 标识，无符号整数，应在文档内唯一。0标识无效标识符。
/// 示例：1000。——《GB/T 33190-2016》 表 2 基本数据类型
/// （Java org.ofdrw.core.basicType.ST_ID）
/// </summary>
public class ST_ID : STBase, IEquatable<ST_ID>
{
    /// <summary>标识符，默认为无符号标识符。</summary>
    public long Id { get; private set; } = 0;

    public ST_ID(long id)
    {
        if (id <= 0) throw new ArgumentException("ID 必须大于 0");
        Id = id;
    }

    public ST_ID(string idStr)
    {
        Id = long.Parse(idStr, CultureInfo.InvariantCulture);
    }

    /// <summary>获取 ST_ID 实例，如果参数非法则返还 null。</summary>
    public static ST_ID? GetInstance(string? idStr)
    {
        if (idStr == null || idStr.Trim().Length == 0) return null;
        return new ST_ID(idStr.Trim());
    }

    public ST_ID SetId(long id)
    {
        Id = id;
        return this;
    }

    /// <summary>获取引用 ID。</summary>
    public ST_RefID Ref() => new(this);

    public override string ToString() => Id.ToString(CultureInfo.InvariantCulture);

    public bool Equals(ST_ID? other) => other != null && Id == other.Id;
    public override bool Equals(object? obj) => obj is ST_ID other && Equals(other);
    public override int GetHashCode() => Id.GetHashCode();
}
