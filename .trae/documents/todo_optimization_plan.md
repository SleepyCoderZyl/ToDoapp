# 待办便签应用 - 功能优化实施计划

## 📋 代码Review总结

### 当前功能

✅ 基础待办事项管理（增删改查）
✅ 待办/已完成双标签页
✅ 小组件模式
✅ 全局快捷键（Ctrl+Alt+Q）
✅ 系统托盘
✅ AI智能解析待办
✅ 截止日期提醒

### 发现的问题

1. **删除即永久** - 没有垃圾箱，误删无法恢复
2. **已完成任务堆积** - 已完成的任务一直显示，占用空间但不想删除
3. **批量操作缺失** - 无法批量完成/删除任务

***

## 🎯 优化功能列表（按优先级排序）

### \[ ] P0 - 垃圾箱/回收站功能（核心需求）

* **Priority**: P0

* **Description**:

  * 在TodoItem中添加`IsDeleted`和`DeletedDate`属性

  * 在主页面增加"垃圾箱"标签页

  * 删除任务时移至垃圾箱而非永久删除

  * 支持从垃圾箱恢复任务

  * 支持永久删除单个任务

  * 支持清空整个垃圾箱

  * 垃圾箱内的任务七天后自动清理（显示剩余时间）

* **Success Criteria**:

  * 用户可以安全删除任务并随时恢复

  * 垃圾箱有独立的显示区域

* **Test Requirements**:

  * `programmatic` TR1.1: 删除任务后出现在垃圾箱

  * `programmatic` TR1.2: 从垃圾箱恢复任务回到待办/已完成列表

  * `programmatic` TR1.3: 可以永久删除和清空垃圾箱

  * `human-judgement` TR1.4: UI显示清晰，操作流程直观

### \[ ] P0 - 归档功能（解决已完成任务堆积）

* **Priority**: P0

* **Description**:

  * 在TodoItem中添加`IsArchived`和`ArchivedDate`属性

  * 在主页面增加"归档"标签页

  * 支持将已完成任务手动归档

  * 支持从归档恢复任务

  * 自动归档（比如完成后7天自动归档）

* **Success Criteria**:

  * 已完成任务可以归档，保持界面整洁

  * 归档的任务可以随时恢复

* **Test Requirements**:

  * `programmatic` TR2.1: 可以归档/取消归档任务

  * `programmatic` TR2.2: 归档的任务在归档标签页显示

  * `human-judgement` TR2.3: 归档操作简单直观

### \[ ] P1 - 批量操作

* **Priority**: P1

* **Depends On**: None

* **Description**:

  * 支持多选任务（复选框或Ctrl+点击）

  * 批量标记完成/未完成

  * 批量移至垃圾箱

  * 批量归档

* **Success Criteria**:

  * 可以高效处理多个任务

* **Test Requirements**:

  * `programmatic` TR3.1: 可以多选任务

  * `programmatic` TR3.2: 批量操作功能正常

***

## 📝 实施计划

**第一阶段**: 垃圾箱功能 + 归档功能（P0）
**第二阶段**: 批量操作（P1）

***

现在开始实施吗？先从垃圾箱和归档功能开始！
