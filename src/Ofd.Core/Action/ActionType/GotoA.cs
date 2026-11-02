namespace Ofd.Core.Action.ActionType;

/// <summary>附件动作（图 76）：打开当前文档内的一个附件。</summary>
public class GotoA : OFDElement, IOFDAction
{
    public GotoA(Dom.DomElement proxy) : base(proxy) { }

    public GotoA() : base("GotoA") { }

    public GotoA(string attachId, bool newWindow) : this()
    {
        SetAttachID(attachId).SetNewWindow(newWindow);
    }

    public GotoA(string attachId) : this()
    {
        SetAttachID(attachId);
    }

    /// <summary>【必选属性】设置 附件标识（xs:IDREF）。</summary>
    public GotoA SetAttachID(string attachId) => (GotoA)AddAttribute("AttachID", attachId);

    /// <summary>【必选属性】获取 附件标识，不存在 null。</summary>
    public string? GetAttachID() => AttributeValue("AttachID");

    /// <summary>【可选属性】设置 是否在新窗口打开。</summary>
    public GotoA SetNewWindow(bool newWindow) => (GotoA)AddAttribute("NewWindow", newWindow ? "true" : "false");

    /// <summary>【可选属性】获取 是否在新窗口打开，null 表示不存在。</summary>
    public bool? GetNewWindow()
    {
        var str = AttributeValue("NewWindow");
        return str == null || str.Trim().Length == 0 ? null : bool.Parse(str.Trim());
    }
}
