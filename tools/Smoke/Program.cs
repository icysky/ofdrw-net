using System.Diagnostics;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Reader;
using Ofd.Tool.Merge;

// 最小烟囱验证：逐步写日志（带内存），用于定位内存暴涨的具体步骤
// 用法：dotnet run --project tools/Smoke -c Release

string logPath = Path.Combine(Path.GetTempPath(), "ofdnet_smoke.log");
File.WriteAllText(logPath, "");

void Log(string msg)
{
    var p = Process.GetCurrentProcess();
    string line = $"{DateTime.Now:HH:mm:ss.fff} [MEM {p.WorkingSet64 / 1024 / 1024}MB] {msg}";
    File.AppendAllText(logPath, line + Environment.NewLine);
}

string root = Path.Combine(Path.GetTempPath(), "ofdnet_smoke_" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(root);

try
{
    Log("start");

    string a = BuildDoc("a.ofd", "A1", "A2");
    Log("built A");

    string b = BuildDoc("b.ofd", "B1");
    Log("built B");

    string dest = Path.Combine(root, "merged.ofd");
    using (var merger = new OFDMerger(dest))
    {
        Log("merger created");
        merger.Add(a);
        Log("added A");
        merger.Add(b);
        Log("added B (before close)");
    }
    Log("merged closed");

    Log($"[1b] A pages = {CountPages(a)}, B pages = {CountPages(b)}");
    Log($"[2] merged pages = {CountPages(dest)}");
    Log($"[3] merged per-page = {AllPages(dest)}");

    string dest2 = Path.Combine(root, "selected.ofd");
    using (var merger = new OFDMerger(dest2)) merger.Add(a, 2);
    Log($"[4] selected pages = {CountPages(dest2)}, texts={string.Join(",", PageTexts(dest2, 1))}");

    string dest3 = Path.Combine(root, "mixed.ofd");
    using (var merger = new OFDMerger(dest3)) merger.AddMix(a, 1, b, 1);
    Log($"[5] mixed pages = {CountPages(dest3)}, texts={string.Join(",", PageTexts(dest3, 1))}");

    Log("SMOKE-OK");
    return 0;
}
catch (Exception ex)
{
    Log("SMOKE-FAIL: " + ex.GetType().Name + " " + ex.Message);
    return 1;
}
finally
{
    try
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
    catch
    {
        // ignore
    }
}

string BuildDoc(string name, params string[] texts)
{
    string docsRoot = Path.Combine(Path.GetTempPath(), "ofdnet_smoke_docs");
    Directory.CreateDirectory(docsRoot);
    string path = Path.Combine(docsRoot, name + "-" + Guid.NewGuid().ToString("N")[..6] + ".ofd");

    Log($"  build {name}: create doc");
    var doc = new OFDDoc(path);
    foreach (var t in texts)
    {
        Log($"    add paragraph '{t}'");
        doc.Add(new Paragraph(t, 4).SetHeight(240));
    }

    Log($"  build {name}: closing (layout + package)");
    doc.Dispose();
    Log($"  build {name}: closed");
    return path;
}

string AllPages(string ofd)
{
    using var reader = new OFDReader(ofd);
    var parts = new List<string>();
    for (int i = 1; i <= reader.GetNumberOfPages(); i++)
        parts.Add($"p{i}[{string.Join("|", PageTexts(ofd, i))}]");
    return string.Join(" ", parts);
}

int CountPages(string ofd)
{
    using var reader = new OFDReader(ofd);
    return reader.GetNumberOfPages();
}

List<string> PageTexts(string ofd, int pageIndex)
{
    using var reader = new OFDReader(ofd);
    var page = reader.GetPage(pageIndex);
    return page.GetContent()!.GetLayers()
        .SelectMany(l => l.GetPageBlocks())
        .SelectMany(b => b switch
        {
            TextObject t => t.GetTextCodes().Select(c => c.GetContent() ?? ""),
            CT_PageBlock pb => pb.GetPageBlocks().OfType<TextObject>()
                .SelectMany(t => t.GetTextCodes()).Select(c => c.GetContent() ?? ""),
            _ => Enumerable.Empty<string>(),
        })
        .ToList();
}
