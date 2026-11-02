using System.Globalization;

namespace Ofd.Core.BasicStructure.Doc.Permission;

/// <summary>
/// 有效期（7.5 图 9）：开始日期不能晚于结束日期，且至少出现一个；
/// 不设置开始/结束表示该端不限。
/// </summary>
public class ValidPeriod : OFDElement
{
    public ValidPeriod(Dom.DomElement proxy) : base(proxy) { }

    public ValidPeriod() : base("ValidPeriod") { }

    public ValidPeriod(DateTime startDate, DateTime endDate) : this()
    {
        SetStartDate(startDate).SetEndDate(endDate);
    }

    /// <summary>【可选属性】设置 有效期开始日期（null 删除属性）。</summary>
    public ValidPeriod SetStartDate(DateTime? startDate)
    {
        if (startDate == null)
        {
            RemoveAttr("StartDate");
            return this;
        }
        return (ValidPeriod)AddAttribute("StartDate", startDate.Value.ToString(Const.LOCAL_DATETIME_FORMAT, CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 有效期开始日期，null 表示不存在。</summary>
    public DateTime? GetStartDate() => ParseDateTime(AttributeValue("StartDate"));

    /// <summary>【可选属性】设置 有效期结束日期（null 删除属性）。</summary>
    public ValidPeriod SetEndDate(DateTime? endDate)
    {
        if (endDate == null)
        {
            RemoveAttr("EndDate");
            return this;
        }
        return (ValidPeriod)AddAttribute("EndDate", endDate.Value.ToString(Const.LOCAL_DATETIME_FORMAT, CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 有效期结束日期，null 表示不存在。</summary>
    public DateTime? GetEndDate() => ParseDateTime(AttributeValue("EndDate"));

    private static DateTime? ParseDateTime(string? s)
    {
        if (s == null || s.Trim().Length == 0) return null;
        return DateTime.ParseExact(s.Trim(), Const.LOCAL_DATETIME_FORMAT, CultureInfo.InvariantCulture);
    }
}
