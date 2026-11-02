using System.Text;
using Ofd.Core;
using Ofd.Core.Dom;

namespace Ofd.Pkg;

/// <summary>
/// 元素杯子（Java org.ofdrw.pkg.tool.ElemCup）：对象序列化/反序列化工具。
/// <para>反序列化 = 向杯子注入水（Inject）；序列化 = 把水倒出（Dump）。</para>
/// 默认紧凑模式（与 dom4j XMLWriter 默认一致，字节可对照）；
/// <see cref="EnableDebugPrint"/> 开启后输出缩进格式（仅调试用，不保证字节一致）。
/// </summary>
public static class ElemCup
{
    /// <summary>是否允许调试模式输出 XML（含空格和换行，仅调试用）。</summary>
    public static bool EnableDebugPrint = false;

    /// <summary>从文件加载反序列化元素对象。</summary>
    public static DomElement Inject(string file) => DomParser.ParseFile(file);

    /// <summary>从流加载反序列化元素对象。</summary>
    public static DomElement Inject(Stream stream) => DomParser.Parse(stream);

    /// <summary>序列化元素为字节序列（UTF-8，紧凑模式；用于摘要计算等）。</summary>
    public static byte[] Dump(DomElement element)
    {
        if (!EnableDebugPrint)
            return DomSerializer.Dump(element.Clone());
        return DumpPretty(element);
    }

    /// <summary>序列化并写入文件（自动创建父目录；文件不存在则创建）。</summary>
    public static void Dump(DomElement? element, string to)
    {
        if (element == null) return;
        if (to == null) throw new ArgumentException("文档元素序列化目标目录（to）为空");

        EnsureParentDirectory(to);
        byte[] bin = Dump(element);
        File.WriteAllBytes(to, bin);
    }

    /// <summary>
    /// 序列化元素并升级命名空间为 OFD 标准命名空间，写入文件
    /// （对应 Java ElemCup.dumpUpNS + OFDNameSpaceModifier）。
    /// </summary>
    public static void DumpUpNS(DomElement? element, string to)
    {
        if (element == null) return;
        if (to == null) throw new ArgumentException("文档元素序列化目标目录（to）为空");

        EnsureParentDirectory(to);
        DomElement cloned = element.Clone();
        UpNs(cloned);
        byte[] bin = EnableDebugPrint ? DumpPretty(cloned) : DomSerializer.Dump(cloned);
        File.WriteAllBytes(to, bin);
    }

    /// <summary>
    /// 将元素树整体改写为 OFD 标准命名空间（root 声明 ofd 前缀）。
    /// 对应 Java OFDNameSpaceModifier：逐个元素比较 (prefix, uri)，不同则更新。
    /// </summary>
    public static void UpNs(DomElement root)
    {
        if (root == null) return;
        var stack = new Stack<DomElement>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var e = stack.Pop();
            if (e.Prefix != Const.OFD_PREFIX || e.Uri != Const.OFD_NAMESPACE)
            {
                e.Prefix = Const.OFD_PREFIX;
                e.Uri = Const.OFD_NAMESPACE;
            }
            foreach (var k in e.Kids)
                if (k is DomElement c)
                    stack.Push(c);
        }
    }

    private static void EnsureParentDirectory(string to)
    {
        if (File.Exists(to)) return;
        string? dir = Path.GetDirectoryName(to);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static byte[] DumpPretty(DomElement element)
    {
        using var ms = new MemoryStream();
        var settings = new System.Xml.XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            NewLineChars = "\n",
            OmitXmlDeclaration = false,
        };
        using (var writer = System.Xml.XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            WriteElement(writer, element, isRoot: true);
            writer.WriteEndDocument();
        }
        return ms.ToArray();
    }

    private static void WriteElement(System.Xml.XmlWriter w, DomElement e, bool isRoot)
    {
        string prefix = e.Prefix.Length > 0 ? e.Prefix : "";
        w.WriteStartElement(e.Name, e.Uri.Length > 0 ? e.Uri : null);
        if (prefix.Length > 0 && isRoot)
            w.WriteAttributeString("xmlns", prefix, null, e.Uri);
        foreach (var (k, v) in e.Attrs)
            w.WriteAttributeString(k, v);
        foreach (var k in e.Kids)
        {
            switch (k)
            {
                case DomElement c: WriteElement(w, c, false); break;
                case DomText t: w.WriteString(t.Text); break;
                case DomCData cd: w.WriteCData(cd.Text); break;
                case DomComment cm: w.WriteComment(cm.Text); break;
            }
        }
        w.WriteEndElement();
    }
}
