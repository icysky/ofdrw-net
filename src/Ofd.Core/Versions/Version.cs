using Ofd.Core.BasicType;

namespace Ofd.Core.Versions;

/// <summary>
/// 版本描述入口（图 89 版本结构列表）。
/// </summary>
public class Version : OFDElement
{
    public Version(Dom.DomElement proxy) : base(proxy) { }

    public Version() : base("Version") { }

    public Version SetID(string id)
    {
        if (id == null || id.Trim().Length == 0)
            throw new ArgumentException("版本标识符（ID）不能为空");
        return (Version)AddAttribute("ID", id);
    }

    public string GetID()
    {
        var str = AttributeValue("ID");
        if (str == null || str.Trim().Length == 0)
            throw new ArgumentException("版本标识符（ID）不能为空");
        return str;
    }

    /// <summary>设置是否是默认版本。</summary>
    public Version SetCurrent(bool current) => (Version)AddAttribute("Current", current ? "true" : "false");

    /// <summary>设置指向包内的版本描述文件。</summary>
    public Version SetBaseLoc(ST_Loc baseLoc) => (Version)AddAttribute("BaseLoc", baseLoc.ToString());

    public ST_Loc? GetBaseLoc()
    {
        var value = AttributeValue("BaseLoc");
        return value == null ? null : new ST_Loc(value);
    }
}
