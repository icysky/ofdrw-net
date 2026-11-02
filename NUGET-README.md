# OfdNet

[ofdrw](https://gitee.com/ofdrw/ofdrw) 的 **.NET 10** 移植，提供 OFD 文档的读取、编辑、排版生成、
数字签名、加密、OFD-A 归档转换与文档合并。

> 本项目是社区移植，不是 ofdrw 官方发布，不代表上游背书。

## 安装

推荐直接安装元包，它会带上全部模块：

```bash
dotnet add package OfdNet
```

也可以按需安装单个模块：

```bash
dotnet add package OfdNet.Core       # 数据模型
dotnet add package OfdNet.Pkg        # 虚拟容器 / 打包解包
dotnet add package OfdNet.Reader     # 读取、资源定位、关键字检索
dotnet add package OfdNet.Layout     # 排版生成
dotnet add package OfdNet.Sign       # 数字签名 / 签章
dotnet add package OfdNet.Crypto     # 加密 / 解密
dotnet add package OfdNet.Gm         # SM2 / SM3 / SES 国密结构
dotnet add package OfdNet.Font       # 字体度量
dotnet add package OfdNet.Archive    # OFD-A 检查与转换
dotnet add package OfdNet.Tool       # 合并、页面删除等工具
```

目标框架：**.NET 10**。

## 快速开始

### 排版生成 OFD

```csharp
using Ofd.Layout;
using Ofd.Layout.Element;

using var doc = new OFDDoc("out.ofd");
doc.Add(new Paragraph("Hello OFD", 5));
doc.Add(new Div(100, 40).SetBackgroundColor(230, 240, 255));
// Dispose / Close 时完成布局并打包
```

### 读取内容

```csharp
using Ofd.Reader;

using var reader = new OFDReader("out.ofd");
int pages = reader.GetNumberOfPages();
var page = reader.GetPage(1);
```

### 关键字检索

```csharp
using Ofd.Reader.Keyword;

var hits = KeywordExtractor.GetKeyWordPositionList(reader, "关键词");
```

### OFD-A 检查与转换

```csharp
using Ofd.Archive.Check;
using Ofd.Archive.Convert;

var violations = new OFDArchiveChecker().Check("in.ofd");
new OFDArchiveConverter().Convert("in.ofd", "out-ofda.ofd");
```

## 字体说明

- 随包提供 **Noto Serif SC**（SIL OFL 1.1），用于跨平台确定性排版；许可证随包附在
  `Assets/NotoSerif/OFL.txt`。
- **不提供** `FangSong_GB2312.ttf`。该字体版权归 GreatWall Computer Co.，没有再分发许可；
  需要时请自行获取已授权副本，文件缺失时字体会回退到系统宋体。
- 字体随文档嵌入时，请自行确认目标字体的再分发授权。

## 许可证与归属

- 本项目以 **Apache License 2.0** 发布，全文见包内 `LICENSE`。
- 上游归属、修改说明与第三方组件声明见包内 `NOTICE`、`THIRD-PARTY-NOTICES.md`。
- 依赖：BouncyCastle.Cryptography（MIT）、SkiaSharp（MIT）。

源码与问题反馈：<https://github.com/icysky/ofdrw-net>
