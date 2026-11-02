namespace Ofd.Core.Crypto.Encryt;

/// <summary>扩展参数（GMT0099 附录 C 图 C.1）。</summary>
public class Parameter : OFDElement
{
    public Parameter(Dom.DomElement proxy) : base(proxy) { }

    public Parameter() : base("Parameter") { }

    public Parameter(string name) : this()
    {
        SetAttrName(name);
    }

    public Parameter(string name, string? value) : this()
    {
        SetAttrName(name);
        SetValue(value);
    }

    /// <summary>【必选属性 OFD 2.0】设置 扩展参数名称（null 抛异常）。</summary>
    public Parameter SetAttrName(string name)
    {
        if (name == null) throw new ArgumentException("扩展参数名称(name)为空");
        return (Parameter)AddAttribute("Name", name);
    }

    /// <summary>【必选属性 OFD 2.0】设置 扩展参数名称（别名）。</summary>
    public Parameter SetKeyName(string name) => SetAttrName(name);

    /// <summary>【必选属性 OFD 2.0】获取 扩展参数名称，缺失抛异常。</summary>
    public string GetAttrName()
    {
        var name = AttributeValue("Name");
        if (name == null) throw new InvalidOperationException("扩展参数名称(name)为空");
        return name;
    }

    /// <summary>【必选属性 OFD 2.0】获取 扩展参数名称（别名）。</summary>
    public string GetKeyName() => GetAttrName();

    /// <summary>【可选 OFD 2.0】设置 扩展参数值（null 按空串）。</summary>
    public Parameter SetValue(string? value) => (Parameter)SetText(value ?? "");

    /// <summary>【可选 OFD 2.0】获取 扩展参数值（可能为 null）。</summary>
    public string? GetValue() => GetText()?.Trim();
}
