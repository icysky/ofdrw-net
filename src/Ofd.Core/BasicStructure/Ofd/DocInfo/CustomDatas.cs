namespace Ofd.Core.BasicStructure.Ofd.DocInfo;

/// <summary>用户自定义元数据集合。其子节点为 CustomData。</summary>
public class CustomDatas : OFDElement
{
    public CustomDatas(Dom.DomElement proxy) : base(proxy) { }

    public CustomDatas() : base("CustomDatas") { }

    /// <summary>增加用户自定义元数据。</summary>
    public CustomDatas AddCustomData(string name, string? value) => AddCustomData(new CustomData(name, value));

    /// <summary>增加用户自定义元数据。</summary>
    public CustomDatas AddCustomData(CustomData customData)
    {
        Add(customData);
        return this;
    }

    /// <summary>获取自定义元数据集合。</summary>
    public List<CustomData> GetCustomDatas() => GetOFDElements("CustomData", e => new CustomData(e));

    /// <summary>获取用户自定义元数据值，不存在返回 null。</summary>
    public string? GetCustomDataValue(string name)
    {
        foreach (var item in GetCustomDatas())
            if (name.Equals(item.GetDataName(), StringComparison.Ordinal))
                return item.GetValue();
        return null;
    }
}
