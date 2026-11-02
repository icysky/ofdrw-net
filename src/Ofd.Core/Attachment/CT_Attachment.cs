using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.Attachment;

/// <summary>附件（20.2 图 92 表 73）。</summary>
public class CT_Attachment : OFDElement
{
    public CT_Attachment(Dom.DomElement proxy) : base(proxy) { }

    public CT_Attachment() : base("Attachment") { }

    /// <summary>【必选属性】设置 附件标识（空白抛异常）。</summary>
    public CT_Attachment SetId(string id)
    {
        if (id == null || id.Trim().Length == 0) throw new ArgumentException("附件标识（ID）为空");
        return (CT_Attachment)AddAttribute("ID", id);
    }

    /// <summary>【必选属性】获取 附件标识，缺失抛异常。</summary>
    public string GetId()
    {
        var str = AttributeValue("ID");
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException("附件标识（ID）为空");
        return str;
    }

    /// <summary>【必选属性】设置 附件名称（空白抛异常）。</summary>
    public CT_Attachment SetAttachmentName(string attachmentName)
    {
        if (attachmentName == null || attachmentName.Trim().Length == 0) throw new ArgumentException("附件名称（Name）为空");
        return (CT_Attachment)AddAttribute("Name", attachmentName);
    }

    /// <summary>【必选属性】获取 附件名称，缺失抛异常。</summary>
    public string GetAttachmentName()
    {
        var str = AttributeValue("Name");
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException("附件名称（Name）为空");
        return str;
    }

    /// <summary>【可选属性】设置 附件格式（空白删除属性）。</summary>
    public CT_Attachment SetFormat(string? format)
    {
        if (format == null || format.Trim().Length == 0)
        {
            RemoveAttr("Format");
            return this;
        }
        return (CT_Attachment)AddAttribute("Format", format);
    }

    /// <summary>【可选属性】获取 附件格式，不存在 null。</summary>
    public string? GetFormat() => AttributeValue("Format");

    /// <summary>【可选属性】设置 创建时间（LocalDateTime 语义，yyyy-MM-dd HH:mm:ss；null 删除属性）。</summary>
    public CT_Attachment SetCreationDate(DateTime? creationDate)
    {
        if (creationDate == null)
        {
            RemoveAttr("CreationDate");
            return this;
        }
        return (CT_Attachment)AddAttribute("CreationDate", creationDate.Value.ToString(Const.LOCAL_DATETIME_FORMAT, CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】设置 创建时间（仅日期，按当日 00:00:00，对应 Java 已废弃重载）。</summary>
    public CT_Attachment SetCreationDate(DateOnly creationDate) =>
        SetCreationDate(creationDate.ToDateTime(TimeOnly.MinValue));

    /// <summary>【可选属性】获取 创建时间日期部分，不存在 null（兼容多格式解析）。</summary>
    public DateOnly? GetCreationDate() => GetCreationDateTime()?.Date is { } d ? DateOnly.FromDateTime(d) : null;

    /// <summary>【可选属性】获取 创建时间，不存在 null（兼容 yyyy-MM-dd HH:mm:ss / yyyy/MM/dd HH:mm:ss / 宽松格式 / ISO）。</summary>
    public DateTime? GetCreationDateTime() => ParseDateTime(AttributeValue("CreationDate"));

    /// <summary>【可选属性】设置 修改时间（yyyy-MM-dd HH:mm:ss；null 删除属性）。</summary>
    public CT_Attachment SetModDate(DateTime? modDate)
    {
        if (modDate == null)
        {
            RemoveAttr("ModDate");
            return this;
        }
        return (CT_Attachment)AddAttribute("ModDate", modDate.Value.ToString(Const.LOCAL_DATETIME_FORMAT, CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】设置 修改时间（仅日期，按当日 00:00:00）。</summary>
    public CT_Attachment SetModDate(DateOnly modDate) =>
        SetModDate(modDate.ToDateTime(TimeOnly.MinValue));

    /// <summary>【可选属性】获取 修改时间日期部分，不存在 null。</summary>
    public DateOnly? GetModDate() => GetModDateTime()?.Date is { } d ? DateOnly.FromDateTime(d) : null;

    /// <summary>【可选属性】获取 修改时间，不存在 null（兼容多格式解析）。</summary>
    public DateTime? GetModDateTime() => ParseDateTime(AttributeValue("ModDate"));

    /// <summary>【可选属性】设置 附件大小（KB；null 删除属性）。</summary>
    public CT_Attachment SetSize(double? size)
    {
        if (size == null)
        {
            RemoveAttr("Size");
            return this;
        }
        return (CT_Attachment)AddAttribute("Size", STBase.Fmt(size.Value));
    }

    /// <summary>【可选属性】获取 附件大小（KB），不存在 null。</summary>
    public double? GetSize()
    {
        var str = AttributeValue("Size");
        if (str == null || str.Trim().Length == 0) return null;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【可选属性】设置 附件是否可见（null 删除属性，缺省 true）。</summary>
    public CT_Attachment SetVisible(bool? visible)
    {
        if (visible == null)
        {
            RemoveAttr("Visible");
            return this;
        }
        return (CT_Attachment)AddAttribute("Visible", visible.Value ? "true" : "false");
    }

    /// <summary>【可选属性】获取 附件是否可见，缺省 true。</summary>
    public bool GetVisible()
    {
        var str = AttributeValue("Visible");
        return str == null || str.Trim().Length == 0 || bool.Parse(str.Trim());
    }

    /// <summary>【可选属性】设置 附件用途（空白删除属性，缺省 none）。</summary>
    public CT_Attachment SetUsage(string? usage)
    {
        if (usage == null || usage.Trim().Length == 0)
        {
            RemoveAttr("Usage");
            return this;
        }
        return (CT_Attachment)AddAttribute("Usage", usage);
    }

    /// <summary>【可选属性】获取 附件用途，缺省 none。</summary>
    public string GetUsage()
    {
        var str = AttributeValue("Usage");
        return str == null || str.Trim().Length == 0 ? "none" : str;
    }

    /// <summary>【可选】设置 附件内容在包内的路径（null 删除元素）。</summary>
    public CT_Attachment SetFileLoc(ST_Loc? fileLoc)
    {
        if (fileLoc == null)
        {
            RemoveOFDElemByNames("FileLoc");
            return this;
        }
        return (CT_Attachment)SetOFDEntity("FileLoc", fileLoc);
    }

    /// <summary>
    /// 【可选】获取 附件内容在包内的路径，不存在 null。
    /// 注：Java 上游误用 ST_Loc.getInstance(Element)（ST_Loc 无此重载），C# 按语义取元素文本。
    /// </summary>
    public ST_Loc? GetFileLoc()
    {
        var e = GetOFDElement("FileLoc");
        return e == null ? null : ST_Loc.GetInstance(ElementText(e).Trim());
    }

    /// <summary>
    /// 日期时间多格式兼容解析（对应 Java parseLocalDateTime 的 4 种格式）。
    /// </summary>
    private static DateTime? ParseDateTime(string? raw)
    {
        if (raw == null || raw.Trim().Length == 0) return null;
        string s = raw.Trim();
        string[] exactFormats =
        {
            Const.LOCAL_DATETIME_FORMAT,      // yyyy-MM-dd HH:mm:ss
            "yyyy/MM/dd HH:mm:ss",            // 非标准兼容
            Const.DATETIME_FORMAT,            // yyyy-MM-ddT HH:mm:ss
        };
        foreach (var fmt in exactFormats)
            if (DateTime.TryParseExact(s, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;

        // 宽松格式（对应 Java 的第 3 种弹性格式）
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose))
            return loose;

        throw new ArgumentException("日期时间格式不正确：" + s);
    }
}
