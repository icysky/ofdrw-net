namespace Ofd.Core.Action.ActionType;

/// <summary>URI 动作（图 77）。</summary>
public class URI : OFDElement, IOFDAction
{
    public URI(Dom.DomElement proxy) : base(proxy) { }

    public URI() : base("URI") { }

    public URI(string uri) : this()
    {
        SetURI(uri);
    }

    public URI(string uri, string? baseUri) : this()
    {
        SetURI(uri).SetBase(baseUri);
    }

    /// <summary>【必选属性】设置 目标 URI 的位置。</summary>
    public URI SetURI(string uri) => (URI)AddAttribute("URI", uri);

    /// <summary>【必选属性】获取 目标 URI 的位置，不存在 null。</summary>
    public string? GetURI() => AttributeValue("URI");

    /// <summary>【可选属性】设置 Base URI（用于相对地址；null 删除属性）。</summary>
    public URI SetBase(string? baseUri)
    {
        if (baseUri == null)
        {
            RemoveAttr("Base");
            return this;
        }
        return (URI)AddAttribute("Base", baseUri);
    }

    /// <summary>【可选属性】获取 Base URI，不存在 null。</summary>
    public string? GetBase() => AttributeValue("Base");
}
