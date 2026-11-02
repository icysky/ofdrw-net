namespace Ofd.Core.PageDescription.Clips;

/// <summary>
/// 图元对象的裁剪区域序列（8.5 图 45 表 34），采用对象空间坐标系；
/// 多个 Clip 时最终裁剪区为交集。
/// </summary>
public class Clips : OFDElement
{
    public Clips(Dom.DomElement proxy) : base(proxy) { }

    public Clips() : base("Clips") { }

    /// <summary>使用一个裁剪对象初始化裁剪序列。</summary>
    public Clips(CT_Clip clip) : this()
    {
        AddClip(clip);
    }

    /// <summary>【必选】增加 图元对象的裁剪区域（null 忽略）。</summary>
    public Clips AddClip(CT_Clip? clip)
    {
        if (clip == null) return this;
        Add(clip);
        return this;
    }

    /// <summary>【必选】获取 裁剪区域序列。</summary>
    public List<CT_Clip> GetClips() => GetOFDElements("Clip", e => new CT_Clip(e));

    /// <summary>【必选】设置变换标志（1.2 版本后控制裁剪区域是否受外部 CTM 影响，默认 false）。</summary>
    public Clips SetTransFlag(bool? transFlag)
    {
        if (transFlag == null)
        {
            RemoveAttr("TransFlag");
            return this;
        }
        return (Clips)AddAttribute("TransFlag", transFlag.Value ? "true" : "false");
    }

    /// <summary>【必选】获取裁剪区域变换标志，缺省 false。</summary>
    public bool GetTransFlag()
    {
        var str = AttributeValue("TransFlag");
        return str != null && str.Trim().Length != 0 && bool.Parse(str.Trim());
    }
}
