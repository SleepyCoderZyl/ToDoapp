# 全局快捷功能重写 - 实现计划

## [ ] 任务 1：创建 QuickAddWindow.xaml 界面
- **Priority**: P0
- **Depends On**: None
- **Description**: 
  - 创建缺失的 QuickAddWindow.xaml 文件
  - 设计现代化的快速添加窗口界面
  - 窗口定位在屏幕中下方
  - 包含输入框和智能解析预览区域
  - 采用现有项目的 ModernStyles 样式
- **Success Criteria**:
  - QuickAddWindow.xaml 文件成功创建
  - 界面布局美观，符合项目风格
  - 窗口可以在屏幕中下方正确显示
- **Test Requirements**:
  - `programmatic` TR-1.1: QuickAddWindow.xaml 文件存在且编译通过
  - `human-judgement` TR-1.2: 界面布局清晰，输入框和预览区域功能分明
- **Notes**: 参考现有 SettingsWindow 和 WidgetWindow 的设计风格

---

## [ ] 任务 2：更新 AppSettings 模型
- **Priority**: P0
- **Depends On**: None
- **Description**: 
  - 在 AppSettings 中添加快捷键相关配置
  - 包含修饰键（Ctrl/Shift/Alt）和主键
  - 默认值设置为 Ctrl+Shift+Alt+Z
- **Success Criteria**:
  - AppSettings 模型已更新
  - 快捷键配置可以正确序列化和反序列化
- **Test Requirements**:
  - `programmatic` TR-2.1: AppSettings 包含 HotKeyModifiers 和 HotKeyKey 属性
  - `programmatic` TR-2.2: 默认值设置为 Ctrl+Shift+Alt+Z
- **Notes**: 使用 JSON 属性名称保持兼容性

---

## [ ] 任务 3：增强并清理 GlobalHotKeyService
- **Priority**: P0
- **Depends On**: 任务 2
- **Description**: 
  - 删除 SimulateCopy 方法及相关代码
  - 删除 VK_CONTROL、VK_C、KEYEVENTF_KEYUP 等不需要的常量
  - 添加支持实时更改快捷键的功能
  - 添加获取当前快捷键的方法
  - 支持从 AppSettings 读取和注册快捷键
  - 改进错误处理
- **Success Criteria**:
  - 可以动态更改和重新注册快捷键
  - 快捷键配置从设置中读取
  - 原复制功能代码已完全移除
- **Test Requirements**:
  - `programmatic` TR-3.1: RegisterHotKey 支持动态重新注册
  - `programmatic` TR-3.2: 添加 GetHotKeyDisplayText 方法返回友好显示文本
  - `programmatic` TR-3.3: SimulateCopy 方法及相关常量已删除
- **Notes**: 保持向后兼容性

---

## [ ] 任务 4：更新 QuickAddWindow.xaml.cs 逻辑
- **Priority**: P0
- **Depends On**: 任务 1
- **Description**: 
  - 修改为直接回车确认（移除 Ctrl 键要求）
  - 实现窗口在屏幕中下方定位
  - 改进智能解析实时预览
  - 添加 ESC 键关闭窗口
  - 优化剪贴板读取逻辑
- **Success Criteria**:
  - 快速添加窗口功能完善
  - 回车直接确认添加
  - 智能预览实时更新
- **Test Requirements**:
  - `programmatic` TR-4.1: 按下 Enter 键可以直接添加待办
  - `programmatic` TR-4.2: 按下 Escape 键关闭窗口
  - `human-judgement` TR-4.3: 窗口在屏幕中下方显示
- **Notes**: 使用 SystemParameters 获取屏幕尺寸

---

## [ ] 任务 5：更新 SettingsViewModel 的快捷键设置界面
- **Priority**: P1
- **Depends On**: 任务 2, 3
- **Description**: 
  - 创建快捷键输入控件，支持录制用户输入的快捷键
  - 实时更新显示当前快捷键
  - 添加应用和重置按钮
  - 实时注册新快捷键
- **Success Criteria**:
  - 用户可以在设置界面自定义快捷键
  - 快捷键更改立即生效
- **Test Requirements**:
  - `programmatic` TR-5.1: 快捷键输入控件可以捕获键盘输入
  - `programmatic` TR-5.2: 快捷键更改后立即重新注册
  - `human-judgement` TR-5.3: 设置界面直观易用
- **Notes**: 防止冲突的快捷键组合

---

## [ ] 任务 6：更新 MainWindow 集成逻辑
- **Priority**: P0
- **Depends On**: 任务 1, 2, 3, 4
- **Description**: 
  - 修改 InitializeGlobalHotKey 使用 AppSettings 配置
  - 修改 OnGlobalHotKeyPressed 打开 QuickAddWindow 而不是直接添加
  - 传递必要的服务引用给 QuickAddWindow
  - 更新默认快捷键为 Ctrl+Shift+Alt+Z
- **Success Criteria**:
  - 全局快捷键触发时显示快速添加窗口
  - 快捷键从设置读取
- **Test Requirements**:
  - `programmatic` TR-6.1: 默认快捷键注册为 Ctrl+Shift+Alt+Z
  - `programmatic` TR-6.2: 按下快捷键打开 QuickAddWindow
  - `human-judgement` TR-6.3: 从 QuickAddWindow 添加的待办正确保存
- **Notes**: 确保窗口正确获取焦点

---

## [ ] 任务 7：编译和测试
- **Priority**: P0
- **Depends On**: 所有任务
- **Description**: 
  - 编译项目确保没有错误
  - 测试所有功能：
    - 默认快捷键 Ctrl+Shift+Alt+Z
    - 快速添加窗口显示位置
    - 智能解析实时预览
    - 回车确认和 ESC 关闭
    - 设置界面更改快捷键
    - 快捷键实时生效
- **Success Criteria**:
  - 项目编译成功
  - 所有功能正常工作
- **Test Requirements**:
  - `programmatic` TR-7.1: 项目编译无错误无警告
  - `programmatic` TR-7.2: 所有测试场景功能正常
- **Notes**: 进行全面测试，确保没有回归
