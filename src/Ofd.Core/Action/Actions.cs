namespace Ofd.Core.Action;

/// <summary>动作序列（图 19 大纲节点结构）：激活时依次执行。</summary>
public class Actions : OFDElement
{
    public Actions(Dom.DomElement proxy) : base(proxy) { }

    public Actions() : base("Actions") { }

    /// <summary>【必选】增加到动作列表。</summary>
    public Actions AddAction(CT_Action action)
    {
        Add(action);
        return this;
    }

    /// <summary>【必选】获取 动作列表。</summary>
    public List<CT_Action> GetActions() => GetOFDElements("Action", e => new CT_Action(e));
}
