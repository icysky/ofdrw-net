// 注：Java 包名为 vpreferences.zoom，C# 若使用同名子命名空间会与 Zoom 类冲突，
// 故三个 zoom 类并入 VPreferences 命名空间（物理文件仍置于 Zoom/ 目录便于对照）。
namespace Ofd.Core.BasicStructure.Doc.VPreferences;

/// <summary>缩放比例选择对象基类（Zoom 与 ZoomMode 的公共父类）。</summary>
public class ZoomScale : OFDElement
{
    public ZoomScale(Dom.DomElement proxy) : base(proxy) { }

    public ZoomScale(string name) : base(name) { }
}
