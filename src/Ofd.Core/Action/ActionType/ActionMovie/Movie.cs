using Ofd.Core.BasicType;

namespace Ofd.Core.Action.ActionType.ActionMovie;

/// <summary>播放视频动作（图 79）。</summary>
public class Movie : OFDElement, IOFDAction
{
    public Movie(Dom.DomElement proxy) : base(proxy) { }

    public Movie() : base("Movie") { }

    public Movie(string resourceId) : this()
    {
        SetResourceID(ST_RefID.GetInstance(resourceId)!);
    }

    public Movie(ST_RefID resourceId) : this()
    {
        SetResourceID(resourceId);
    }

    /// <summary>【必选属性】设置 引用资源文件中定义的视频资源标识。</summary>
    public Movie SetResourceID(ST_RefID resourceId) => (Movie)AddAttribute("ResourceID", resourceId.ToString());

    /// <summary>【必选属性】获取 引用资源文件中定义的视频资源标识，不存在 null。</summary>
    public ST_RefID? GetResourceID() => ST_RefID.GetInstance(AttributeValue("ResourceID"));

    /// <summary>【可选属性】设置 放映参数（null 删除属性，缺省 Play）。</summary>
    public Movie SetOperator(PlayType? @operator)
    {
        if (@operator == null)
        {
            RemoveAttr("Operator");
            return this;
        }
        return (Movie)AddAttribute("Operator", @operator.ToString());
    }

    /// <summary>【可选属性】获取 放映参数，缺失/空抛异常（与 Java 一致）。</summary>
    public PlayType GetOperator() => PlayTypeExtensions.GetInstance(AttributeValue("Operator"));
}
