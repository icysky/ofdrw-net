using Ofd.Core.Attachment;

namespace Ofd.Layout.Edit;

/// <summary>
/// 附件对象构造器（Java Attachment）：包装 <see cref="CT_Attachment"/>，附带源文件路径与同名替换开关。
/// </summary>
public class Attachment
{
    /// <summary>附件源文件。</summary>
    private string? _file;

    /// <summary>附件对象。</summary>
    private readonly CT_Attachment _atmObj;

    /// <summary>是否禁用同名替换（默认 false，即同名替换）。</summary>
    private bool _disableReplace;

    /// <summary>创建附件（同名文件将被替换）。</summary>
    public Attachment(string name, string? file) : this(name, file, false) { }

    /// <summary>创建附件。</summary>
    public Attachment(string name, string? file, bool disableReplace)
    {
        if (file == null || !File.Exists(file))
            throw new ArgumentException("附件文件(file)为空或不存在");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("附件名称(name)不能为空");

        _file = file;
        _disableReplace = disableReplace;
        _atmObj = new CT_Attachment();
        SetName(name);
    }

    /// <summary>【必选属性】设置附件名称。</summary>
    public Attachment SetName(string attachmentName)
    {
        _atmObj.SetAttachmentName(attachmentName);
        return this;
    }

    /// <summary>【必选属性】获取附件名称。</summary>
    public string GetName() => _atmObj.GetAttachmentName();

    /// <summary>【可选属性】设置附件格式。</summary>
    public Attachment SetFormat(string? format)
    {
        _atmObj.SetFormat(format);
        return this;
    }

    public string? GetFormat() => _atmObj.GetFormat();

    /// <summary>【可选属性】设置创建时间（仅日期）。</summary>
    [Obsolete("请使用 SetCreationDateTime")]
    public Attachment SetCreationDate(DateOnly creationDate)
    {
        _atmObj.SetCreationDate(creationDate);
        return this;
    }

    /// <summary>【可选属性】设置创建时间。</summary>
    public Attachment SetCreationDateTime(DateTime creationDate)
    {
        _atmObj.SetCreationDate(creationDate);
        return this;
    }

    [Obsolete("请使用 GetCreationDateTime")]
    public DateOnly? GetCreationDate() => _atmObj.GetCreationDate();

    public DateTime? GetCreationDateTime() => _atmObj.GetCreationDateTime();

    /// <summary>【可选属性】设置修改时间（仅日期）。</summary>
    [Obsolete("请使用 SetModDateTime")]
    public Attachment SetModDate(DateOnly modDate)
    {
        _atmObj.SetModDate(modDate);
        return this;
    }

    /// <summary>【可选属性】设置修改时间。</summary>
    public Attachment SetModDateTime(DateTime modDate)
    {
        _atmObj.SetModDate(modDate);
        return this;
    }

    [Obsolete("请使用 GetModDateTime")]
    public DateOnly? GetModDate() => _atmObj.GetModDate();

    /// <summary>
    /// 获取修改时间。
    /// <para>注：Java 原实现返回的是创建时间（复制粘贴缺陷），此处返回真实修改时间。</para>
    /// </summary>
    public DateTime? GetModDateTime() => _atmObj.GetModDateTime();

    /// <summary>【可选属性】设置附件是否可见（默认 true）。</summary>
    public Attachment SetVisible(bool? visible)
    {
        _atmObj.SetVisible(visible);
        return this;
    }

    public bool? GetVisible() => _atmObj.GetVisible();

    /// <summary>【可选属性】设置附件用途（默认 none）。</summary>
    public Attachment SetUsage(string? usage)
    {
        _atmObj.SetUsage(usage);
        return this;
    }

    public string GetUsage() => _atmObj.GetUsage();

    /// <summary>获取附件文件路径。</summary>
    public string? GetFile() => _file;

    /// <summary>设置附件文件。</summary>
    public Attachment SetFile(string? file)
    {
        _file = file;
        return this;
    }

    /// <summary>获取底层附件对象。</summary>
    public CT_Attachment GetAttachment() => _atmObj;

    /// <summary>设置是否禁用同名替换（默认 false）。</summary>
    public Attachment SetDisableReplace(bool disableReplace)
    {
        _disableReplace = disableReplace;
        return this;
    }

    public bool IsDisableReplace() => _disableReplace;
}
