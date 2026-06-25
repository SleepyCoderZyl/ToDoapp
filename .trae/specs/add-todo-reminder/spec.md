# 待办定时提醒功能 Spec

## Why
当前待办仅支持"日期"维度（`DueDate`），无法表达"明天下午 3 点开会"、"10 点前提交材料"这类带具体时点与"提前 N 分钟"提醒诉求；用户希望在不破坏现有日期输入与导入导出兼容性的前提下，扩展为支持时间点 + 提前量提醒，并把弹窗体验统一复用设置里的"提醒弹窗"组件。

## What Changes
- `TodoItem` 新增 `DueTime`（`TimeOnly?`）与 `ReminderOffsetMinutes`（`int?`）字段，扩展 `HasReminder` 语义：开启提醒时按 `DueDate + DueTime - ReminderOffsetMinutes` 触发。
- `TodoStorageItem` 同步增加对应可空字段（`dueTime`/`reminderOffsetMinutes`/`lastReminderShownAt`），保证旧版 JSON 文件可正常反序列化、导出文件结构向后兼容。
- `SmartTodoParser` 新增"时分"与"提前 N"规则：
  - 解析 `HH:mm`、`H点` / `H点半` / `H点M分`、`上午/下午/晚上/早上/中午/傍晚 H(点)` 等中文时间表达；
  - 解析 `提前N分钟/半小时/一刻钟/N小时` 作为提前量；
  - 解析结果 `ParsedTodoResult` 增加 `DueTime`、`ReminderOffsetMinutes`、对应 `*SourceHint`。
- 新增 `TodoReminderService`，由 `MainTimer_Tick` 调用：
  - 计算 `TriggerTime = DueDate.Date + DueTime - ReminderOffsetMinutes`；
  - 触发后通过 `Dispatcher` 在 UI 线程上弹出复用 `StartupReminderWindow`（扩展 `ReminderSnapshot` 支持待办来源文案），并将 `LastReminderShownAt` 持久化，避免同一任务在同一次触发窗口内重复弹出；
  - 已完成 / 已删除 / 已归档的任务不参与提醒。
- 顶部输入框（`MainWindow` `NewTaskTextBox`、`QuickAddWindow` `InputTextBox`）增加"解析预览"，显示时间、提前量等解析结果（复用现有解析预览样式与位置，新增一行提示）。
- `EditTask_Click` 自定义弹窗中增加"具体时间"与"提前提醒分钟数"两个输入控件（`TimePicker` 由现有 `CalendarPopup`/`TextBox` + `ComboBox` 组合实现，分钟数走下拉/数字框）。

## Impact
- Affected specs:
  - 待办数据持久化与导入导出（`ToDoapp/Services/TodoService.cs`、`ToDoapp/Models/TodoStorageItem.cs`）
  - 智能解析（`ToDoapp/Services/SmartTodoParser.cs`）
  - 提醒服务（`ToDoapp/Services/StartupReminderService.cs`、新增 `TodoReminderService.cs`）
  - 主窗口 / 快速添加窗口 / 编辑弹窗（`ToDoapp/Views/MainWindow.*.cs`、`ToDoapp/Views/QuickAddWindow.*`）
- Affected code:
  - `ToDoapp/Models/TodoItem.cs`
  - `ToDoapp/Models/TodoStorageItem.cs`
  - `ToDoapp/Models/AppConstants.cs`（如有常量新增）
  - `ToDoapp/Services/SmartTodoParser.cs`
  - `ToDoapp/Services/TodoReminderService.cs`（新增）
  - `ToDoapp/Services/StartupReminderService.cs`（扩展 `ReminderSnapshot` 与构建逻辑）
  - `ToDoapp/Views/StartupReminderWindow.xaml`（允许显示"待办来源"附加行）
  - `ToDoapp/Views/MainWindow.xaml` / `MainWindow.Tasks.cs` / `MainWindow.Integration.cs`（解析预览 + 编辑弹窗增强 + 周期检查）
  - `ToDoapp/Views/QuickAddWindow.xaml` / `QuickAddWindow.xaml.cs`（解析预览扩展）
  - `ToDoapp.Tests/SmartTodoParserTests.cs`（新增时间/提前量解析用例）
  - `ToDoapp.Tests/TodoItemTests.cs` / `TodoServiceTests.cs`（新增新字段序列化与导入导出兼容用例）
  - `ToDoapp.Tests/TodoReminderServiceTests.cs`（新增触发判定与"已提醒去重"用例）

## ADDED Requirements

### Requirement: 待办支持时间与提前提醒
系统 SHALL 在 `TodoItem` 上支持"具体时间（`DueTime`）"和"提前提醒分钟数（`ReminderOffsetMinutes`）"两个可空字段；当 `HasReminder` 为真且 `DueDate`/`DueTime` 均存在时，按 `TriggerTime = DueDate.Date + DueTime - ReminderOffsetMinutes` 计算提醒触发时刻；任何字段缺失时，相应计算回退到按 `DueDate` 当天 23:59 触发或保留原行为。

#### Scenario: 用户输入带时间的待办
- **WHEN** 用户在主窗口输入"明天下午 3 点提交周报"
- **THEN** 解析得到 `DueDate = 明天`、`DueTime = 15:00`、`ReminderOffsetMinutes = null`，创建后 `HasReminder = true`，状态栏提示包含时间。

#### Scenario: 用户输入带提前量的待办
- **WHEN** 用户输入"明早 9 点开周会，提前 10 分钟提醒"
- **THEN** 解析得到 `DueDate = 明天`、`DueTime = 09:00`、`ReminderOffsetMinutes = 10`，创建后 `HasReminder = true`，UI 提示中显示"09:00（提前 10 分钟）"。

#### Scenario: 旧版数据导入
- **WHEN** 加载缺少 `dueTime` / `reminderOffsetMinutes` / `lastReminderShownAt` 字段的旧版 `todos.json`
- **THEN** 对应字段保持 `null`，`HasReminder` 沿用旧值，不抛异常。

### Requirement: 输入框智能解析时间与提前量
`SmartTodoParser` SHALL 解析以下中文时间表达并填充 `ParsedTodoResult.DueTime`：`HH:mm` / `H点` / `H点半` / `H点M分` / `H时M分`、以及 `上午/下午/晚上/早上/今早/明早/中午/傍晚/凌晨 H点` 系列；同时 SHALL 解析 `提前N分钟/半小时/一刻钟/N小时/N天` 形式填入 `ReminderOffsetMinutes`，并通过 `DateSourceHint` / `TimeSourceHint` 暴露命中片段以便 UI 预览。

#### Scenario: 解析 "10点半 开会"
- **WHEN** 调用 `SmartTodoParser.Parse("10点半 开会")`
- **THEN** `DueTime = 10:30`，`Title = "开会"`，`TimeSourceHint` 含"时段"或"24小时"等。

#### Scenario: 解析 "下午 3:30 提交"
- **WHEN** 调用 `SmartTodoParser.Parse("下午 3:30 提交")`
- **THEN** `DueTime = 15:30`，`Title = "提交"`。

#### Scenario: 解析 "明天 9 点 提前 5 分钟 提醒"
- **WHEN** 调用 `SmartTodoParser.Parse("明天 9 点 提前 5 分钟 提醒")`
- **THEN** `DueDate = 明天日期`、`DueTime = 09:00`、`ReminderOffsetMinutes = 5`，`Title` 不再包含时间与提前量提示文字。

### Requirement: 复用弹窗展示待办提醒
`TodoReminderService` SHALL 在 `MainWindow` 主定时器（`MainTimer_Tick`）驱动下扫描所有未完成、未删除、未归档且 `HasReminder` 的待办；当 `now` 达到 `TriggerTime` 且 `LastReminderShownAt` 为空或早于 `TriggerTime` 时，SHALL 通过 `MainWindow.Dispatcher` 在 UI 线程上弹出 `StartupReminderWindow`，传入选中任务的 `Title` 与 `DueDate/DueTime/ReminderOffset` 等摘要；弹窗关闭或点击"知道了"后，将 `LastReminderShownAt` 写入 `TodoStorageItem` 并保存。

#### Scenario: 到达触发时刻
- **WHEN** 当前时间达到某提醒待办的 `TriggerTime`
- **THEN** 弹出弹窗，标题显示"待办提醒：<Title>"，副标题包含"截止 <DueDate> <DueTime>（提前 N 分钟）"，按钮复用"知道了/打开主窗口"。

#### Scenario: 同一触发窗口内去重
- **WHEN** 同一任务的弹窗在 `TriggerTime` 同分钟窗口内已弹出过
- **THEN** 不再重复弹出，且 `LastReminderShownAt` 已被更新至本次触发时间。

#### Scenario: 已完成任务不再提醒
- **WHEN** 任务 `IsCompleted` 或 `IsDeleted` 或 `IsArchived` 为真
- **THEN** `TodoReminderService` 跳过该任务，不弹窗、不更新 `LastReminderShownAt`。

### Requirement: 导入导出与存储兼容
`TodoStorageItem` SHALL 以可空方式持久化新字段（`dueTime` 存为 `HH:mm:ss` 字符串，`reminderOffsetMinutes` 与 `lastReminderShownAt` 存为可空数字/ISO8601 字符串）；反序列化时缺失字段 SHALL 视为 `null`；导出文件 SHALL 包含新字段；加载旧版（仅含 `dueDate` 等老字段）的文件 SHALL 正常返回结果不抛异常。

#### Scenario: 导出含提醒字段的待办
- **WHEN** 用户导出包含 `DueTime = 18:00` 与 `ReminderOffsetMinutes = 15` 的任务
- **THEN** 导出 JSON 含 `"dueTime": "18:00:00"` 与 `"reminderOffsetMinutes": 15` 字段。

#### Scenario: 导入旧版 JSON
- **WHEN** 导入仅含 `title/dueDate/hasReminder` 等老字段的 JSON
- **THEN** `TodoService.LoadTodosFromFile` 成功返回，新字段均为 `null`。

#### Scenario: 备份恢复
- **WHEN** 从旧版生成的备份恢复数据
- **THEN** 加载、提示文案与现状一致，行为不回退、不抛异常。

### Requirement: 编辑弹窗支持时间与提前量
`MainWindow.EditTask_Click` 构建的自定义编辑面板 SHALL 增加"具体时间"（24 小时制 `TextBox`，格式 `HH:mm`）和"提前提醒分钟数"（`ComboBox` 提供 `不提醒/准时/5/10/15/30/60` 分钟等常见值）两个控件，保存时校验时间格式并写回 `TodoItem.DueTime` / `ReminderOffsetMinutes`；校验失败时弹窗保持打开并显示行内错误，不关闭。

#### Scenario: 编辑任务修改时间
- **WHEN** 用户在编辑弹窗中将时间改为 `14:30`、提前量改为 `15` 分钟
- **THEN** 保存后 `TodoItem.DueTime = 14:30`、`ReminderOffsetMinutes = 15`，状态栏提示"已修改"。

#### Scenario: 输入非法时间
- **WHEN** 用户输入 `25:99` 之类非法时间
- **THEN** 行内显示错误文案"时间格式应为 HH:mm"，主按钮保持不可用直到修正或点击取消。

## MODIFIED Requirements
无（现有 `HasReminder` / `DueDate` / 解析规则保持兼容，仅在其之上扩展）。

## REMOVED Requirements
无。
