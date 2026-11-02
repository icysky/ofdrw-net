namespace Ofd.Core.Crypto.Encryt;

/// <summary>扩展参数节点（GMT0099 附录 C 图 C.1）。</summary>
public class ExtendParams : OFDElement
{
    public ExtendParams(Dom.DomElement proxy) : base(proxy) { }

    public ExtendParams() : base("ExtendParams") { }

    /// <summary>【可选 OFD 2.0】增加 扩展参数（null 忽略）。</summary>
    public ExtendParams AddParameter(Parameter? parameter)
    {
        if (parameter == null) return this;
        Add(parameter);
        return this;
    }

    /// <summary>【可选 OFD 2.0】增加 扩展参数（名称+值）。</summary>
    public ExtendParams AddParameter(string name, string? value) => AddParameter(new Parameter(name, value));

    /// <summary>【可选 OFD 2.0】获取 扩展参数列表。</summary>
    public List<Parameter> GetParameters() => GetOFDElements("Parameter", e => new Parameter(e));

    /// <summary>根据参数名获取参数值，不存在 null。</summary>
    public string? GetValue(string? name)
    {
        if (name == null) return null;
        foreach (var parameter in GetParameters())
            if (name.Equals(parameter.GetAttrName(), StringComparison.Ordinal))
                return parameter.GetValue();
        return null;
    }
}
