using Ofd.Core.Dom;

namespace Ofd.Core.PageDescription.Clips;

/// <summary>可被裁剪的对象标记接口（Java ClipAble）：裁剪对象可以是 CT_Text、CT_Path。</summary>
public interface IClipAble
{
}

/// <summary>裁剪对象工厂（Java ClipAble.getInstance 的静态方法外置）。</summary>
public static class ClipAbleFactory
{
    /// <summary>解析元素为裁剪对象；仅支持 ofd:Text / ofd:Path。</summary>
    public static IClipAble? GetInstance(DomElement element)
    {
        string qName = element.Prefix.Length > 0 ? element.Prefix + ":" + element.Name : element.Name;
        return qName switch
        {
            "ofd:Text" or "Text" => new Ofd.Core.Text.Text.CT_Text(element),
            "ofd:Path" or "Path" => new Ofd.Core.Graph.PathObj.CT_Path(element),
            _ => throw new ArgumentException("不是 ClipAble 子类，未知元素类型：" + qName),
        };
    }
}
