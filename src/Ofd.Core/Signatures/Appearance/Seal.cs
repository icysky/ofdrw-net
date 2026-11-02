using Ofd.Core.BasicType;

namespace Ofd.Core.Signatures.Appearance;

/// <summary>电子印章信息（18.2.1 图 86 表 67）。</summary>
public class Seal : OFDElement
{
    public Seal(Dom.DomElement proxy) : base(proxy) { }

    public Seal() : base("Seal") { }

    public Seal(ST_Loc baseLoc) : this()
    {
        SetBaseLoc(baseLoc);
    }

    /// <summary>【必选】设置 指向包内的安全电子印章文件路径（null 抛异常）。</summary>
    public Seal SetBaseLoc(ST_Loc baseLoc)
    {
        if (baseLoc == null) throw new ArgumentException("指向包内的安全电子印章文件路径（BaseLoc）为空");
        return (Seal)SetOFDEntity("BaseLoc", baseLoc);
    }

    /// <summary>【必选】获取 安全电子印章文件路径，缺失抛异常。</summary>
    public ST_Loc GetBaseLoc()
    {
        var e = GetOFDElement("BaseLoc");
        if (e == null) throw new InvalidOperationException("指向包内的安全电子印章文件路径（BaseLoc）为空");
        return ST_Loc.GetInstance(ElementText(e).Trim())!;
    }

    /// <summary>【可选 OFD 2.0】设置 印模图片存储位置（null 删除元素）。</summary>
    public Seal SetImageLoc(ST_Loc? imageLoc)
    {
        if (imageLoc == null)
        {
            RemoveOFDElemByNames("ImageLoc");
            return this;
        }
        return (Seal)SetOFDEntity("ImageLoc", imageLoc);
    }

    /// <summary>【可选 OFD 2.0】获取 印模图片存储位置，不存在 null。</summary>
    public ST_Loc? GetImageLoc()
    {
        var e = GetOFDElement("ImageLoc");
        return e == null ? null : ST_Loc.GetInstance(ElementText(e).Trim());
    }
}
