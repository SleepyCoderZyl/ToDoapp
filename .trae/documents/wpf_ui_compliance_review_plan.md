# WPF / .NET 10 UI 规范全量审查与改进建议

> 任务: `/plan` — 检查所有页面 UI 是否符合 WPF 规范
> 流程: ① 检索所依赖 .NET 版本最佳实践 ② 全面审计 12 个 XAML 页面 ③ 输出分级建议
> 状态: 检索 + 审计完成；建议已分 P0/P1/P2/P3 列出，等待用户决定实施范围

---

## 1. 摘要 (Executive Summary)

**整体结论**: 项目主体符合 WPF + MVVM 最佳实践，但 `MainWindow.xaml` 因 908 行单体文件 + 5 个 `*.cs` 分部类代码后置，已经偏离"声明式 XAML"的核心精神，构成 **可维护性热点**。同时存在 **可访问性 (AutomationProperties) 缺失** 和 **XAML 模板重复** 两大共性问题。

| 维度 | 评分 | 说明 |
|------|------|------|
| 整体架构 (MVVM 分离) | ★★★★☆ | 6 个设置面板已完成 XAML 化重构；App.xaml Implicit DataTemplate 正确 |
| XAML 可维护性 | ★★☆☆☆ | MainWindow.xaml 908 行单文件；4 个 Tab 内容完全 inline |
| 可访问性 (a11y) | ★☆☆☆☆ | 全局 0 处 `AutomationProperties.Name`；图标按钮仅靠 ToolTip 提示 |
| 资源/主题化 | ★★★★★ | DynamicResource + LightTheme/DarkTheme 切换 + 颜色/笔刷分层完整 |
| 命令绑定 (ICommand) | ★★★★☆ | 设置面板 100% 命令化；MainWindow 标题栏仍以 Click= 事件为主 |
| 性能 (虚拟化) | ★★★★★ | ListBox 均开启 `VirtualizingPanel.IsVirtualizing="True"` + Recycling |
| 国际化/本地化 | ★★☆☆☆ | XAML 中文字符串硬编码，无 `resx`/`.x:Static` 机制 |

**P0 风险 (建议优先处理)**:
1. `MainWindow.xaml` 单文件 908 行 — 拆分 UserControl
2. `MainWindow.xaml.cs` 5 个分部类 + 18 个事件 `+=` / 4 个 `-=` 订阅 — 部分订阅可能泄漏
3. 全局缺失 `AutomationProperties.Name` — Windows 商店认证/屏幕阅读器直接不通过

---

## 2. 检索到的 .NET 10 / WPF 最佳实践 (Best Practice Research)

> 通过 context7 查询 `/dotnet/wpf` (官方仓库 llms.txt) 与现有项目规则的交叉验证。

### 2.1 官方 Microsoft WPF 核心原则 (dotnet/wpf)

1. **XAML 是声明式 UI 模型**: "WPF utilizes XAML to offer a declarative model for application programming, enabling separation of UI design from business logic."  任何"在 C# 里 `new FrameworkElement()`"都应当被视为反模式（**本项目设置页已修复**）。
2. **MVVM 是默认架构**: "WPF applications typically follow the MVVM pattern, leveraging data binding and commands to maintain separation of concerns."  命令 (`ICommand`) 优先于事件处理器 (`Click=`, `KeyDown=`)。
3. **数据绑定首选**: "Use the Binding markup extension in XAML to connect UI elements to data sources." 绑定应是 `INotifyPropertyChanged` ViewModel 属性。
4. **XAML 性能路线图** ([dotnet/wpf/roadmap.md](https://github.com/dotnet/wpf/blob/main/roadmap.md)): 重点在 **内存占用、启动时间、渲染性能、可访问性**。

### 2.2 派生最佳实践 (社区/Prism/微软 MVP 共识)

| 主题 | 最佳实践 | 反模式 |
|------|----------|--------|
| 大型 Window | 拆为多个 `UserControl` + ContentControl/DockPanel 组合 | 单一 Window 嵌入 1000+ 行布局 |
| 列表项模板 | 抽取到 `Resources/DataTemplate x:Key="..."` 或 `ItemTemplateSelector` | 多个 ListBox 重复写 `<DataTemplate>` |
| 图标按钮 | `AutomationProperties.Name="..."` + `ToolTip` 双标注 | 仅靠 ToolTip（无障碍工具读不出） |
| 拖动标题栏 | `WindowChrome` + `MouseLeftButtonDown` (本项目做法) ✅ | 自己处理 WM_NCHITTEST |
| 命令 vs 事件 | `Command="{Binding XxxCommand}"` | `Click="Xxx_Click"` 直接绑事件 |
| 颜色 | `DynamicResource` 引用主题字典 | 控件内硬编码 `Brush="#..."` |
| 尺寸/间距 | .NET 6+ 的 `Grid.RowSpacing` / `Grid.ColumnSpacing` / `StackPanel.Spacing` | 每个子元素 `Margin="0,8,0,0"` |
| 资源键 | `<sys:Double x:Key="...">` 与 `<CornerRadius>` 同级 | 字符串常量在多个 XAML 重复 |
| 设计时数据 | `d:DataContext="{d:DesignInstance Type=vm:XxxVM}"` | 缺设计器预览 |
| 渲染优化 | `VirtualizingPanel.IsVirtualizing=True`、`Recycling`、缓存 `DataTemplate` | `ListBox` 默认栈式渲染 |
| DPI 感知 | `PerMonitorV2` + `SnapsToDevicePixels=True`（已用） | 硬编码 `FontSize=14` 不随 DPI 缩放 |

### 2.3 .NET 10 WPF 运行时新行为 (待确认)

- 启动优化: 减少默认 theme dictionary 合并次数 → 与本项目无关
- 渲染线程优化: 已经默认开启
- Source: [dotnet/wpf GitHub](https://github.com/dotnet/wpf)

---

## 3. 当前架构快照 (Phase 1 探索结论)

### 3.1 12 个 UI 文件清单

| # | 文件 | 类型 | 行数 | 复杂度 |
|---|------|------|------|--------|
| 1 | `ToDoapp/Views/MainWindow.xaml` | Window | **908** | 🔴 高 |
| 2 | `ToDoapp/Views/WidgetView.xaml` | UserControl | 233 | 🟡 中 |
| 3 | `ToDoapp/Views/WidgetWindow.xaml` | Window | 42 | 🟢 低 |
| 4 | `ToDoapp/Views/QuickAddWindow.xaml` | Window | 109 | 🟢 低 |
| 5 | `ToDoapp/Views/SettingsWindow.xaml` | Window | 160 | 🟢 低 |
| 6 | `ToDoapp/Views/StartupReminderWindow.xaml` | Window | 159 | 🟢 低 |
| 7 | `ToDoapp/Views/Settings/General/HotKeySettingsView.xaml` | UserControl | 75 | 🟢 低 |
| 8 | `ToDoapp/Views/Settings/General/StartupSettingsView.xaml` | UserControl | 31 | 🟢 低 |
| 9 | `ToDoapp/Views/Settings/General/StartupReminderSettingsView.xaml` | UserControl | 260 | 🟡 中 |
| 10 | `ToDoapp/Views/Settings/General/StartInWidgetModeSettingsView.xaml` | UserControl | 31 | 🟢 低 |
| 11 | `ToDoapp/Views/Settings/Appearance/OpacitySettingsView.xaml` | UserControl | 89 | 🟢 低 |
| 12 | `ToDoapp/Views/Settings/Appearance/AlwaysOnTopSettingsView.xaml` | UserControl | 31 | 🟢 低 |
| 13 | `ToDoapp/App.xaml` | Application | 47 | 🟢 低 |
| 14 | `ToDoapp/Themes/Generic.xaml` | ResourceDict | (CustomControl) | 🟡 中 |

### 3.2 已发现的 WPF 规范偏差清单

#### 🔴 P0 — 必须修复 (直接影响可维护性 / 内存)

| ID | 位置 | 问题 | 修复方向 |
|----|------|------|----------|
| P0-1 | `MainWindow.xaml` L1-908 | **单文件 908 行**: 4 个 Tab 全 inline；标题栏/输入区/列表区/底栏写在同一 XAML | 拆为 `TitleBarView` / `TaskInputView` / `PendingTaskListView` / `CompletedTaskListView` / `ArchivedTaskListView` / `TrashListView` / `StatusBarView` 7 个 UserControl + 1 个 `MainWindow.xaml` 仅做容器 |
| P0-2 | `MainWindow.xaml` L218-240, 350-373, 776-799, 218-241 | **ListBoxItem 模板重复 4 次**: "未完成/已完成/归档/垃圾箱" 4 个 Tab 的 ItemContainerStyle 完全一致 | 抽到 `Resources/ModernStyles.xaml` 的 `<Style x:Key="TaskListBoxItemStyle">` |
| P0-3 | `MainWindow.xaml.cs:73-76` | **事件订阅部分未释放**: `_opacityManager.OpacityChanged += OnOpacityChanged` 在 `OnClosed` 找不到对应 `-=`（已确认 `SettingsChanged` 也无对应 `-=`）。WPF 不会因窗口关闭自动解 `+=` 静态/单例事件 | 在 `OnClosed` 中增加 `-=` 配对；或使用 `WeakEventManager` |
| P0-4 | 全局 0 处 | **缺少 `AutomationProperties.Name`**: 所有图标按钮（设置/主题/最小化/关闭/删除/归档/恢复等 ~15 个）仅设 `ToolTip`，屏幕阅读器 (Narrator) 完全无法朗读 | 每个图标按钮加 `AutomationProperties.Name="..."` (中文即可) |

#### 🟡 P1 — 强烈建议 (一致性 / 可访问性 / 性能)

| ID | 位置 | 问题 | 修复方向 |
|----|------|------|----------|
| P1-1 | `MainWindow.xaml:9` | 缺 `d:DataContext` / `d:DesignHeight/Width` | 加 `xmlns:vm="clr-namespace:ToDoapp.ViewModels"` + `d:DataContext="{d:DesignInstance Type=vm:MainWindowViewModel}"` |
| P1-2 | `MainWindow.xaml:219-241` 等 | `RenderTransform` + `RenderTransformOrigin` 已设但未配任何动画 (ScaleX/ScaleY 固定为 1) — 死代码 | 删除未使用的 Setter |
| P1-3 | `MainWindow.xaml:251-254` | `CheckBox.Checked` / `Unchecked` 走 code-behind 事件 (`TaskCheckBox_Checked`) — 与项目其他地方的 `ICommand` 风格不一致 | 改用 `Behaviors` + `EventToCommand` (已引 `Microsoft.Xaml.Behaviors.Wpf`)，或直接在 VM 暴露 `ICommand` |
| P1-4 | `MainWindow.xaml:262-268, 395-401, 814-820` | **TextBlock.Width 通过 Binding 计算**: 用了 `WidthAdjustConverter` 计算 `actualWidth - 71`，是已知反模式 | 改为 `Grid.IsSharedSizeScope` + `SharedSizeGroup`；或干脆移除显式 Width 让 TextBlock 走 `TextTrimming` + 父容器布局 |
| P1-5 | `MainWindow.xaml:90, 99, 108, 116, 125, 177, 324, 446, 463, 623, 640, 846, 863` | `Fill="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}"` 跨越视觉树查找 | 写一个 `IconPathStyle` (基于 `Path` 的隐式 Style) 通过 `AttachedProperty` 同步 `Foreground`；或用 `ControlTemplate` 内 `TemplateBinding Foreground` |
| P1-6 | `Resources/ModernStyles.xaml:344-360` (WidgetScrollBarStyle) | 缺 `Horizontal` Orientation 处理；与 `ModernScrollBarStyle` 几乎重复 | 合并为 `ModernScrollBarStyle` + Orientation 模板分支 |
| P1-7 | `Resources/Themes/DarkTheme.xaml:30` | `DangerSubtleBrush` 用 `#33F85149` (ARGB 字面量) 与项目其他颜色 "Color + Brush" 分层不一致 | 改为 `<Color x:Key="DangerSubtleColor">#33F85149</Color>` + `<SolidColorBrush ... />` |
| P1-8 | `Resources/Themes/DarkTheme.xaml:46, 49, 60, 65` | `WindowBackgroundBrush` / `ContentBackgroundBrush` / `WidgetBackgroundBrush` / `DialogBackgroundBrush` 全部是 `#202020` | 合并为 1 个 `SurfaceBaseBrush` 或在 DarkTheme 中显式差异（如 1% 灰度差） |
| P1-9 | `SettingsWindow.xaml:62-71` 等 6 个 Settings UC | `FontSize="14"` 硬编码 6 次 | 抽取为 `Style x:Key="SettingsHeaderStyle"` / `"SettingsDescriptionStyle"` 集中管理 |
| P1-10 | 全局 0 处 | 无 `x:Uid` 标记 — 阻碍后续本地化 (i18n) | 关键文本（按钮/标题/标签）加 `x:Uid="Title_Label"` (注: 非强制) |

#### 🟢 P2 — 可选 (整洁度 / 文档化)

| ID | 位置 | 问题 | 修复方向 |
|----|------|------|----------|
| P2-1 | `MainWindow.xaml:14` | `Background="Transparent"` + `AllowsTransparency="True"` 与 `WindowChrome.ResizeBorderThickness="8"` 共存时存在鼠标 hit-test 边界问题 | 保持现状（现代硬件无影响） |
| P2-2 | `MainWindow.xaml:46-58` | 窗口启动 `EventTrigger` + `Storyboard` 淡入 — 仅作用于 `MainBorder.Opacity`；若 `ShowActivated=False` 时窗口已经可见，会有 0.3s 半透明闪烁 | 改用 `Window.ContentRendered` 事件 + code-behind 触发 `BeginStoryboard` |
| P2-3 | `MainWindow.xaml.cs:24` | `private DispatcherTimer _mainTimer = new();` 字段初始化为 `new()`，会立即分配；构造函数内才 Stop | 改为 `private readonly DispatcherTimer _mainTimer = new();` 并在 ctor 末尾 Stop |
| P2-4 | `Resources/ModernStyles.xaml:1-431` | 全部样式集中 1 个文件 (431 行) | 按控件类型拆为 `ButtonStyles.xaml` / `TextBoxStyles.xaml` / `ListBoxStyles.xaml` / `MenuStyles.xaml` |
| P2-5 | `Settings/General/StartupReminderSettingsView.xaml:155-165, 238-248` | 用 `DataTrigger` 监听 `Items.Count == 0` 显示空态 | 写一个 `CollectionEmptyToVisibilityConverter` 取代冗长 DataTrigger |
| P2-6 | `MainWindow.xaml:11-12` | `WindowStyle="None"` + `AllowsTransparency="True"` + 自绘 `WindowChrome` 缺任务栏右键菜单 / 拖到顶部自动最大化 | 通过 `TaskbarItemInfo` 配合 `WindowChrome` 可补；但对工具型 App 是合理 trade-off |
| P2-7 | `ModernTextBoxStyle` (`Resources/ModernStyles.xaml:137-173`) | 自定义 ControlTemplate 替换了默认 TextBox 模板，**未保留 PART_ContentHost 之外的可访问性属性** (e.g. `Label` 关联) | 检验 `AutomationProperties.LabeledBy` 是否仍能工作 (本项目未用 `<Label>`，暂不构成问题) |

#### ⚪ P3 — 已合规 — 留作记录

- ✅ `App.xaml` Implicit DataTemplate 注册 6 个 Settings VM 类型
- ✅ `SettingsWindow` 6 个面板 `d:DataContext` 完整
- ✅ ListBox 全部开启 `VirtualizingPanel.IsVirtualizing="True"` + `Recycling`
- ✅ 颜色与笔刷分层：DarkTheme/LightTheme 各自定义 Color + Brush
- ✅ 命令优先：`HotKeySettingsView`、`StartupReminderSettingsView` 增删改全部 `Command=`
- ✅ WindowChrome 拖动：所有自定义标题栏统一 `MouseLeftButtonDown` 处理
- ✅ 资源键（`RadiusWindow`、`RadiusControl` 等）集中于 `ModernColors.xaml`

---

## 4. 分级实施建议 (按 karpathy-guidelines "不过度工程")

### 4.1 P0 修复 (建议本会话做)

| 任务 | 涉及文件 | 预期收益 | 风险 |
|------|----------|----------|------|
| **P0-1**: 拆 `MainWindow.xaml` 为 7 个 UserControl | 新建 7 个 `.xaml(.cs)` + 修改 `MainWindow.xaml` | 单文件 < 200 行；可独立测试/复用 | 中等（需验证 Tab 切换、拖动、Widget 模式不破） |
| **P0-2**: 抽取 `TaskListBoxItemStyle` | 修改 `ModernStyles.xaml` + 3 个 Tab 的 `ItemContainerStyle` | 减少 4 份重复模板 ~120 行 | 低 |
| **P0-3**: 事件订阅 `-=` 配对 | 修改 `MainWindow.xaml.cs` `OnClosed` | 消除潜在内存泄漏 | 低 |
| **P0-4**: 全局补 `AutomationProperties.Name` | 修改所有 12 个 XAML | 通过屏幕阅读器 + Windows 认证 | 低 |

### 4.2 P1 修复 (建议在 P0 完成后做)

- P1-1 加 MainWindow 设计时 `d:DataContext`
- P1-2 删 RenderTransform 死代码
- P1-3 改 `ICommand` 替代 `CheckBox.Checked/=Unchecked` 事件 (主窗体) — 影响范围有限
- P1-4 `SharedSizeGroup` 替代 `WidthAdjustConverter`
- P1-5 写 `IconPathStyle` 替代 `RelativeSource AncestorType` 视觉树查找
- P1-7, P1-8 颜色系统一致性整理

### 4.3 不建议做的 "过度优化" (karpathy-guidelines 原则)

- ❌ **不引入** Prism Region/Module — 项目规模不允许
- ❌ **不引入** `CommunityToolkit.Mvvm` — 已有 `ObservableObjectBase`
- ❌ **不引入** i18n 框架 (resx) — 当前不需多语言，硬编码中文更易维护
- ❌ **不拆分** `ModernStyles.xaml` 为 4 个文件 — 431 行可接受，拆分后无明显收益
- ❌ **不改写** `WindowChrome` — 现有方案已稳定
- ❌ **不全局替换** `Click=` 事件为 `Command` — Settings 面板已 100% 命令化，主窗体少量 Click 是合理 trade-off

---

## 5. 建议落地步骤 (决策表)

> 因本次任务范围大，建议先确认是否要做 P0，再依次推进。

| 步骤 | 内容 | 是否纳入本任务 |
|------|------|---------------|
| Step 1 | 全局补 `AutomationProperties.Name` (P0-4) | 建议做 |
| Step 2 | 事件订阅 `-=` 配对 (P0-3) | 建议做 |
| Step 3 | 抽取 `TaskListBoxItemStyle` (P0-2) | 建议做 |
| Step 4 | 拆 `MainWindow.xaml` (P0-1) | **需用户决策** — 工程量最大 |
| Step 5 | P1 系列清理 | 视 P0 完成情况 |
| Step 6 | 调 `TRAE-code-review` 验证 | 强制 |

---

## 6. 验证 (Verification)

无论实施哪一步，完成后建议执行：

1. **编译**: `dotnet build e:\Working\todoapp\ToDoapp\ToDoapp.csproj` — 无错无警告
2. **测试**: `dotnet test e:\Working\todoapp\ToDoapp.Tests\ToDoapp.Tests.csproj` — 全绿
3. **运行验证清单**:
   - [ ] 启动应用 → 主窗口 4 个 Tab 切换正常
   - [ ] 添加/勾选/删除待办 正常
   - [ ] 设置页 6 个面板切换正常 + 主题切换后颜色刷新
   - [ ] 小组件模式 (Widget) 拖动/缩放/折叠/展开正常
   - [ ] 全局快捷键 (添加待办/显示主窗) 正常
   - [ ] 启动提醒弹窗 OK
4. **可访问性** (如 Step 1 完成):
   - [ ] Windows Narrator 朗读图标按钮名称
5. **代码审查**: 调 `TRAE-code-review` skill 走最终检查

---

## 7. 引用 (References)

- [dotnet/wpf GitHub](https://github.com/dotnet/wpf) — 官方仓库
- [WPF Roadmap (Fundamentals: Performance, Accessibility)](https://github.com/dotnet/wpf/blob/main/roadmap.md)
- [WPF Data Binding 文档](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/)
- [WPF MVVM 模式概述](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/architecture/mvvm-overview)
- [WPF Automation Properties 文档](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/accessibility/automation-properties)
- 项目内历史审查:
  - [.trae/documents/settings_xaml_best_practices_verification.md](file:///e:/Working/todoapp/.trae/documents/settings_xaml_best_practices_verification.md) — 设置页 XAML 化重构已完成
  - [.trae/documents/code_review_report.md](file:///e:/Working/todoapp/.trae/documents/code_review_report.md) — 上一轮 code review 报告

---

## 8. 待用户决策 (AskUserQuestion)

请用户确认实施范围，再进入执行阶段:

1. **P0-1 (拆 MainWindow)** 工程量最大，是否纳入本任务？
2. **P0-2/3/4** (小修复) 是否一次性都做？
3. **P1 系列** 是当前一并做还是分批做？
