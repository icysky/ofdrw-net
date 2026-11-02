using System.Xml;

namespace Ofd.Core.Dom;

/// <summary>
/// 轻量 DOM 解析器（dom4j SAXReader 的最小对应）。
/// 语义对齐 dom4j 默认行为：保留空白文本、保留注释/CDATA、属性插入序、PI 丢弃。
/// DTD 处理与 Java 版 SAXReaderFactory 一致：<b>禁止 DOCTYPE 声明</b>
/// （Java 设置 disallow-doctype-decl=true，此处 DtdProcessing.Prohibit 等价），杜绝 XXE。
/// 解析 → <see cref="DomSerializer.Dump"/> 往返字节一致（golden 单测覆盖 7 场景）。
/// </summary>
public static class DomParser
{
    private static readonly XmlReaderSettings Settings = new()
    {
        // 与 Java SAXReaderFactory 的 disallow-doctype-decl=true 对齐：含 DOCTYPE 的文档解析失败
        DtdProcessing = DtdProcessing.Prohibit,
        IgnoreComments = false,
        IgnoreProcessingInstructions = false,
        IgnoreWhitespace = false,
        XmlResolver = null,
        ConformanceLevel = ConformanceLevel.Document,
        CloseInput = true,
        CheckCharacters = true,
    };

    /// <summary>解析 XML 字符串，返回根元素。文档级注释/PI 不保留（dom4j Document 层内容，ofdrw 不使用）。</summary>
    public static DomElement Parse(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml), Settings);
        return Read(reader);
    }

    public static DomElement Parse(Stream stream) =>
        Parse(XmlReader.Create(stream, Settings));

    public static DomElement ParseFile(string path) =>
        Parse(XmlReader.Create(path, Settings));

    private static DomElement Parse(XmlReader reader)
    {
        using (reader)
            return Read(reader);
    }

    private static DomElement Read(XmlReader reader)
    {
        DomElement? root = null;
        var stack = new Stack<DomElement>();

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                {
                    var el = new DomElement(reader.LocalName, reader.Prefix, reader.NamespaceURI);
                    if (reader.MoveToFirstAttribute())
                    {
                        do
                        {
                            // xmlns 声明由命名空间机制处理，不进属性表（Dump 时仅根元素声明）
                            if (reader.Prefix == "xmlns" || reader.Name == "xmlns") continue;
                            el.Attrs.Add((reader.Name, reader.Value));
                        } while (reader.MoveToNextAttribute());
                        reader.MoveToElement();
                    }
                    if (root == null)
                        root = el;
                    else if (stack.Count > 0)
                        stack.Peek().Add(el); // 走 Add 维护 Parent 双向链
                    if (!reader.IsEmptyElement)
                        stack.Push(el);
                    break;
                }
                case XmlNodeType.EndElement:
                    stack.Pop();
                    break;

                // dom4j 保留元素内空白为 Text 节点（含缩进），此处同样保留以保证往返一致
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    if (stack.Count > 0)
                        stack.Peek().Kids.Add(new DomText(reader.Value));
                    break;

                case XmlNodeType.CDATA:
                    if (stack.Count > 0)
                        stack.Peek().Kids.Add(new DomCData(reader.Value));
                    break;

                case XmlNodeType.Comment:
                    if (stack.Count > 0)
                        stack.Peek().Kids.Add(new DomComment(reader.Value));
                    break;

                // XmlDeclaration / DocumentType / ProcessingInstruction / None：跳过
            }
        }

        return root ?? throw new InvalidDataException("XML 中未找到根元素");
    }
}
