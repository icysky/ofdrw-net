using Ofd.Core.Action.ActionType;
using Ofd.Core.Action.ActionType.ActionMovie;
using Ofd.Core.Action.ActionType.ActionGoto;
using Ofd.Core.Dom;

namespace Ofd.Core.Action;

/// <summary>动作类型标记接口（Java OFDAction，表 51 动作类型属性）。</summary>
public interface IOFDAction
{
}

/// <summary>
/// 动作类型工厂（Java OFDAction.getInstance(Element) 的静态方法外置）。
/// </summary>
public static class OFDActionFactory
{
    public static IOFDAction GetInstance(DomElement element)
    {
        string qName = element.Prefix.Length > 0 ? element.Prefix + ":" + element.Name : element.Name;
        switch (qName)
        {
            case "ofd:Goto":
            case "Goto":
                return new Goto(element);
            case "ofd:URI":
            case "URI":
                return new URI(element);
            case "ofd:GotoA":
            case "GotoA":
                return new GotoA(element);
            case "ofd:Sound":
            case "Sound":
                return new Sound(element);
            case "ofd:Movie":
            case "Movie":
                return new Movie(element);
            default:
                throw new ArgumentException("未知的动作类型：" + qName);
        }
    }
}
