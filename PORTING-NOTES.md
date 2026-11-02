# 移植说明（ofdrw → ofdrwNet）

本文说明 ofdrwNet 的来源、与上游 [ofdrw](https://gitee.com/ofdrw/ofdrw) 的差异
（新增 / 修改 / 移除），同时作为 Apache License 2.0 第 4(b) 条「标注修改」的说明。

上游归属与版权声明见 [NOTICE](NOTICE)，许可证全文见 [LICENSE](LICENSE)，
第三方组件与字体许可见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

## 1. 来源与版本

| 项 | 内容 |
|---|---|
| 上游项目 | OFD Reader & Writer (ofdrw) |
| 上游地址 | https://gitee.com/ofdrw/ofdrw（作者 Gitee 同步仓库，优先）<br>https://github.com/ofdrw/ofdrw（原始仓库） |
| 上游版本 | **2.4.0**（`ofdrwJava/pom.xml`） |
| 上游许可证 | Apache License 2.0 |
| 本项目 | ofdrwNet，C# / .NET 10 |
| 本项目许可证 | Apache License 2.0 |
| 移植方式 | 按模块与类结构对照移植；平台相关能力以 .NET 生态重写 |

移植覆盖 OFD 读取、编辑、排版生成、数字签名/签章、加密容器、OFD-A 归档转换与文档合并，
不依赖 Java 运行时。

## 2. 模块映射

| 上游 Java 模块 | 上游代码量（约） | .NET 工程 | 状态 |
|---|---:|---|---|
| `ofdrw-core` | 17,541 行 | `Ofd.Core` | 已移植 |
| `ofdrw-layout` | 12,975 行 | `Ofd.Layout` | 已移植 |
| `ofdrw-gm` | 4,571 行 | `Ofd.Gm` | 已移植 |
| `ofdrw-reader` | 4,249 行 | `Ofd.Reader` | 已移植 |
| `ofdrw-archive` | 3,393 行 | `Ofd.Archive` | 已移植（23 条规则 / 23 个处理器） |
| `ofdrw-sign` | 2,895 行 | `Ofd.Sign` | 已移植（SES V1/V4/V5、GB/T 35275） |
| `ofdrw-pkg` | 2,388 行 | `Ofd.Pkg` | 已移植 |
| `ofdrw-crypto` | 1,971 行 | `Ofd.Crypto` | 已移植（GM/T 0099） |
| `ofdrw-tool` | 991 行 | `Ofd.Tool` | 已移植 |
| `ofdrw-font` | 462 行 | `Ofd.Font` | 已移植（字体度量为重写，见 §4） |
| `ofdrw-converter` | — | 无 | **未移植** |
| `ofdrw-graphics2d` | — | 无 | **未移植** |
| `ofdrw-full` | — | 无 | 聚合 POM，无代码 |
| `ofdrw-gv` | — | 并入 `Ofd.Pkg` | 仅一个 `GlobalVar` |

## 3. 新增（上游没有、由本移植补充）

### 3.1 自研 XML DOM（替代 dom4j）

- 新增 `DomNode` / `DomElement` / `DomParser` / `DomSerializer`（`Ofd.Core/Dom`）与
  通用元素 `OFDElement`。
- 目标不是"随便一个 XML 库"：OFD 签名对字节敏感，序列化必须保留 dom4j 的输出契约
  （属性顺序、命名空间前缀、空元素写法、数字/时间格式）。这部分由 XML golden 测试
  与 Java 输出逐字节比对。
- 上游依赖的 `SAXReaderFactory`、`OFDNameSpaceModifier`、`DefaultElementProxy`、
  `OFDCommonQName` 等 dom4j 适配代码不再需要，其能力分别由自研 DOM、`NameSpaceModifier`、
  `OFDElement` 承接。

### 3.2 字体度量与内置字体

- 新增 `BuiltinFonts`、`FontMetrics`、`TextBounds`，提供"不依赖系统已安装字体"的
  字体加载与度量路径。
- `EnvFont` 重写为 .NET 侧跨平台字体目录探测（Windows / Linux）。
- 可选随包字体：Noto Serif SC（SIL OFL 1.1，可自由再分发）；仿宋需使用者自备
  （原因见 §7）。

### 3.3 签名辅助

- 新增 `Sm2SignUtil`：统一 SM2 签名（DER 编码 r,s）、流式 SM3 摘要与流读取，
  供 SES V1/V4/V5 与 GB/T 35275 容器复用；上游各容器为内联实现。

### 3.4 上游缺陷修复（OFD-A 转换）

`ofdrw-archive` 的上游实现在特定文档上不可用，本移植已修复：

1. 缺少 `DocumentRes.xml` / `PublicRes.xml` 时，`ResourcePlacementHandler` 抛
   `FileNotFoundException`；
2. 白名单 DFS 只匹配带引号的属性值，而 `DocRoot` 是元素文本，导致转换产物只剩
   `OFD.xml`（不可读）。

因此 **OFD-A 转换环节的行为以本移植为准**，与上游 2.4.0 不一致。

### 3.5 测试、对拍与工具

- xUnit 测试 400+ 个测试方法：单元、集成、端到端、结构签名 golden、Java 对拍。
- golden 基线覆盖：`STBase.fmt` 数字格式、XML 序列化字节、排版/注释/加密/签章/签名/水印结构。
- 双向互操作验证：Java 签名 → C# 验证，C# 签名 → Java 验证。
- 工具：`tools/Smoke`（最小控制台验证）；Java 对拍探针 `tools/javaprobe`、
  `tools/SignProbe` 位于开发仓库根目录（与 Java 参考源码同仓）。

### 3.6 工程化

- SDK 风格工程 + `Ofd.slnx` / `Directory.Build.props`（`net10.0`、nullable、implicit usings）。
- JUnit → xUnit；Maven → `dotnet` CLI。

## 4. 修改 / 替换（能力对应，实现不同）

| 上游实现 | 本项目实现 | 说明 |
|---|---|---|
| dom4j（解析/序列化） | 自研 DOM（`DomParser`/`DomSerializer`） | 保留 dom4j 序列化契约，golden 逐字节比对 |
| `java.awt.Font` + `FontMetrics` | SkiaSharp 读取物理字体度量 | 物理字体 advance 宽度实测与 AWT 0 偏差；字体名解析/行高口径需适配 |
| BouncyCastle Java（bcprov/bcpkix） | BouncyCastle.Cryptography 2.7.0 | SM2/SM3/SM4、X.509、PKCS#10/#12、CMS/GB/T 35275；ASN.1 结构逐字段移植 |
| `java.util.zip` / commons-compress | `System.IO.Compression`（ZipArchive） | 容器读写 |
| `javax.imageio` / AWT 图像 | SkiaSharp | PNG/JPEG/BMP 等；JBIG2 除外 |
| `java.time` / `BigDecimal` / `DecimalFormat` | `DateTime`/`DateTimeOffset`、`decimal` + 自定义格式化 | `STBase.fmt` 等格式逐字节复刻（含 Java 的"怪"行为） |
| Java 集合/迭代器/异常体系 | .NET 集合、LINQ、异常类型 | 语义对齐，不做逐方法照搬 |
| Java 命名（camelCase） | C# 命名（PascalCase） | 公共 API 全量重命名 |
| Java 接口（无前缀） | C# 接口（`I` 前缀） | 如 `ExtendSignatureContainer`→`IExtendSignatureContainer`、`SignIDProvider`→`ISignIdProvider`、`Drawer`→`IDrawer`、`Processor`→`IProcessor`、`ElementSplit`→`IElementSplit`、`RenderPrepare`→`IRenderPrepare`、`ContainerFileFilter`→`IContainerFileFilter` |
| `ofdrw-gv` 的 `GlobalVar` | 移入 `Ofd.Pkg` | 上游独立模块只含此类 |
| `ofdrw-pkg` 中 3 个 DOM 工具类 | 合并为等价的 .NET 工具 | 行为一致，文件组织不同 |
| `SignedDataValidateContainer` 等验证容器 | 统一为 `IValidateContainer` 实现 | 类组织调整，验证语义不变 |
| `DefaultElementProxy` / `OFDCommonQName` | 合并进 `OFDElement` | 自研 DOM 后不再需要代理层 |

## 5. 移除 / 未实现

### 5.1 未移植模块

- **`ofdrw-converter`**：PDF→OFD、OFD→PDF/图片/HTML/SVG、字体解析与子集化。
  依赖 PDFBox、AWT `Graphics2D` 与自绘 PDF 渲染器，.NET 侧无等价能力，
  属于独立产品线，未纳入本次移植。
- **`ofdrw-graphics2d`**：converter 专用的 `Graphics2D` 支撑。
- **`ofdrw-full`**：仅聚合依赖，无代码。
- **`ofdrw-gv`**：仅 `GlobalVar`，已并入 `Ofd.Pkg`。

连带不包含：fontbox TTF 解析器（5.4k 行）、JBIG2 解码、PDF 渲染引擎。

### 5.2 功能级移除

- **JBIG2 解码**：`ImageUtils.ReadJb2` 抛 `NotSupportedException`（SkiaSharp 不提供 JBIG2）。
- **字体子集化**：因数科等阅读器对 Noto 子集渲染异常，已回退为完整字体/系统字体，
  `tools/fonts/make_subsets.py` 仅作实验保留。
- **Java 专有依赖**：AWT/ImageIO/dom4j/commons-io/Maven/SLF4J 等不再引入。

### 5.3 随包商业字体

`FangSong_GB2312.ttf`（Copyright GreatWall Computer Co. 1994）**不再随仓库分发**。
使用者需自行获取并放到 `src/Ofd.Font/Assets/FangSong/`，说明见
[该目录 README](src/Ofd.Font/Assets/FangSong/README.md)。

## 6. 已知行为差异

| 项 | 说明 |
|---|---|
| JBIG2 解码 | 不支持，`ImageUtils.ReadJb2` 抛 `NotSupportedException` |
| 旧版读取器 | `DLOFDReader`（已标注过时）不解析 STAMP 结构，签章注释返回空 |
| 默认字体度量 | 未放置仿宋时回退系统宋体；Noto Serif SC 可作为替代 |
| 仿宋 ASCII 字宽 | 等宽 0.5em，数字后紧跟字母时视觉偏挤；可用 Noto 或开启 `OpticalKerning` |
| 归并文档 DocType | `OFDMerger` 产出为 `DocType=OFD`，需再转换一次才符合 OFD-A |
| 示例印章自签名 | 上游测试资源 `SESealData.DER` 自签名无效，仅用于结构对拍 |
| OFD-A 转换 | 本移植修复了上游两处缺陷，行为优于上游 |

## 7. 字体与再分发

- **Noto Serif SC**：SIL OFL 1.1，版权 © 2017-2023 Adobe，许可证随字体位于
  `src/Ofd.Font/Assets/NotoSerif/OFL.txt`，允许随软件分发。
- **仿宋 `FangSong_GB2312.ttf`**：商业字体，无再分发许可，**本仓库不提供**；
  请自行获取已授权副本。详见
  [Assets/FangSong/README.md](src/Ofd.Font/Assets/FangSong/README.md)。
- 其他第三方组件见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

## 8. 验证方式

```bash
dotnet build Ofd.slnx -c Release
dotnet test tests/Ofd.Tests/Ofd.Tests.csproj -c Release
```

关键回归点：结构签名 golden、C#↔Java 双向签名/验签互操作、OFD-A 转换后的页数与可读性。
