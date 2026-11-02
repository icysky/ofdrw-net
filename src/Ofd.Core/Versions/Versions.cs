namespace Ofd.Core.Versions;

/// <summary>版本序列（一个 OFD 文档可能有多个版本）。</summary>
public class Versions : OFDElement
{
    public Versions(Dom.DomElement proxy) : base(proxy) { }

    public Versions() : base("Versions") { }

    public Versions(Version version) : this()
    {
        AddVersion(version);
    }

    /// <summary>增 加版本描述入口。</summary>
    public Versions AddVersion(Version version)
    {
        Add(version);
        return this;
    }

    /// <summary>获取 版本描述入口列表。</summary>
    public List<Version> GetVersions() => GetOFDElements("Version", e => new Version(e));
}
