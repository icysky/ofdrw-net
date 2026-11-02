namespace Ofd.Core.Action;

/// <summary>动作类型结构（图 73）：事件类型 + 动作实体，可选启动区域。</summary>
public class CT_Action : OFDElement
{
    public CT_Action(Dom.DomElement proxy) : base(proxy) { }

    public CT_Action() : base("Action") { }

    public CT_Action(EventType type, IOFDAction action) : this()
    {
        SetEvent(type).SetAction(action);
    }

    /// <summary>【必选属性】设置 事件类型（表 52）。</summary>
    public CT_Action SetEvent(EventType eventType) => (CT_Action)AddAttribute("Event", eventType.ToString());

    /// <summary>【必选属性】获取 事件类型，未知抛异常。</summary>
    public EventType GetEvent() => EventTypeExtensions.GetInstance(AttributeValue("Event"));

    /// <summary>
    /// 【可选】设置 多个复杂区域为该链接对象的启动区域
    /// （不出现时以所在图元或页面的外接矩形作为启动区域，见 9.3）。
    /// </summary>
    public CT_Action SetRegion(global::Ofd.Core.Graph.Tight.CT_Region region)
    {
        Set(region);
        return this;
    }

    /// <summary>【可选】获取 启动区域，不存在 null。</summary>
    public global::Ofd.Core.Graph.Tight.CT_Region? GetRegion()
    {
        var e = GetOFDElement("Region");
        return e == null ? null : new global::Ofd.Core.Graph.Tight.CT_Region(e);
    }

    /// <summary>【必选】设置 动作（先移除已有五类动作元素再追加）。</summary>
    public CT_Action SetAction(IOFDAction action)
    {
        RemoveOFDElemByNames("URI", "Sound", "GotoA", "Movie", "Goto");
        Add((OFDElement)action);
        return this;
    }

    /// <summary>【必选】获取 动作实体（按元素名工厂分派）。</summary>
    public IOFDAction GetAction()
    {
        var elements = Elements();
        if (elements.Count == 0)
            throw new InvalidOperationException("错误的Action结构，没有任何子节点");
        return OFDActionFactory.GetInstance(elements[0]);
    }
}
