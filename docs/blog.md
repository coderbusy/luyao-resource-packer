# LuYao.ResourcePacker - 优雅地管理 .NET 项目资源文件

在 .NET 项目开发中，你是否遇到过这些问题？

- 使用"嵌入的资源"时，需要手动编写代码来访问文件流，繁琐且容易出错
- "资源管理器"功能虽然方便，但需要额外的配置和操作步骤
- 资源文件被嵌入到 DLL 中，导致程序体积急剧膨胀
- 资源文件名是字符串硬编码，没有智能提示，容易写错

如果你也为这些问题困扰，那么 **LuYao.ResourcePacker** 正是为你准备的解决方案。

## 项目简介

LuYao.ResourcePacker 是一个轻量级的 .NET 资源文件打包和访问库，它通过以下特性优化了资源文件的使用体验：

### 核心特性

1. **构建时自动打包**：在编译时自动将多个资源文件打包成单个 .dat 文件，避免 DLL 体积膨胀
2. **智能分级压缩**：采用 GZip 自动压缩，根据文件大小和类型智能决策，优化包体空间
3. **目录扫描**：默认扫描项目中的 `Resources` 目录，自动识别所有资源文件
4. **MSBuild 深度集成**：无需额外配置，安装 NuGet 包即可自动启用
5. **简洁的运行时 API**：提供异步和同步两种方式读取资源
6. **强类型访问**：自动生成类似 Android R 类的强类型访问代码，支持智能提示和编译时检查
7. **高度可配置**：通过 MSBuild 属性灵活配置资源目录、输出文件名等

### 适用场景

- 需要内嵌配置文件、模板文件、数据文件的应用程序
- 包含大量静态资源的类库项目
- 需要优化程序包体积的项目
- 追求代码质量和类型安全的项目

## 安装方法

LuYao.ResourcePacker 通过 NuGet 包分发，支持多种安装方式。

### 方式一：使用 .NET CLI（推荐）

在项目目录下运行以下命令：

```bash
dotnet add package LuYao.ResourcePacker.MSBuild
dotnet add package LuYao.ResourcePacker
```

### 方式二：使用 Package Manager Console

在 Visual Studio 的包管理器控制台中运行：

```powershell
Install-Package LuYao.ResourcePacker.MSBuild
Install-Package LuYao.ResourcePacker
```

### 方式三：在 .csproj 文件中直接引用

编辑项目的 `.csproj` 文件，添加以下内容：

```xml
<ItemGroup>
  <PackageReference Include="LuYao.ResourcePacker.MSBuild" Version="1.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
  <PackageReference Include="LuYao.ResourcePacker" Version="1.0.0" />
</ItemGroup>
```

### 包说明

- **LuYao.ResourcePacker.MSBuild**：构建时工具包，包含 MSBuild 任务和源代码生成器
- **LuYao.ResourcePacker**：运行时库，提供资源读取 API

## 使用方式

### 基础使用

#### 1. 准备资源文件

在项目根目录创建 `Resources` 文件夹，并添加资源文件：

```
YourProject/
├── Resources/
│   ├── message.json
│   ├── config.txt
│   ├── template.html
│   └── data.xml
└── YourProject.csproj
```

#### 2. 构建项目

运行 `dotnet build`，资源文件将自动打包成 `YourProject.dat` 文件并复制到输出目录。

#### 3. 访问资源（传统方式）

使用 `ResourcePackageReader` 类读取资源：

```csharp
using LuYao.ResourcePacker;

// 创建资源读取器
using var reader = new ResourcePackageReader("YourProject.dat");

// 异步读取为字符串
string message = await reader.ReadResourceAsStringAsync("message");
Console.WriteLine(message);

// 异步读取为字节数组
byte[] data = await reader.ReadResourceAsync("config");

// 同步读取
string template = reader.ReadResourceAsString("template");

// 列出所有资源键
foreach (var key in reader.ResourceKeys)
{
    Console.WriteLine($"Resource: {key}");
}
```

### 高级使用：强类型 API

源代码生成器会自动创建一个名为 `R` 的内部静态类（类似 Android 开发），提供强类型的资源访问方式。

#### 1. 使用生成的 R 类

```csharp
using YourProject; // 导入包含 R 类的命名空间

// 使用常量访问资源键（编译时检查，支持智能提示）
Console.WriteLine(R.Keys.message);
Console.WriteLine(R.Keys.config);
Console.WriteLine(R.Keys.template);

// 使用生成的强类型方法读取资源
string message = await R.ReadMessageAsyncAsString();
byte[] configBytes = await R.ReadConfigAsync();
string template = await R.ReadTemplateAsyncAsString();

// 如果需要，也可以访问底层的 Reader
ResourcePackageReader reader = R.Reader;
```

#### 2. 强类型 API 的优势

- ✅ **智能提示**：IDE 自动提示可用的资源名称
- ✅ **编译时检查**：资源名拼写错误在编译时就能发现
- ✅ **重构安全**：重命名资源文件时，引用处会产生编译错误
- ✅ **代码清晰**：消除魔法字符串，代码意图更明确
- ✅ **类型安全**：每个资源都有对应的读取方法

### 自定义配置

在 `.csproj` 文件中配置项目属性：

```xml
<PropertyGroup>
  <!-- 启用/禁用资源打包（默认：true） -->
  <ResourcePackerEnabled>true</ResourcePackerEnabled>
  
  <!-- 自定义资源目录（默认：Resources） -->
  <ResourcePackerDirectory>MyResources</ResourcePackerDirectory>
  
  <!-- 自定义输出文件名（默认：$(AssemblyName).dat） -->
  <ResourcePackerOutputFileName>custom-name.dat</ResourcePackerOutputFileName>
</PropertyGroup>
```

### 流式读取大文件

对于大型资源文件，可以使用流式读取避免一次性加载到内存：

```csharp
using var reader = new ResourcePackageReader("YourProject.dat");

// 获取资源流
using var stream = reader.GetStream("large-file");

// 使用流进行处理
using var fileStream = File.Create("output.dat");
await stream.CopyToAsync(fileStream);
```

**压缩资源的流式读取**：对于压缩的资源，`GetStream()` 方法会返回一个自动解压的流，无需手动处理解压逻辑，同时避免了将整个解压后的内容加载到内存中。

## 智能压缩特性

LuYao.ResourcePacker 内置了智能分级压缩功能，在构建时自动优化资源包体积。

### 压缩策略

系统采用 GZip 压缩算法，根据文件大小和类型自动决策：

1. **小于 255 字节的文件**：不压缩
   - 理由：压缩开销大于收益
   
2. **255 字节 - 4KB 的文件**：尝试完整压缩
   - 只有压缩比达到 5% 以上才会保存压缩版本
   
3. **大于 4KB 的文件**：采样评估
   - 取前 8KB 作为样本进行压缩评估
   - 如果样本压缩比达到 5% 以上，则压缩完整文件
   
4. **已压缩格式**：自动跳过
   - 识别并跳过已压缩的文件格式（jpg、png、zip、mp3、mp4、pdf、字体文件等）

### 压缩效果

根据文件类型，压缩效果各不相同：

- **文本文件**：通常可达 50-70% 压缩率
- **JSON/XML 配置文件**：通常可达 60-80% 压缩率
- **源代码文件**：通常可达 50-65% 压缩率
- **二进制可执行文件**：通常可达 10-30% 压缩率
- **图片/音视频文件**：0-5% 压缩率（自动跳过）

### 使用方式

压缩是完全自动和透明的：

```csharp
// 打包时自动根据规则进行压缩（构建时）
// 无需任何配置

// 读取时自动解压（运行时）
var content = await reader.ReadResourceAsStringAsync("config");

// 流式读取压缩资源（自动解压）
using var stream = reader.GetStream("large-text-file");
```

### 技术细节

- **压缩算法**：GZip
- **最小压缩比**：5%
- **流式解压**：支持流式读取压缩资源，无需完整加载到内存
- **线程安全**：支持并发访问压缩资源
- **元数据存储**：索引中记录原始大小、压缩后大小、压缩标志
- **版本兼容**：压缩特性保持文件格式版本号为 1，向后兼容

## 实现原理

### 构建时处理

1. **MSBuild 集成**：通过 `.props` 和 `.targets` 文件在构建管道中注入自定义任务
2. **资源扫描**：`ResourcePackerTask` 扫描指定目录下的所有文件
3. **二进制打包**：使用自定义的二进制格式将资源打包成单个 `.dat` 文件
4. **源代码生成**：`ResourcePackageGenerator` 分析资源文件，生成强类型访问代码

### 文件格式设计

`.dat` 文件采用自定义的二进制格式，支持压缩：

```
[版本号: 1字节] [资源数量: 4字节] [索引区] [数据区]
```

**索引区结构**（每个资源）：
- 资源键名（字符串）
- 原始大小（4字节）
- 存储大小（4字节）
- 压缩标志（1字节）

**数据区**：存储资源的原始或压缩后的字节数据

这种设计的优势：
- 快速索引查找
- 支持随机访问
- 支持流式读取
- 支持透明压缩/解压
- 紧凑的存储格式
- 高效的空间利用

### 源代码生成器工作流程

1. **资源发现**：通过 `AdditionalFiles` 获取资源目录中的所有文件
2. **键名提取**：使用 `ResourceKeyHelper` 从文件路径提取资源键（去除扩展名）
3. **代码生成**：生成包含以下内容的 `R.g.cs` 文件：
   - `R.Keys` 嵌套类：包含所有资源键常量
   - `R.Reader` 静态属性：提供 `ResourcePackageReader` 实例
   - 强类型方法：为每个资源生成 `ReadXxxAsync()` 和 `ReadXxxAsyncAsString()` 方法

### 命名空间处理

源代码生成器遵循以下规则确定 `R` 类的命名空间：

1. 优先使用 `.csproj` 中的 `<RootNamespace>` 属性
2. 如果未设置，则使用程序集名称（`AssemblyName`）

这确保了生成的代码与项目的命名空间约定保持一致。

## 最佳实践与注意事项

### 最佳实践

#### 1. 使用有意义的资源文件名

资源键由文件名（不含扩展名）生成，使用清晰的命名：

```
✅ 推荐：
Resources/
├── app-config.json
├── email-template.html
└── user-data.xml

❌ 不推荐：
Resources/
├── config1.json
├── temp.html
└── data.xml
```

#### 2. 组织资源文件结构

将相关资源放在同一目录下，方便管理：

```
Resources/
├── app-config.json
├── email-template.html
├── report-template.html
├── user-data.xml
└── seed.sql
```

**注意**：资源键名只基于文件名（不含扩展名），不包含子目录路径。如果使用子目录组织资源，请确保不同子目录下的文件名不重复，否则会导致键名冲突。

#### 3. 选择合适的读取方式

- **小文件**：直接使用 `ReadResourceAsync()` 或 `ReadResourceAsStringAsync()`
- **大文件**：使用 `GetStream()` 进行流式读取
- **频繁访问**：考虑缓存读取结果

#### 4. 利用强类型 API

优先使用生成的 `R` 类而非字符串键名：

```csharp
// ✅ 推荐：强类型，有智能提示
var content = await R.ReadAppConfigAsyncAsString();

// ❌ 不推荐：魔法字符串，容易出错
var content = await reader.ReadResourceAsStringAsync("app-config");
```

#### 5. 处理多项目场景

**同一项目内的资源访问**：
```csharp
// 在项目内部直接使用 R 类
var data = await R.ReadDataAsyncAsString();
```

**跨程序集访问的限制**：

生成的 `R` 类默认使用 `internal` 可见性修饰符，这意味着它只能在定义它的程序集（项目）内部访问。如果你需要在其他项目中访问某个项目的资源，有以下几种方案：

1. **推荐方案**：在每个需要资源的项目中独立管理资源
   ```csharp
   // Project1 中
   var config1 = await R.ReadConfigAsyncAsString();
   
   // Project2 中  
   var config2 = await R.ReadConfigAsyncAsString();
   ```

2. **传递数据而非 R 类**：如果确实需要跨项目共享资源，可以在资源所在的项目中读取后传递数据
   ```csharp
   // 在 LibraryProject 中提供公共方法
   public class ResourceProvider
   {
       public static async Task<string> GetConfigAsync()
       {
           return await R.ReadConfigAsyncAsString();
       }
   }
   
   // 在 ConsumerProject 中使用
   var config = await LibraryProject.ResourceProvider.GetConfigAsync();
   ```

3. **使用传统 API**：跨程序集访问时使用 `ResourcePackageReader`
   ```csharp
   // 在其他项目中手动加载 .dat 文件
   var reader = new ResourcePackageReader("path/to/LibraryProject.dat");
   var data = await reader.ReadResourceAsStringAsync("config");
   ```

### 注意事项

#### 1. 资源文件名限制

- 文件名（不含扩展名）会转换为 C# 标识符
- 非法字符（如空格、特殊符号）会被替换为下划线
- 避免使用 C# 关键字作为文件名

#### 2. 构建输出

- `.dat` 文件会自动复制到输出目录
- 确保在部署时包含 `.dat` 文件
- 对于发布版本，`.dat` 文件必须与主程序集在同一目录

#### 3. 资源更新

- 修改资源文件后需要重新构建项目
- 生成的 `R` 类会自动更新
- 如果修改了资源文件名，相关代码会产生编译错误

#### 4. 性能考虑

- `ResourcePackageReader` 在初始化时会加载索引，但不会加载资源内容
- 每次 `ReadResourceAsync()` 调用都会创建新的文件流（线程安全）
- 考虑复用 `ResourcePackageReader` 实例，避免重复加载索引
- **压缩资源**：
  - 解压操作在读取时自动进行
  - 流式读取压缩资源时，解压是按需进行的，不会一次性加载整个文件
  - 对于频繁访问的压缩资源，考虑缓存解压后的内容

#### 5. 版本兼容性

- 当前格式版本为 1
- 未来版本会保持向后兼容
- 如果遇到不兼容的文件版本，会抛出 `InvalidDataException`

#### 6. 并发访问

- `ResourcePackageReader` 是线程安全的
- 每次读取操作使用独立的文件流
- 可以在多线程环境下安全使用

## 项目协议与支持

### 开源协议

LuYao.ResourcePacker 使用 **MIT 协议**开源，这意味着：

- ✅ 可以自由使用、修改、分发
- ✅ 可以用于商业项目
- ✅ 无需支付任何费用
- ⚠️ 需要保留原作者版权声明

### 获取支持

如果在使用过程中遇到问题，可以通过以下方式获取帮助：

#### 1. 查看文档

- [项目主页](https://github.com/coderbusy/luyao-resource-packer)
- [README 文档](https://github.com/coderbusy/luyao-resource-packer/blob/main/README.md)
- [示例项目](https://github.com/coderbusy/luyao-resource-packer/tree/main/examples)

#### 2. 提交 Issue

在 [GitHub Issues](https://github.com/coderbusy/luyao-resource-packer/issues) 页面：

- 报告 Bug
- 请求新功能
- 提出改进建议

提交 Issue 时，请包含：
- 问题的详细描述
- 复现步骤
- 预期行为和实际行为
- 环境信息（.NET 版本、操作系统等）

#### 3. 参与贡献

欢迎提交 Pull Request！贡献方式包括：

- 修复 Bug
- 添加新功能
- 改进文档
- 优化性能
- 增加测试用例

### 项目统计

[![NuGet Version](https://img.shields.io/nuget/v/LuYao.ResourcePacker)](https://www.nuget.org/packages/LuYao.ResourcePacker/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/LuYao.ResourcePacker)](https://www.nuget.org/packages/LuYao.ResourcePacker/)
[![GitHub Stars](https://img.shields.io/github/stars/coderbusy/luyao-resource-packer?style=social)](https://github.com/coderbusy/luyao-resource-packer/stargazers)

### 相关资源

- **NuGet 包**：[LuYao.ResourcePacker](https://www.nuget.org/packages/LuYao.ResourcePacker/)
- **源代码**：[GitHub Repository](https://github.com/coderbusy/luyao-resource-packer)
- **作者**：Soar360
- **创建日期**：2025-10-25

## 总结

LuYao.ResourcePacker 为 .NET 项目提供了一个优雅、高效的资源文件管理解决方案。通过自动化的构建时打包和强类型的运行时访问，它显著改善了开发体验，同时优化了程序包的体积。

### 核心优势

- 🚀 **零配置**：安装即用，无需复杂设置
- 💪 **强类型**：编译时检查，减少运行时错误
- 📦 **体积优化**：智能压缩 + 资源独立打包，显著减小包体积
- ⚡ **高性能**：支持流式读取和流式解压，适用于大文件
- 🔧 **高度可配置**：灵活适应不同项目需求
- 🌟 **Android 风格**：熟悉的 R 类设计，降低学习成本
- 🎯 **智能压缩**：自动识别文件类型，按需压缩，透明解压

无论你是开发小型工具还是大型应用，LuYao.ResourcePacker 都能帮助你更好地管理资源文件。现在就试试吧！

```bash
dotnet add package LuYao.ResourcePacker.MSBuild
dotnet add package LuYao.ResourcePacker
```

---

*如果觉得这个项目有用，欢迎在 [GitHub](https://github.com/coderbusy/luyao-resource-packer) 上给个 ⭐ Star！*
