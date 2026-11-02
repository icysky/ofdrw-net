namespace Ofd.Core.BasicStructure.Doc.VPreferences;

/// <summary>自动缩放模式（默认 Default，7.5 表 9 视图首选项）。</summary>
public class ZoomMode : ZoomScale
{
    public ZoomMode(Dom.DomElement proxy) : base(proxy) { }

    private ZoomMode() : base("ZoomMode") { }

    private ZoomMode(string type) : this()
    {
        Proxy.Kids.Add(new Dom.DomText(type));
    }

    /// <summary>自动缩放模式类型。</summary>
    public enum Type
    {
        /// <summary>默认缩放</summary>
        Default,

        /// <summary>合适高度</summary>
        FitHeight,

        /// <summary>合适宽度</summary>
        FitWidth,

        /// <summary>合适区域</summary>
        FitRect,
    }

    /// <summary>获取自动缩放模式类型，未知类型抛异常（与 Java 一致）。</summary>
    public Type GetType()
    {
        string str = ElementText(Proxy);
        return str switch
        {
            "Default" => Type.Default,
            "FitHeight" => Type.FitHeight,
            "FitWidth" => Type.FitWidth,
            "FitRect" => Type.FitRect,
            _ => throw new ArgumentException("未知的自动缩放模式： " + str),
        };
    }

    /// <summary>工厂方式创建实例。</summary>
    public static ZoomMode GetInstance(Type type) => new(type.ToString());
}
