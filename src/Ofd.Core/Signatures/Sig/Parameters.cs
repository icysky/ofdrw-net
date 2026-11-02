namespace Ofd.Core.Signatures.Sig;

/// <summary>签名扩展属性集（OFD 2.0 GMT0099 B.2）。</summary>
public class Parameters : OFDElement
{
    public Parameters(Dom.DomElement proxy) : base(proxy) { }

    public Parameters() : base("Parameters") { }

    /// <summary>【必选 OFD 2.0】增加 签名扩展属性（同名覆盖；null 忽略）。</summary>
    public Parameters AddParameter(Parameter? parameter)
    {
        if (parameter == null) return this;
        string? name = parameter.GetNameAttr();
        var oldP = GetParameter(name!);
        if (oldP != null) Remove(oldP);
        Add(parameter);
        return this;
    }

    /// <summary>【必选 OFD 2.0】增加 签名扩展属性（名称+值）。</summary>
    public Parameters AddParameter(string name, string? value) => AddParameter(new Parameter(name, value));

    /// <summary>【必选 OFD 2.0】增加 签名扩展属性（名称+类型+值）。</summary>
    public Parameters AddParameter(string name, string? type, string? value) =>
        AddParameter(new Parameter(name, type ?? "", value));

    /// <summary>通过属性名删除签名扩展属性，返回被删除项或 null。</summary>
    public Parameter? RemoveParameter(string? name)
    {
        if (name == null) return null;
        var oldP = GetParameter(name);
        if (oldP != null) Remove(oldP);
        return oldP;
    }

    /// <summary>通过属性名获取签名扩展属性，不存在 null。</summary>
    public Parameter? GetParameter(string name)
    {
        foreach (var p in GetParameters())
            if (name.Equals(p.GetNameAttr(), StringComparison.Ordinal))
                return p;
        return null;
    }

    /// <summary>【必选 OFD 2.0】获取 所有签名扩展属性。</summary>
    public List<Parameter> GetParameters() => GetOFDElements("Parameter", e => new Parameter(e));
}
