using System.Globalization;

namespace Ofd.Core.BasicType;

/// <summary>
/// 标识符引用，无符号整数，此标识符应为文档内已定义的标识符。
/// 示例：1000。——《GB/T 33190-2016》 表 2 基本数据类型
/// （Java org.ofdrw.core.basicType.ST_RefID）
/// </summary>
public class ST_RefID : STBase
{
    public ST_ID? Ref { get; private set; }

    public ST_RefID(ST_ID? refId)
    {
        Ref = refId;
    }

    public ST_RefID(long refId) : this(new ST_ID(refId))
    {
    }

    /// <summary>获取 ST_RefID 实例，如果参数非法则返还 null。</summary>
    public static ST_RefID? GetInstance(string? idStr)
    {
        if (idStr == null || idStr.Trim().Length == 0) return null;
        return new ST_RefID(ST_ID.GetInstance(idStr.Trim()));
    }

    public static ST_RefID GetInstance(long id) => new(new ST_ID(id));

    public ST_ID? GetRefId() => Ref;

    public ST_RefID SetRefId(ST_ID? refId)
    {
        Ref = refId;
        return this;
    }

    public override string ToString() => Ref?.ToString() ?? "";
}
