using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.Action.ActionType;

/// <summary>播放音频动作（图 78）。</summary>
public class Sound : OFDElement, IOFDAction
{
    public Sound(Dom.DomElement proxy) : base(proxy) { }

    public Sound() : base("Sound") { }

    public Sound(ST_RefID resourceID) : this()
    {
        SetResourceID(resourceID);
    }

    /// <summary>【必选属性】设置 引用资源文件中的音频资源标识符。</summary>
    public Sound SetResourceID(ST_RefID resourceId) => (Sound)AddAttribute("ResourceID", resourceId.ToString());

    /// <summary>【必选属性】获取 引用资源文件中的音频资源标识符，不存在 null。</summary>
    public ST_RefID? GetResourceID() => ST_RefID.GetInstance(AttributeValue("ResourceID"));

    /// <summary>
    /// 【可选属性】设置 播放音量（[0,100]，上游实现为 max(v,0) 后 %100，保持行为）。
    /// </summary>
    public Sound SetVolume(int volume)
    {
        volume = Math.Max(volume, 0);
        volume %= 100;
        return (Sound)AddAttribute("Volume", volume.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 播放音量，缺失抛异常（与 Java 一致）。</summary>
    public int GetVolume() => int.Parse(AttributeValue("Volume") ?? throw new InvalidOperationException("Volume 不存在"), CultureInfo.InvariantCulture);

    /// <summary>【可选属性】设置 是否重复播放（null 删除属性）。</summary>
    public Sound SetRepeat(bool? repeat)
    {
        if (repeat == null)
        {
            RemoveAttr("Repeat");
            return this;
        }
        return (Sound)AddAttribute("Repeat", repeat.Value ? "true" : "false");
    }

    /// <summary>【可选属性】获取 是否重复播放，缺省 false。</summary>
    public bool GetRepeat() => BoolAttr("Repeat");

    /// <summary>【可选属性】设置 是否同步播放（null 删除属性）。</summary>
    public Sound SetSynchronous(bool? synchronous)
    {
        if (synchronous == null)
        {
            RemoveAttr("Synchronous");
            return this;
        }
        return (Sound)AddAttribute("Synchronous", synchronous.Value ? "true" : "false");
    }

    /// <summary>【可选属性】获取 是否同步播放，缺省 false。</summary>
    public bool GetSynchronous() => BoolAttr("Synchronous");

    private bool BoolAttr(string name)
    {
        var str = AttributeValue(name);
        return str != null && str.Trim().Length != 0 && bool.Parse(str.Trim());
    }
}
