namespace Ofd.Core.BasicStructure.Ofd.DocInfo;

/// <summary>用户自定义元数据，可以指定一个名称及其对应的值。</summary>
public class CustomData : OFDElement
{
    public CustomData(Dom.DomElement proxy) : base(proxy) { }

    /// <summary>创建自定义元数据。</summary>
    public CustomData(string name, string? value) : base("CustomData")
    {
        AddAttribute("Name", name);
        Proxy.Kids.Add(new Dom.DomText(value ?? ""));
    }

    /// <summary>获取元数据名称。</summary>
    public string? GetDataName() => AttributeValue("Name");

    /// <summary>设置元数据名称。</summary>
    public CustomData SetDataName(string? name) => (CustomData)AddAttribute("Name", name);

    /// <summary>获取元数据值。</summary>
    public string? GetValue() => GetText();

    /// <summary>设置元数据的值。</summary>
    public CustomData SetValue(string? value) => (CustomData)SetText(value ?? "");
}
