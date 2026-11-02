using System.IO.Compression;
using System.Text;
using Ofd.Layout;
using Ofd.Layout.Edit;
using Ofd.Layout.Element;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>附件：属性构造、文档内落位与同名替换策略。</summary>
public class AttachmentTests : IDisposable
{
    private readonly string _root;

    public AttachmentTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_att_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch
        {
            // ignore
        }
    }

    private string CreateFile(string name, string content = "hello-attachment")
    {
        string p = Path.Combine(_root, name);
        File.WriteAllText(p, content, Encoding.UTF8);
        return p;
    }

    private static Dictionary<string, byte[]> ReadEntries(string ofdPath)
    {
        var res = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using var zip = ZipFile.OpenRead(ofdPath);
        foreach (var e in zip.Entries)
        {
            using var ms = new MemoryStream();
            using var s = e.Open();
            s.CopyTo(ms);
            res[e.FullName] = ms.ToArray();
        }
        return res;
    }

    [Fact]
    public void Attachment_ctor_validates_and_round_trips()
    {
        string file = CreateFile("a.txt");

        var att = new Attachment("说明书", file)
            .SetFormat("txt")
            .SetUsage("none")
            .SetVisible(true)
            .SetDisableReplace(true);

        Assert.Equal("说明书", att.GetName());
        Assert.Equal("txt", att.GetFormat());
        Assert.Equal("none", att.GetUsage());
        Assert.True(att.GetVisible());
        Assert.True(att.IsDisableReplace());
        Assert.Equal(file, att.GetFile());
        Assert.Equal("说明书", att.GetAttachment().GetAttachmentName());

        // 时间：设置与读取一致（ModDate 修复了 Java 返回创建时间的缺陷）
        var now = new DateTime(2024, 5, 6, 7, 8, 9);
        att.SetCreationDateTime(now).SetModDateTime(now.AddDays(1));
        Assert.Equal(now, att.GetCreationDateTime());
        Assert.Equal(now.AddDays(1), att.GetModDateTime());

        // 非法入参
        Assert.Throws<ArgumentException>(() => new Attachment("x", null));
        Assert.Throws<ArgumentException>(() => new Attachment("x", Path.Combine(_root, "nope.txt")));
        Assert.Throws<ArgumentException>(() => new Attachment("  ", file));
    }

    [Fact]
    public void Doc_attachment_is_placed_in_container()
    {
        string outPath = Path.Combine(_root, "with-att.ofd");
        string file = CreateFile("doc.txt", "attachment-body");

        using (var doc = new OFDDoc(outPath))
        {
            doc.Add(new Paragraph("page", 4));
            doc.AddAttachment(new Attachment("附件A", file).SetFormat("txt"));
        }

        var entries = ReadEntries(outPath);

        // 附件列表文件
        Assert.Contains("Doc_0/Attachments.xml", entries.Keys);
        string xml = Encoding.UTF8.GetString(entries["Doc_0/Attachments.xml"]);
        Assert.Contains("Attachment", xml);
        Assert.Contains("附件A", xml);
        Assert.Contains("Format=\"txt\"", xml);
        Assert.Contains("FileLoc", xml);

        // 附件实体文件已入包
        Assert.Contains(entries.Keys, k => k.StartsWith("Doc_0/Res/") && k.EndsWith("doc.txt"));
    }

    [Fact]
    public void Same_name_attachment_is_replaced_by_default()
    {
        string outPath = Path.Combine(_root, "replace.ofd");
        string f1 = CreateFile("a.txt", "one");
        string f2 = CreateFile("b.txt", "two");

        using (var doc = new OFDDoc(outPath))
        {
            doc.Add(new Paragraph("p", 4));
            doc.AddAttachment(new Attachment("同名", f1));
            doc.AddAttachment(new Attachment("同名", f2));
        }

        string xml = Encoding.UTF8.GetString(ReadEntries(outPath)["Doc_0/Attachments.xml"]);
        int count = xml.Split("<ofd:Attachment ").Length - 1 + xml.Split("<Attachment ").Length - 1;
        Assert.Equal(1, count);
        Assert.Contains("b.txt", xml);
        Assert.DoesNotContain("a.txt", xml);
    }

    [Fact]
    public void Disable_replace_keeps_both_entries()
    {
        string outPath = Path.Combine(_root, "keep.ofd");
        string f1 = CreateFile("a.txt", "one");
        string f2 = CreateFile("b.txt", "two");

        using (var doc = new OFDDoc(outPath))
        {
            doc.Add(new Paragraph("p", 4));
            doc.AddAttachment(new Attachment("同名", f1, disableReplace: true));
            doc.AddAttachment(new Attachment("同名", f2, disableReplace: true));
        }

        string xml = Encoding.UTF8.GetString(ReadEntries(outPath)["Doc_0/Attachments.xml"]);
        int count = xml.Split("<ofd:Attachment ").Length - 1 + xml.Split("<Attachment ").Length - 1;
        Assert.Equal(2, count);
        Assert.Contains("a.txt", xml);
        Assert.Contains("b.txt", xml);
    }

    [Fact]
    public void Delete_attachment_removes_entry()
    {
        string outPath = Path.Combine(_root, "del.ofd");
        string f1 = CreateFile("a.txt");
        string f2 = CreateFile("b.txt");

        using (var doc = new OFDDoc(outPath))
        {
            doc.Add(new Paragraph("p", 4));
            doc.AddAttachment(new Attachment("保留", f2));
            doc.AddAttachment(new Attachment("删除", f1));

            // 同一次会话内删除
            doc.DeleteAttachment("删除");
        }

        var entries = ReadEntries(outPath);
        Assert.Contains("Doc_0/Attachments.xml", entries.Keys);
        string xml = Encoding.UTF8.GetString(entries["Doc_0/Attachments.xml"]);
        Assert.Contains("保留", xml);
        Assert.DoesNotContain("删除", xml);
    }

    [Fact]
    public void Attachment_placement_uses_given_absolute_path()
    {
        string outPath = Path.Combine(_root, "abs.ofd");
        string file = CreateFile("c.txt");

        using (var doc = new OFDDoc(outPath))
        {
            doc.Add(new Paragraph("p", 4));
            // 指定到文档 Res 目录
            doc.AddAttachment("/Doc_0/Res", new Attachment("C", file));
        }

        var entries = ReadEntries(outPath);
        Assert.Contains(entries.Keys, k => k.StartsWith("Doc_0/Res/") && k.EndsWith("c.txt"));
    }
}
