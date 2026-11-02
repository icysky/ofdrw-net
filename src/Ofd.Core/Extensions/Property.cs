namespace Ofd.Core.Extensions;

/// <summary>扩展属性：“Name Type Value” 数值组，用于简单扩展（第 17 章 图 83 表 64）。</summary>
public class Property : OFDElement
{
    public Property(Dom.DomElement proxy) : base(proxy) { }

    public Property() : base("Property") { }

    public Property(string name, string value, string? type) : this()
    {
        SetExtensionName(name).SetValue(value).SetType(type);
    }

    public Property(string name, string value) : this()
    {
        SetExtensionName(name).SetValue(value);
    }

    /// <summary>【必选属性】设置 扩展属性名称（空白抛异常）。</summary>
    public Property SetExtensionName(string name)
    {
        if (name == null || name.Trim().Length == 0) throw new ArgumentException("扩展属性名称（Name）不能为空");
        return (Property)AddAttribute("Name", name);
    }

    /// <summary>【必选属性】获取 扩展属性名称，缺失抛异常。</summary>
    public string GetExtensionName()
    {
        var str = AttributeValue("Name");
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException("扩展属性名称（Name）不能为空");
        return str;
    }

    /// <summary>【可选属性】设置 扩展属性值类型（空白删除属性）。</summary>
    public Property SetType(string? type)
    {
        if (type == null || type.Trim().Length == 0)
        {
            RemoveAttr("Type");
            return this;
        }
        return (Property)AddAttribute("Type", type);
    }

    /// <summary>【可选属性】获取 扩展属性值类型，不存在 null。</summary>
    public string? GetType() => AttributeValue("Type");

    /// <summary>【必选】设置 属性值（内容为元素文本）。</summary>
    public Property SetValue(string? value) => (Property)SetText(value ?? "");

    /// <summary>
    /// 【必选】获取 属性值（元素文本）。
    /// 注：Java 上游误查子元素 "Value"（setter 写的是文本），C# 已修正为读文本。
    /// </summary>
    public string? GetValue() => GetText()?.Trim();
}
