using Ofd.Core.Dom;

namespace Ofd.Core.Action.ActionType.ActionGoto;

/// <summary>Goto 目的地标记接口（Java OFDGotoTarget）：CT_Dest 或 Bookmark。</summary>
public interface IOFDGotoTarget
{
}

/// <summary>Goto 目的地工厂（Java OFDGotoTarget.getInstance 的静态方法外置）。</summary>
public static class OFDGotoTargetFactory
{
    public static IOFDGotoTarget GetInstance(DomElement element)
    {
        string qName = element.Prefix.Length > 0 ? element.Prefix + ":" + element.Name : element.Name;
        return qName switch
        {
            "ofd:Dest" => new CT_Dest(element),
            "ofd:Bookmark" => new GotoBookmark(element),
            _ => throw new ArgumentException("未知Goto的目标类型：" + qName),
        };
    }
}
