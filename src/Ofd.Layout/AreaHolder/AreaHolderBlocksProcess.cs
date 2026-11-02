using Ofd.Pkg.Container;

namespace Ofd.Layout.AreaHolder;

/// <summary>
/// 区域占位区块处理器（Java AreaHolderBlocksProcess）：列表文件的创建/读取/查找/删除。
/// </summary>
public static class AreaHolderBlocksProcess
{
    /// <summary>尝试获取区域占位区块列表，不存在则创建。</summary>
    public static AreaHolderBlocks Obtain(DocDir docDir)
    {
        if (docDir == null) throw new ArgumentException("文档目录（docDir）为空");
        return Exist(docDir) ? Get(docDir) : Create(docDir);
    }

    /// <summary>创建区域占位区块列表（已存在则覆盖）。</summary>
    public static AreaHolderBlocks Create(DocDir docDir)
    {
        if (docDir == null) throw new ArgumentException("文档目录（docDir）为空");

        var blocks = new AreaHolderBlocks();
        docDir.PutObj(AreaHolderBlocks.AreaHolderBlocksFile, blocks);
        return blocks;
    }

    /// <summary>获取区域占位区块列表。</summary>
    public static AreaHolderBlocks Get(DocDir docDir)
    {
        var obj = docDir.GetObjByFile(AreaHolderBlocks.AreaHolderBlocksFile);
        return new AreaHolderBlocks(obj);
    }

    /// <summary>是否存在区域占位区块列表文件。</summary>
    public static bool Exist(DocDir? docDir) =>
        docDir != null && docDir.Exist(AreaHolderBlocks.AreaHolderBlocksFile);

    /// <summary>按名称查找区域占位区块（同名返回第一个）。</summary>
    public static CT_AreaHolderBlock? Find(AreaHolderBlocks? blocks, string? areaName)
    {
        if (blocks == null || areaName == null) return null;

        return blocks.GetAreaHolderBlocks().FirstOrDefault(b => areaName == b.GetAreaName());
    }

    /// <summary>删除区域占位区块列表文件。</summary>
    public static void Delete(DocDir? docDir)
    {
        if (docDir == null) return;
        if (Exist(docDir)) docDir.DeleteFile(AreaHolderBlocks.AreaHolderBlocksFile);
    }
}
