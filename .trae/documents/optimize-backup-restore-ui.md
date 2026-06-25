# 优化"恢复备份"对话框 UI 计划

## 摘要

优化"恢复备份"对话框的配色、布局和尺寸，使其与应用整体风格一致，符合 Win11 设计最佳实践。移除不必要的文件名显示，减小整体尺寸，并修复"最新"标签改变列表项高度的问题。

## 当前状态分析

### 问题识别

1. **配色硬编码，不匹配主题**：`DialogBackupListBoxItemStyle` 中使用了大量硬编码颜色（`#F2F2F2`, `#333333`, `#555555`, `#FFFFFF` 等），未使用应用的动态主题资源。在深色主题下会严重不协调。
2. **不必要的元素**：`BackupItemDataTemplate` 中显示了文件名（如 `todos-20260609-104551897.json`），对用户无意义。
3. **尺寸过大**：ListBox 高度固定 320，对话框宽度 480，且每项 MinHeight=44 加上文件名和标签后实际更高，在只有几个备份时显得臃肿。
4. **"最新"标签改变高度**：标签位于垂直 StackPanel 中，当显示时会撑高整个列表项。
5. **选中状态不美观**：选中时背景变为深灰 `#555555`，与应用橙色系主题不符。

### 涉及文件

* `ToDoapp\Resources\AppDialogStyles.xaml` — 备份项数据模板和列表项样式

* `ToDoapp\Views\MainWindow.Integration.cs` — 对话框动态构建代码（ListBox 高度、对话框宽度）

## 应用现有配色体系

应用已定义了完整的动态主题资源，应使用这些资源确保深浅主题自适应：

* 背景：`CardBrush` / `PanelBrush` / `HoverBrush`

* 文字：`TextPrimaryBrush` / `TextSecondaryBrush` / `TextMutedBrush`

* 主色：`PrimaryBrush` / `PrimarySubtleBrush`

* 边框：`BorderBrush`

* 圆角：`RadiusControl` (4)

## 拟议修改

### 1. 修改 `AppDialogStyles.xaml` — `BackupItemDataTemplate`

**位置**：第 80\~126 行
**修改内容**：

* 移除文件名 `TextBlock`（第 92\~96 行）

* 将"最新"标签与日期/大小放在同一行，使用水平布局，避免改变高度

* 改为单行紧凑布局：左侧为日期+最新标签（水平排列），右侧为文件大小

* 日期字号从 14 调整为 13，减轻视觉重量

* 文件大小使用 `TextMutedBrush` 而非 Opacity

* 整体高度由 MinHeight=44 降低为更紧凑的 36

**修改后结构示意**：

```
+--------------------------------------------------+
| 2026-06-09 10:45:51  [最新]          23.71 KB    |
+--------------------------------------------------+
```

### 2. 修改 `AppDialogStyles.xaml` — `DialogBackupListBoxItemStyle`

**位置**：第 129\~177 行
**修改内容**：

* 背景：从硬编码 `#F2F2F2` 改为 `{DynamicResource CardBrush}`（浅色下白色，深色下 `#242424`）

* 前景：从硬编码 `#333333` 改为 `{DynamicResource TextPrimaryBrush}`

* Hover 背景：从 `#E8E8E8` 改为 `{DynamicResource HoverBrush}`

* Hover 前景：改为 `{DynamicResource TextPrimaryBrush}`

* 选中背景：从 `#555555` 改为 `{DynamicResource PrimarySubtleBrush}`（主题主色浅底）

* 选中前景：从 `#FFFFFF` 改为 `{DynamicResource PrimaryBrush}`（主题主色文字）

* 选中+Hover 背景：从 `#666666` 改为 `{DynamicResource PrimarySubtleBrush}`，前景保持主色

* 左侧指示条：保持 `PrimaryBrush`，但增加圆角和微调 Margin

* 内边距：从 `12,10` 调整为 `10,8` 更紧凑

* Margin：从 `0,3` 调整为 `0,2` 更紧凑

### 3. 修改 `MainWindow.Integration.cs` — `ShowBackupRecoveryDialog()`

**位置**：第 250\~261 行附近
**修改内容**：

* `ListBox` 高度从 320 减小到 260（更紧凑，仍可容纳约 5\~6 项）

* 对话框宽度从 480 减小到 420（第 321 行 `DialogService.ShowCustomDialog` 的最后一个参数）

* 移除 `Padding = new Thickness(4, 2, 4, 2)` 或设为更小的值

## 假设与决策

* 假设用户希望备份列表保持卡片式外观，只是配色和尺寸需要调整。

* 决定：选中状态使用主色系的 subtle 背景（`PrimarySubtleBrush`）+ 主色文字（`PrimaryBrush`），而非当前深灰反白，这样更现代且与主题一致。

* 决定："最新"标签使用 pill 形状（`RadiusPill`）而非小圆角，与 Win11 风格一致。

* 决定：移除文件名后，日期行加粗改为 SemiBold，整体视觉层次仍然清晰。

## 验证步骤

1. 编译项目，确认无 XAML 解析错误。
2. 运行应用，切换到系统托盘菜单的"恢复备份"。
3. 检查浅色主题下：

   * 列表项背景为白色卡片，悬停为浅灰，选中为浅橙色底+橙色字。

   * "最新"标签为橙色 pill，不增加项高度。

   * 无文件名显示。
4. 检查深色主题下：

   * 列表项背景为深灰卡片，悬停为稍浅深灰，选中为深橙底+橙色字。

   * 整体配色协调，无硬编码颜色导致的突兀感。
5. 确认对话框宽度约 420，ListBox 高度约 260，整体更紧凑。

