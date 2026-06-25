# 优化滑块控件为现代 Win11 风格

## Summary

将 `OpacitySettingsView.xaml` 中的原生 WPF Slider 替换为符合现代 Windows 11 视觉风格的自定义样式，同时保持所有现有功能（数据绑定、范围、步长、实时预览）不变。

## Current State Analysis

* **OpacitySettingsView\.xaml** 使用原生 `Slider`，带有 `TickPlacement="BottomRight"` 和刻度线，视觉上简陋，与项目其他现代化控件（Button、TextBox、CheckBox、ScrollBar 等）风格不统一。

* **ModernStyles.xaml** 已包含大量现代化控件样式，但缺少 Slider 的自定义样式。

* **主题资源** 已定义丰富的 Brush（PrimaryBrush、BorderBrush、BackgroundLightBrush 等）和圆角常量（RadiusControl=4、RadiusPill=14）。

* **OpacitySettingsViewModel.cs** 仅暴露 `BackgroundOpacity` 和 `ContentOpacity`，功能简单，无需改动。

## Proposed Changes

### 1. ModernStyles.xaml — 新增 ModernSliderStyle

**文件**: `e:\Working\todoapp\ToDoapp\Resources\ModernStyles.xaml`

在 `</ResourceDictionary>` 之前新增一个 `Style`（x:Key="ModernSliderStyle"，TargetType="Slider"）：

* **ControlTemplate 结构**:

  * 外层 `Grid` 垂直居中，高度 20（给 Thumb 留出空间）。

  * **轨道背景（未填充部分）**: `Border` 高度 4，圆角 `RadiusPill`，背景 `BorderBrush`（或 `BackgroundLightBrush`）。

  * **轨道填充（已选部分）**: 使用 `Rectangle` 绑定到 `PART_SelectionRange`，背景 `PrimaryBrush`，圆角左半部分（通过两个重叠的 Rectangle 或 Border 实现左右圆角）。

  * **Thumb**: 圆形 `Ellipse` 或圆角 `Border`，直径 16，默认背景 `TextPrimaryBrush`，悬停放大并显示 `PrimaryBrush` 外圈或填充，按下缩小。

  * **禁用状态**: 整体 `Opacity=0.4`。

  * **过渡动画**: 为 Thumb 的 `RenderTransform` 添加 `ScaleTransform`，配合 `DoubleAnimation` 实现悬停/按下的缩放动画（或在 Trigger 中直接设置）。

* **WPF 最佳实践**:

  * 使用 `PART_Track` 命名 Track 控件，WPF Slider 需要此名称来定位。

  * 保留 Slider 的标准行为：`IsMoveToPointEnabled`、`SmallChange`、`LargeChange` 等由控件自身处理，无需模板额外逻辑。

  * 使用 `DynamicResource` 引用颜色，确保主题切换生效。

  * 移除刻度线相关元素（TickBar），Win11 风格 Slider 通常不显示刻度。

### 2. OpacitySettingsView\.xaml — 应用新样式并精简属性

**文件**: `e:\Working\todoapp\ToDoapp\Views\Settings\Appearance\OpacitySettingsView.xaml`

对两个 `Slider` 做如下修改：

* 添加 `Style="{StaticResource ModernSliderStyle}"`。

* 移除 `TickPlacement="BottomRight"`（Win11 风格不需要刻度线，但吸附功能保留）。

* 保留 `TickFrequency="0.1"` 与 `IsSnapToTickEnabled="True"`，确保滑块值始终按 0.1 步长吸附。

* 移除固定 `Width="300"`，让其真正 `HorizontalAlignment="Stretch"` 填满 Grid 列（当前 Grid 第一列是 `*`，但固定宽度限制了响应式布局）。

* 保留：`Minimum="0.2"`, `Maximum="1.0"`, `Value` 绑定, `SmallChange="0.1"`, `LargeChange="0.1"`, `IsMoveToPointEnabled="True"`。

* 保留百分比 `TextBlock` 显示。

### 3. ViewModel — 无需改动

**文件**: `e:\Working\todoapp\ToDoapp\ViewModels\Settings\Appearance\OpacitySettingsViewModel.cs`

* 不做任何修改，确保功能完全不变。

## Assumptions & Decisions

1. **Win11 风格定义**: 采用细轨道（4px）+ 圆形 Thumb（16px）+ 主题色填充，这是 WinUI 3 / Windows 11 设置应用的典型 Slider 外观。
2. **移除刻度线**: 用户截图中刻度线显得杂乱，Win11 原生设置中 Slider 基本不带刻度线。
3. **固定宽度移除**: 让 Slider 随窗口自适应是 WPF 布局最佳实践，固定宽度在设置页面中不必要。
4. **颜色复用**: 直接使用现有主题资源（PrimaryBrush、BorderBrush、TextPrimaryBrush、HoverBrush 等），不引入新颜色。
5. **不添加新文件**: 样式直接写入 ModernStyles.xaml，与项目现有控件样式组织方式一致。

## Verification Steps

1. 编译项目成功。
2. 打开设置 > 外观 > 透明度设置，两个滑块应显示为现代风格：

   * 轨道为细圆角条，左侧为主题色填充，右侧为灰色。

   * Thumb 为圆形，悬停时有放大/颜色变化效果。

   * 无刻度线。
3. 拖动滑块，百分比文本实时更新，小组件透明度实时变化（与修改前行为一致）。
4. 验证值范围仍为 0.2 \~ 1.0，步长仍为 0.1。

