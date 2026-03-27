# 小组件透明度分离调节 - 实施计划

## [ ] 任务 1: 在 AppSettings 中添加 ContentOpacity 配置项
- **Priority**: P0
- **Depends On**: None
- **Description**: 
  - 在 AppSettings.cs 中添加 ContentOpacity 属性
  - 默认值设为 1.0（完全不透明）
  - 使用 JsonPropertyName 序列化
- **Success Criteria**:
  - 配置项正确添加，默认值合理
- **Test Requirements**:
  - `programmatic` TR-1.1: 编译通过，配置项存在
- **Notes**: 保持向后兼容

## [ ] 任务 2: 在 WidgetOpacityManager 中添加 ContentOpacity 属性
- **Priority**: P0
- **Depends On**: Task 1
- **Description**: 
  - 添加 ContentOpacity 私有字段和公共属性
  - 添加 ContentOpacityChanged 事件
  - 添加 EffectiveContentOpacity 计算属性
  - 在构造函数中从 SettingsService 读取值
  - 更新 SettingsService.UpdateWidgetOpacity 方法
- **Success Criteria**:
  - 属性变更时触发事件
  - 值范围限制在 0.2-1.0
- **Test Requirements**:
  - `programmatic` TR-2.1: 属性变更时事件被触发
  - `programmatic` TR-2.2: 值限制在 0.2-1.0 范围内

## [ ] 任务 3: 修改 WidgetWindow.xaml 分别控制背景和内容透明度
- **Priority**: P0
- **Depends On**: Task 2
- **Description**: 
  - 为 MainBorder 绑定背景透明度
  - 为内容区域绑定内容透明度
  - 修改 WidgetWindow.xaml.cs 订阅 ContentOpacityChanged
- **Success Criteria**:
  - 背景和内容透明度可以独立控制
- **Test Requirements**:
  - `human-judgment` TR-3.1: 调节背景透明度时内容不受影响
  - `human-judgment` TR-3.2: 调节内容透明度时背景不受影响

## [ ] 任务 4: 修改设置界面，添加两个独立的滑块控件
- **Priority**: P0
- **Depends On**: Task 3
- **Description**: 
  - 修改 CreateOpacitySettingContent 方法
  - 添加背景透明度滑块
  - 添加内容透明度滑块
  - 添加对应的百分比文本显示
  - 更新描述文本
- **Success Criteria**:
  - 设置界面有两个独立的滑块控件
- **Test Requirements**:
  - `human-judgment` TR-4.1: 设置界面显示两个滑块
  - `human-judgment` TR-4.2: 滑块值改变时实时更新
  - `programmatic` TR-4.3: 编译通过

## [ ] 任务 5: 修改 WidgetView 以支持内容透明度控制
- **Priority**: P0
- **Depends On**: Task 4
- **Description**: 
  - 确保 WidgetView 的内容正确应用透明度
  - 更新相关的 ContentOpacity 绑定
- **Success Criteria**:
  - WidgetView 的内容透明度正确应用
- **Test Requirements**:
  - `human-judgment` TR-5.1: 小组件内的文字透明度正确调节

## [ ] 任务 6: 测试和编译项目
- **Priority**: P1
- **Depends On**: Task 5
- **Description**: 
  - 完整编译项目
  - 测试各功能
  - 确保无回归
- **Success Criteria**:
  - 编译无警告无错误
  - 所有功能正常
- **Test Requirements**:
  - `programmatic` TR-6.1: 编译成功，无错误
  - `programmatic` TR-6.2: Release 模式编译通过
