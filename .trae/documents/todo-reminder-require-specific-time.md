# 计划：优化待办提醒触发条件

## 1. Summary（目标）

仅当待办同时具备 `DueDate` 与 `DueTime`（具体时间）时，才允许触发弹窗提醒；
无具体时间的待办（仅有日期 / 解析得到"全天"）一律不弹窗。

整个改造遵循 karpathy-guidelines：**不新增类型/方法/服务**，全部在已有调用链中做外科手术式收紧。

## 2. Current State Analysis（根因）

### 2.1 当前触发链

| 步骤             | 位置                                                                     | 现状                                                                             |
| -------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| 1. 计算触发时刻      | `ToDoapp/Models/TodoItem.cs:223` `GetReminderTriggerTime()`            | 当 `DueTime` 为 `null` 时**回退为当天 23:59**，导致"只选日期"的待办也会到点弹窗                        |
| 2. 周期扫描        | `ToDoapp/Services/TodoReminderService.cs:34-38`                        | `!trigger.HasValue` 时跳过；`trigger` 非空即进入候选                                      |
| 3. 弹窗          | `ToDoapp/Views/MainWindow.Integration.cs:138-181` `CheckTodoReminders` | 命中后弹 `StartupReminderWindow`                                                   |
| 4. 创建入口（日期选择器） | `ToDoapp/ViewModels/MainWindowViewModel.cs:165-169`                    | `selectedDueDate.HasValue` 分支直接 `HasReminder = true`，与 `DueTime` 无关            |
| 5. 创建入口（智能解析）  | `ToDoapp/ViewModels/MainWindowViewModel.cs:170-176`                    | `parsedResult.DueDate.HasValue` 时也直接 `HasReminder = true`，未校验 `DueTime`        |
| 6. 快速添加        | `ToDoapp/Views/QuickAddWindow.xaml.cs:148-154`                         | 与 (5) 同样的无条件 `HasReminder = true`                                              |
| 7. 编辑保存        | `ToDoapp/Views/MainWindow.Tasks.cs:291`                                | `selectedItem.HasReminder = selectedItem.DueDate.HasValue;` —— 把"有日期"等同于"开启提醒" |

### 2.2 旧行为与新需求差异

* 旧：只填日期 → 23:59 弹窗（"全天任务"语义）。

* 新：只填日期 → 不弹窗；必须显式填时间才会弹窗。

* 兼容性：旧版 JSON 中 `HasReminder=true` 但 `DueTime=null` 的待办，新逻辑下也不再弹窗（这是用户期望）。

## 3. Proposed Changes（手术式收紧，全部修改已有代码）

> 原则：让"提醒"在模型层就要求 `DueTime`，下游的 `TodoReminderService` 已经具备
> `if (!trigger.HasValue) continue;` 的现成短路逻辑，因此**不需要改动 Service**。

### 3.1 [ToDoapp/Models/TodoItem.cs](file:///e:/Working/todoapp/ToDoapp/Models/TodoItem.cs#L223-L233) — 收紧触发时刻计算

`GetReminderTriggerTime()` 当前的 `?? new TimeSpan(23, 59, 0)` 回退是问题的源头。
改为：**缺少** **`DueTime`** **直接返回** **`null`**。

```csharp
public DateTime? GetReminderTriggerTime()
{
    if (!HasReminder || !DueDate.HasValue || !DueTime.HasValue)
    {
        return null;
    }

    var dueAt = DueDate.Value.Date.Add(DueTime.Value.ToTimeSpan());
    var offset = ReminderOffsetMinutes ?? 0;
    return dueAt.AddMinutes(-offset);
}
```

* 影响：仅修改一处表达式条件，行为级联到 `TodoReminderService.Scan`（已有 `!trigger.HasValue` 短路）。

* 不动 `ReminderTimeDisplay` 的"全天"分支：保留兼容展示，仅影响"是否弹窗"。

### 3.2 [ToDoapp/ViewModels/MainWindowViewModel.cs](file:///e:/Working/todoapp/ToDoapp/ViewModels/MainWindowViewModel.cs#L165-L176) — 收紧创建入口

`AddSmartTask` 中两处 `todoItem.HasReminder = true;` 改为条件赋值：

```csharp
if (selectedDueDate.HasValue)
{
    todoItem.DueDate = selectedDueDate.Value;
    // 日期选择器不提供时间 → 不自动开启提醒，避免与"必须具体时间"规则冲突
}
else if (parsedResult.DueDate.HasValue)
{
    todoItem.DueDate = parsedResult.DueDate.Value;
    todoItem.DueTime = parsedResult.DueTime;
    todoItem.ReminderOffsetMinutes = parsedResult.ReminderOffsetMinutes;
    // 仅当解析得到具体时间时，才标记"开启提醒"
    todoItem.HasReminder = parsedResult.DueTime.HasValue;
}
```

* 日期选择器场景：用户既然没填时间，就当作"记录日期但不要弹窗"，与 3.1 行为一致。

* 智能解析场景：自然语言中包含时间才会触发提醒。

### 3.3 [ToDoapp/Views/QuickAddWindow.xaml.cs](file:///e:/Working/todoapp/ToDoapp/Views/QuickAddWindow.xaml.cs#L148-L154) — 同步收紧

```csharp
if (parsedResult.DueDate.HasValue)
{
    todoItem.DueDate = parsedResult.DueDate.Value;
    todoItem.DueTime = parsedResult.DueTime;
    todoItem.ReminderOffsetMinutes = parsedResult.ReminderOffsetMinutes;
    todoItem.HasReminder = parsedResult.DueTime.HasValue;
}
```

### 3.4 [ToDoapp/Views/MainWindow.Tasks.cs](file:///e:/Working/todoapp/ToDoapp/Views/MainWindow.Tasks.cs#L291) — 编辑弹窗同步

把"开启提醒"绑定到"日期 + 时间同时存在"：

```csharp
selectedItem.HasReminder = selectedItem.DueDate.HasValue && selectedItem.DueTime.HasValue;
```

### 3.5 测试同步（验证新行为）

#### [ToDoapp.Tests/TodoItemTests.cs](file:///e:/Working/todoapp/ToDoapp.Tests/TodoItemTests.cs#L114-L130) — `GetReminderTriggerTime_FullDayWithoutTime_DefaultsTo2359`

将原"23:59 回退"断言改为：

```csharp
[Fact]
public void GetReminderTriggerTime_WithoutDueTime_ReturnsNull()
{
    var todo = new TodoItem
    {
        Title = "全天任务",
        CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
        DueDate = new DateTime(2026, 5, 13),
        DueTime = null,
        ReminderOffsetMinutes = 0,
        HasReminder = true
    };

    Assert.Null(todo.GetReminderTriggerTime());
}
```

#### [ToDoapp.Tests/TodoReminderServiceTests.cs](file:///e:/Working/todoapp/ToDoapp.Tests/TodoReminderServiceTests.cs#L243-L258) — `Scan_FullDayWithoutTime_DefaultsToEndOfDay`

将原"匹配到当天 23:59"断言改为"无具体时间的待办被排除"：

```csharp
[Fact]
public void Scan_FullDayWithoutTime_ExcludesTodo()
{
    var service = new TodoReminderService();
    var todo = CreateTodo(
        title: "全天任务",
        dueDate: new DateTime(2026, 5, 13),
        dueTime: null,
        offsetMinutes: 0);
    var now = new DateTime(2026, 5, 13, 23, 59, 30);

    var matches = service.Scan(new[] { todo }, now);

    Assert.Empty(matches);
}
```

#### 保留并新增的对照测试

* 保留 `Scan_WhenTriggerTimeReached_IncludesTodo`（带时间的正向用例）。

* 保留 `GetReminderTriggerTime_WithDateAndTime_ComputesOffsetCorrectly`。

* 保留 `ReminderTimeDisplay_WithoutTime_FallsBackToAllDay`（展示层兼容，不影响触发）。

## 4. Assumptions & Decisions

1. **不做"未填时间时改为默认时间"的提示弹窗**——用户输入"明天下午 3 点"已能正确解析，无须额外引导。
2. **不修改** **`ReminderTimeDisplay`** **的"全天"分支**——历史数据展示兼容；后续可通过 UI 引导让用户显式补时间。
3. **不在** **`TodoReminderService`** **内追加条件**——上游 `GetReminderTriggerTime` 已能返回 `null`，Service 本身已有短路检查，重复条件会变成 dead code（违反 karpathy-guidelines §3）。
4. **不引入新属性 / 新方法 / 新类**——只调整现有 4 处赋值的判定条件。
5. **不影响 CheckOverdueTasks / IsOverdue 逻辑**——过期判定仍按 `DueDate`，与提醒触发解耦。

## 5. Verification（执行步骤）

按 `.trae/rules/wpf.md` 的工作流执行：

1. **静态检查**

   * 修改 4 个生产文件 + 2 个测试文件 → 复查无新代码（净行数变化控制在 ±10 行内）。
2. **代码评审**

   * 调起 `TRAE-code-review` skill，对 diff 进行正确性 / 可维护性 / 性能扫描。
3. **编译**

   * `dotnet build todo.sln -c Debug` → 期望 0 warning / 0 error。
4. **测试**

   * `dotnet test ToDoapp.Tests/ToDoapp.Tests.csproj --filter "FullyQualifiedName~TodoReminder|FullyQualifiedName~TodoItem"`

   * 关键断言：

     * `GetReminderTriggerTime_WithoutDueTime_ReturnsNull` 通过。

     * `Scan_FullDayWithoutTime_ExcludesTodo` 通过。

     * 原 `Scan_WhenTriggerTimeReached_IncludesTodo` 仍通过。
5. **冒烟（人工）**

   * 启动应用 → 添加"明天下午 3 点开会" → 状态栏显示时间、3 点前不弹窗。

   * 添加"明天"（仅日期，UI 不再有"全天"提醒） → 第二天 23:59 不弹窗。

## 6. Out of Scope（明确不做）

* 不调整 `SmartTodoParser` 解析规则（已经能从自然语言中提取时间）。

* 不改 `StartupReminderWindow` 文案 / 布局。

* 不调整 `MainTimer_Tick` 30s 周期（与"是否弹窗"无关）。

* 不增加新的提醒类型或配置项。

