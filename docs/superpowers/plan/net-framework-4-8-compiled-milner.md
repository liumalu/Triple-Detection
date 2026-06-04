# Plan: 回退到 .NET Framework 4.8

## Context

用户要求将 Triple-Detection 项目从 .NET 8 回退到 .NET Framework 4.8。此操作涉及重大技术变更，需要重写多个层次。

## 当前技术栈 (.NET 8)

| 组件 | 当前版本 | .NET Framework 4.8 替代 |
|------|----------|------------------------|
| 框架 | net8.0-windows | net48 |
| 语言 | C# 12 | C# 7.3 (最大) |
| MVVM | CommunityToolkit.Mvvm 8.3.2 | 需替换 |
| DI | Microsoft.Extensions.DI 8.0.1 | 需替换 |
| ORM | EF Core 8.0.11 | Entity Framework 6 (EF6) |
| JSON | Newtonsoft.Json 13.0.3 | Newtonsoft.Json (可用) |
| VisionMaster SDK | v4.2.0 | 需验证兼容性 |

## 关键挑战

### 1. EF Core → EF6 重写 (关键)
- EF Core 在 .NET Framework 上不可用，需完全重写为 EF6
- API 差异显著：DbContext、Fluent API、查询语法均不同
- 估算工作量：2-4 周

### 2. MVVM 框架替换
- CommunityToolkit.Mvvm 专为 .NET 6+ 设计
- 需替换为 Prism 8.x + DryIoc (旧项目使用的方案)

### 3. DI 容器替换
- Microsoft.Extensions.DI 不可用
- 改用 DryIoc (Prism 8.x 捆绑)

### 4. C# 12 语法移除
- `.NET Framework 4.8` 仅支持 C# 7.3
- 需重写：primary constructors、collection expressions、raw string literals、records 等

### 5. VisionMaster SDK 验证
- 需确认 SDK 是否支持 .NET Framework 4.8
- 如果只提供 .NET Core DLL，则迁移不可能

## 实施步骤

### Phase 1: 可行性验证
1. 联系 VisionMaster 供应商确认 .NET Framework 4.8 支持
2. 如不支持则终止迁移

### Phase 2: 创建新项目结构
1. 创建 `TripleDetection.App.sln` (.NET Framework 4.8)
2. 修改 `TripleDetection.csproj`:
   - `net8.0-windows` → `net48`
   - `LangVersion` 12.0 → 7.3
   - 移除 `UseWindowsForms`

### Phase 3: NuGet 包调整
```
移除:
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.Data.Sqlite
- System.Drawing.Common

添加:
- Prism.DryIoc (8.x)
- EntityFramework (6.x)
- DryIoc (4.x)
```

### Phase 4: 代码重写

#### 4.1 EF6 DbContext
- 重写 `TripleDetectionDbContext.cs`
- 使用 EF6 的 `DbContext`、`DbModelBuilder` API
- 迁移所有实体配置

#### 4.2 MVVM 重构
- `ObservableObject` → `Prism.Mvvm.ViewModelBase`
- `RelayCommand` → `Prism.Commands.DelegateCommand`
- `WeakReferenceMessenger` → 移除或用 Prism EventAggregator

#### 4.3 DI 重构
- `IServiceCollection` → `DryIoc.IContainer`
- 或使用 Prism 的 `IContainerRegistry`

#### 4.4 C# 7.3 语法修复
- 移除所有 C# 8+ 语法
- 将 records 改回 class
- 移除 primary constructors

### Phase 5: 测试验证
- 编译验证
- 运行验证
- 功能回归测试

## 关键文件修改清单

| 文件 | 操作 |
|------|------|
| `TripleDetection.csproj` | 重写框架和包引用 |
| `Presentation/App.xaml.cs` | 重写 DI 容器 |
| `Presentation/ViewModels/*.cs` | MVVM 模式重写 |
| `Infrastructure/Persistence/TripleDetectionDbContext.cs` | EF6 重写 |
| `Infrastructure/Repositories/*.cs` | EF6 适配 |

## 风险提示

✅ **VisionMaster SDK 已确认支持 .NET Framework 4.8**

⚠️ **工作量估计：6-8 周**

## 验证方式

1. 编译: `msbuild TripleDetection.csproj /t:Rebuild /p:Configuration=Debug`
2. 运行: 启动应用程序，验证登录和主界面
3. 功能测试: 检测流程、数据库操作

---

## 详细实施计划

### Phase 1: 项目配置 (.csproj 重写)

**文件:** `TripleDetection.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>7.3</LangVersion>
    <OutputType>WinExe</OutputType>
    <UseWPF>true</UseWPF>
    <PlatformTarget>x64</PlatformTarget>
    <RootNamespace>TripleDetection</RootNamespace>
    <AssemblyName>TripleDetection</AssemblyName>
    <VmInstallPath>C:\Program Files\VisionMaster4.2.0</VmInstallPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Prism.DryIoc" Version="8.1.97" />
    <PackageReference Include="EntityFramework" Version="6.4.4" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Drawing.Common" Version="4.7.2" />
  </ItemGroup>

  <!-- VisionMaster SDK DLLs -->
  <ItemGroup>
    <Reference Include="VM.Core" HintPath="$(VmInstallPath)\Development\V4.x\ComControls\Assembly\VM.Core.dll" Private="True" />
    <!-- ... 其他 SDK 引用 ... -->
  </ItemGroup>
</Project>
```

**关键变更:**
- `TargetFramework`: `net8.0-windows` → `net48`
- `LangVersion`: `12.0` → `7.3`
- 移除 `Nullable>enable</Nullable>`
- 移除 EF Core 包
- 添加 `Prism.DryIoc` 8.1.97
- 添加 `EntityFramework` 6.4.4

---

### Phase 2: EF6 DbContext 重写

**文件:** `Infrastructure/Persistence/TripleDetectionDbContext.cs`

变更:
- `using Microsoft.EntityFrameworkCore` → `using System.Data.Entity`
- `DbContext` from EF6 namespace
- `DbSet<T>` 改为显式 `{ get; set; }`
- `OnModelCreating` 参数改为 `DbModelBuilder`
- `ApplyConfigurationsFromAssembly` → `Configurations.AddFromAssembly`

---

### Phase 3: EF6 配置类重写

**文件:** `Infrastructure/Persistence/Configurations/*.cs`

- `IEntityTypeConfiguration<T>` → `EntityTypeConfiguration<T>`
- `EntityTypeBuilder<T>` from EF6 namespace

---

### Phase 4: DI 容器迁移 (App.xaml.cs)

**文件:** `Presentation/App.xaml.cs`

`Microsoft.Extensions.DI` → `Prism.DryIoc`:
- `IServiceCollection` → `IContainerRegistry`
- `services.AddSingleton/X/Transient` → `container.RegisterInstance/Register/RegisterTransient`
- 继承 `PrismApplication` 或手动设置 DryIoc

---

### Phase 5: MVVM 模式迁移 (ViewModels)

**文件:** `Presentation/ViewModels/*.cs`

| 原模式 | 新模式 |
|--------|--------|
| `ObservableObject` | `ViewModelBase` |
| `[ObservableProperty]` | `SetProperty(ref _field, value)` |
| `RelayCommand` | `DelegateCommand` |
| `WeakReferenceMessenger` | `IEventAggregator` |

---

### Phase 6: C# 7.3 兼容性修复

- **Records → Classes**: `public record X(Y Z)` → class with constructor
- **Expression-bodied → 传统语法**: `=> Property` → `{ get { return ...; } }`
- **File-scoped namespaces → Block scoped**: `namespace X;` → `namespace X { }`

---

### Phase 7: Prism Events 定义

**新文件:** `Presentation/Events/PrismEvents.cs`

```csharp
public class LogAddedEvent : PubSubEvent<string> { }
public class DetectionResultEvent : PubSubEvent<DetectionResult> { }
```

---

### 实施顺序

1. `TripleDetection.csproj` - 修改框架和包引用
2. NuGet restore - 验证所有包可解析
3. C# 7.3 语法修复 - 转换 records、expression-bodied、file-scoped namespaces
4. EF6 DbContext + Configurations - 重写 DbContext 和所有实体配置
5. Prism Events - 定义 PubSubEvent 类
6. App.xaml.cs - DI 容器迁移到 Prism.DryIoc
7. ViewModels - 转换 ObservableObject→ViewModelBase, RelayCommand→DelegateCommand
8. Services - 用 IEventAggregator 替换 WeakReferenceMessenger
9. DatabaseInitializer - 适配 EF6
10. Build + Fix - 迭代修复兼容性
11. Runtime verification

---

### 关键文件清单

| 阶段 | 文件 |
|------|------|
| 1 | `TripleDetection.csproj` |
| 2 | `Infrastructure/Persistence/TripleDetectionDbContext.cs` |
| 2 | `Infrastructure/Persistence/Configurations/*.cs` |
| 5 | `Presentation/ViewModels/LoginViewModel.cs` |
| 5 | `Presentation/ViewModels/Detection/MainViewModel.cs` |
| 6 | `Presentation/App.xaml.cs` |
| 7 | `Presentation/Events/PrismEvents.cs` |