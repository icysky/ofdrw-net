namespace Ofd.Core.Attachment;

/// <summary>附件列表（20.1 图 91 表 72）：一个 OFD 文件可定义多个附件。</summary>
public class Attachments : OFDElement
{
    public Attachments(Dom.DomElement proxy) : base(proxy) { }

    public Attachments() : base("Attachments") { }

    /// <summary>【可选】增加 附件（null 忽略）。</summary>
    public Attachments AddAttachment(CT_Attachment? attachment)
    {
        if (attachment == null) return this;
        Add(attachment);
        return this;
    }

    /// <summary>【可选】获取 附件列表。</summary>
    public List<CT_Attachment> GetAttachments() => GetOFDElements("Attachment", e => new CT_Attachment(e));
}
