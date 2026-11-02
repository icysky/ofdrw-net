# 第三方组件与许可证声明

本项目（ofdrwNet）自身以 **Apache License 2.0** 发布，全文见 [LICENSE](LICENSE)。

本项目是 [ofdrw](https://gitee.com/ofdrw/ofdrw)（Apache License 2.0）的 .NET 移植。
上游归属声明与修改说明见 [NOTICE](NOTICE)。上游原始代码与其 LICENSE 位于同一仓库的
`ofdrwJava/` 目录（如果发布范围包含该目录）。

以下组件随源码或二进制产物分发，各自保留原许可证。分发本项目的源码包、NuGet 包或
安装包时，必须同时附上对应的许可证全文与版权声明。

## 运行时依赖

| 组件 | 版本 | 许可证 | 用途 | 许可证全文 |
|---|---|---|---|---|
| BouncyCastle.Cryptography | 2.7.0 | MIT | SM2/SM3、X.509、PKCS#10/#12、CMS/GB/T 35275 | `THIRD-PARTY-LICENSES/BouncyCastle-Cryptography-LICENSE.md` |
| SkiaSharp | 3.119.0 | MIT | 字体度量（`Ofd.Font`） | `THIRD-PARTY-LICENSES/SkiaSharp-LICENSE.txt` |
| SkiaSharp.NativeAssets.Win32 | 3.119.0 | MIT（含 Skia 及其他原生依赖，见随包声明） | Windows 原生二进制 | `THIRD-PARTY-LICENSES/SkiaSharp-THIRD-PARTY-NOTICES.txt` |

> SkiaSharp 的 NuGet 包内含原生 Skia 及其第三方依赖，`SkiaSharp-THIRD-PARTY-NOTICES.txt`
> 为官方随包声明（约 140 KB），发布二进制时必须一并保留。

## 内置字体

### Noto Serif SC（Regular / Bold）

- 文件：`src/Ofd.Font/Assets/NotoSerif/NotoSerifSC-Regular.ttf`、`NotoSerifSC-Bold.ttf`
- 版权：Copyright © 2017-2023 Adobe (http://www.adobe.com/)
- 许可证：SIL Open Font License 1.1，全文见 `src/Ofd.Font/Assets/NotoSerif/OFL.txt`
- 允许随软件打包、嵌入和再分发；字体本身不得单独售卖；不得使用保留字体名发布修改版。

### FangSong_GB2312（不得公开分发）

- 文件：`src/Ofd.Font/Assets/FangSong/FangSong_GB2312.ttf`
- 字体内嵌声明：`(C) Copyright GreatWall Computer Co. 1994`，
  `Trademark of GreatWall Co., Beijing`
- **该字体为商业字体，未附带任何再分发许可。** 不得提交到公开 GitHub 仓库，
  不得放入 NuGet 包、安装包或对外交付物。
- **当前状态：已从 Git 索引移除**（`git rm --cached`），本地文件不会被删除，
  可用于本地构建与测试；`.gitignore` 已忽略 `src/Ofd.Font/Assets/FangSong/*.ttf`，
  防止误提交。
- 需要该字体的使用者请自行获取已授权副本，说明见
  [src/Ofd.Font/Assets/FangSong/README.md](src/Ofd.Font/Assets/FangSong/README.md)。

移除后的替代方案（任选其一）：

1. 使用者自备已授权的仿宋字体，放到 `Assets/FangSong/`（本地开发推荐）；
2. 改用可自由分发的仿宋字体（如 FandolFang，需遵守其 GPL + 字体例外条款），
   并同步调整 `Ofd.Font/BuiltinFonts.cs` 与相关 golden 基线；
3. 仅使用已内置的 Noto Serif SC（OFL）。

字体缺失时 `BuiltinFonts.FangSong()` 返回 `null`，排版会回退到系统仿宋/宋体；
依赖内置仿宋的测试需要相应跳过或改用其他字体。

## 测试与开发依赖（不随产品分发）

| 组件 | 版本 | 许可证 |
|---|---|---|
| Microsoft.NET.Test.Sdk | 17.12.0 | MIT |
| xunit | 2.9.2 | Apache License 2.0 |
| xunit.runner.visualstudio | 3.0.2 | Apache License 2.0 |

## 测试数据

`tests/Ofd.Tests/TestData/` 下的 `real-*.ofd`、`java-*.ofd` 等样本来自 ofdrw
测试资源（Apache License 2.0），仅用于兼容性与结构对拍测试。
样本中可能嵌入第三方字体或印章图片，对外再分发前请自行确认其权利状态。

## 商标

`OFDRW`、`ofdrw` 为上游项目名称；本项目仅以“ofdrw 的 .NET 移植”方式描述来源，
不使用其名称暗示官方认可或背书。Apache License 2.0 第 6 条不授予商标许可。
