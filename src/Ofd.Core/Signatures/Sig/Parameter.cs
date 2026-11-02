namespace Ofd.Core.Signatures.Sig;

/// <summary>签名扩展属性（OFD 2.0 GMT0099 B.2）：Name 属性 + 元素文本为值。</summary>
public class Parameter : OFDElement
{
    public Parameter(Dom.DomElement proxy) : base(proxy) { }

    public Parameter() : base("Parameter") { }

    public Parameter(string name, string? type, string? value) : this()
    {
        SetNameAttr(name);
        SetType(type);
        SetValue(value);
    }

    public Parameter(string name, string? value) : this()
    {
        SetNameAttr(name);
        SetValue(value);
    }

    /// <summary>【必选属性】设置 扩展属性名称（空白抛异常）。</summary>
    public Parameter SetNameAttr(string name)
    {
        if (name == null || name.Trim().Length == 0) throw new ArgumentException("扩展属性的名称（Name）不能为空");
        return (Parameter)AddAttribute("Name", name);
    }

    /// <summary>【必选属性】获取 扩展属性名称，不存在 null。</summary>
    public string? GetNameAttr() => AttributeValue("Name");

    /// <summary>
    /// 【可选属性】设置 扩展属性值类型（null 删除属性）。
    /// 注：Java 上游误写 removeAttr(type)（传变量），C# 修正为删除 "Type" 属性。
    /// </summary>
    public Parameter SetType(string? type)
    {
        if (type == null)
        {
            RemoveAttr("Type");
            return this;
        }
        return (Parameter)AddAttribute("Type", type);
    }

    /// <summary>【可选属性】获取 扩展属性值类型，不存在 null。</summary>
    public string? GetType() => AttributeValue("Type");

    /// <summary>【可选】设置 签名扩展属性值（null 按空串）。</summary>
    public Parameter SetValue(string? value) => (Parameter)SetText(value ?? "");

    /// <summary>【可选】获取 签名扩展属性值（空串表示无）。</summary>
    public string GetValue() => GetText()?.Trim() ?? "";
}
