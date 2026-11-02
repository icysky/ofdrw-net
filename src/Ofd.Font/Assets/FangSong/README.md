# 仿宋字体（不随仓库分发）

本目录用于放置内置仿宋字体 `FangSong_GB2312.ttf`，让生成端在 Linux/容器环境下
不依赖系统是否安装中文字体。

**该字体不随本仓库分发，需要你自行获取。**

## 原因

原随包字体 `FangSong_GB2312.ttf` 的字体内嵌声明为：

```
(C) Copyright GreatWall Computer Co. 1994
Trademark of GreatWall Co., Beijing
```

该字体没有任何再分发许可，因此不能提交到公开仓库，也不能放入 NuGet 包或对外交付物。
本仓库已将该文件从 Git 索引移除，并在 `.gitignore` 中忽略 `*.ttf`。

## 如何自行获取

1. 从你已获得授权的来源取得仿宋字体文件，例如：
   - Windows 中文字体包中的「仿宋_GB2312」；
   - 你所在单位已购买授权的商业仿宋字体。
2. 将文件复制到本目录，文件名必须为 `FangSong_GB2312.ttf`。
3. 重新构建，`Ofd.Font` 会自动复制到输出目录的 `Assets/FangSong/` 下。

也可以使用其他可自由分发的仿宋字体（如 FandolFang），但需要自行确认其许可证，
并且字体度量不同会影响排版位置与 golden 基线。

## 缺失时的行为

- `BuiltinFonts.FangSongPath` 返回 `null`，`BuiltinFonts.FangSong()` 返回 `null`；
- `Font.GetDefault()` 回退到系统「宋体」（SimSun）；系统也没有时由调用方显式指定字体；
- 依赖内置仿宋的测试（`tests/Ofd.Tests/Layout/OpticalKerningTests.cs`、
  `tests/Ofd.Tests/Layout/CanvasFontParsingTests.cs`）会因缺少字体失败，
  请在本地补齐字体后再运行，或自行调整这两个测试。

## 许可证

再次强调：不要把该字体提交到 Git，也不要随产品分发。
第三方组件与字体的整体许可说明见仓库根目录 [THIRD-PARTY-NOTICES.md](../../../../THIRD-PARTY-NOTICES.md)。
