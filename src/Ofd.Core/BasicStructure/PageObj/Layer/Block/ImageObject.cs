using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;
using Ofd.Core.Image;

namespace Ofd.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>图像对象（7.7 表 16，内容见第 10 章；带播放视频动作时见第 12 章）。</summary>
public class ImageObject : CT_Image, IPageBlockType
{
    public ImageObject(Dom.DomElement proxy) : base(proxy) { }

    private ImageObject() : base("ImageObject") { }

    public ImageObject(ST_ID id) : this()
    {
        SetObjID(id);
    }

    public ImageObject(long id) : this(new ST_ID(id))
    {
    }

    /// <summary>【必选属性】设置 对象 ID（null 抛异常）。</summary>
    public ImageObject SetId(ST_ID id)
    {
        if (id == null) throw new ArgumentException("ID 不能为空");
        SetObjID(id);
        return this;
    }

    /// <summary>【必选属性】获取 对象 ID，不存在 null。</summary>
    public ST_ID? GetID() => GetObjID();
}
