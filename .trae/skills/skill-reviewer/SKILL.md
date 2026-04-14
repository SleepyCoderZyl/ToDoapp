---
name: "skill-reviewer"
description: "Review and optimize SKILL.md files for quality, completeness, and best practices. Invoke when user asks to review a skill, check skill quality, or improve skill documentation."
---

# Skill Reviewer

审查和优化 SKILL.md 文件，确保技能文件符合规范、内容完整且易于使用。

## 审查维度

### 1. 格式规范 (CRITICAL)

**Frontmatter 检查：**
- [ ] 包含 `name` 字段
- [ ] 包含 `description` 字段
- [ ] description 说明技能和调用时机
- [ ] 使用正确的 YAML 格式

**文件结构：**
```yaml
---
name: "skill-name"
description: "技能描述。Invoke when..."
---

# 标题

内容...
```

### 2. 内容完整性 (HIGH)

**必须包含：**
- [ ] 技能用途说明
- [ ] 调用时机说明
- [ ] 使用示例
- [ ] 输入/输出格式
- [ ] 常见用例

**可选但推荐：**
- [ ] 参数说明
- [ ] 错误处理
- [ ] 注意事项
- [ ] 相关技能链接

### 3. 描述质量 (HIGH)

**description 字段要求：**
- 长度：50-200字符
- 包含：技能功能 + 调用时机
- 格式：`"功能描述。Invoke when..."`

**示例：**
```yaml
# 好的描述
description: "Reviews code for security and performance issues. Invoke when user asks for code review or before merging changes."

# 差的描述
description: "Code reviewer"  # 太简短，缺少调用时机
```

### 4. 可读性 (MEDIUM)

- [ ] 使用清晰的标题层级
- [ ] 使用列表和表格组织信息
- [ ] 包含代码示例
- [ ] 使用中文或英文（保持一致）
- [ ] 避免过长段落

### 5. 实用性 (MEDIUM)

- [ ] 示例可直接使用
- [ ] 覆盖常见场景
- [ ] 避免过于抽象
- [ ] 提供默认值/推荐值

## 审查清单

### SKILL.md 文件检查表

```markdown
## 格式检查
- [ ] Frontmatter 包含 name 和 description
- [ ] description 包含功能说明和调用时机
- [ ] 使用正确的分隔符 ---

## 内容检查
- [ ] 有清晰的标题
- [ ] 有使用说明
- [ ] 有示例代码/命令
- [ ] 有输入输出说明

## 质量检查
- [ ] 描述简洁明了
- [ ] 示例可运行
- [ ] 无拼写错误
- [ ] 格式统一

## 完整性检查
- [ ] 覆盖主要使用场景
- [ ] 包含错误处理（如需要）
- [ ] 包含参数说明（如需要）
```

## 常见问题和修复

### 问题1：description 太简短

**修复前：**
```yaml
description: "Git workflow skill"
```

**修复后：**
```yaml
description: "Provides Git workflow best practices for personal development. Invoke when user asks about git workflow, commit conventions, or branch strategy."
```

### 问题2：缺少调用时机

**修复前：**
```yaml
description: "Reviews code for bugs and issues"
```

**修复后：**
```yaml
description: "Reviews code for bugs and issues. Invoke when user asks for code review or before merging changes."
```

### 问题3：格式不统一

**修复前：**
```markdown
##使用示例
git commit -m "message"
```

**修复后：**
```markdown
## 使用示例

```bash
git commit -m "message"
```
```

### 问题4：示例不完整

**修复前：**
```markdown
使用 git switch 切换分支
```

**修复后：**
```markdown
### 切换分支

```bash
# 切换到现有分支
git switch master

# 创建并切换分支
git switch -c feature-branch
```
```

## 审查输出格式

```markdown
## Skill Review Report: [skill-name]

### 总体评价
[简要评价技能质量]

### ✅ 优点
1. [优点1]
2. [优点2]

### 🔧 需要改进

#### Critical
- [ ] [关键问题及修复建议]

#### High
- [ ] [重要问题及修复建议]

#### Medium
- [ ] [一般建议]

### 具体建议

#### 1. [问题类别]
**位置：** [行号或章节]
**问题：** [描述]
**建议：** [修复方案]
**示例：**
```
[修复后的代码示例]
```

### 评分
- 格式规范：⭐⭐⭐⭐⭐
- 内容完整：⭐⭐⭐⭐☆
- 描述质量：⭐⭐⭐⭐⭐
- 可读性：⭐⭐⭐⭐☆
- 实用性：⭐⭐⭐⭐⭐

**总分：** X/5.0

### 行动项
- [ ] [具体修复任务1]
- [ ] [具体修复任务2]
```

## 最佳实践

### Skill 设计原则

1. **单一职责** - 一个skill只做一件事
2. **明确边界** - 清晰说明什么情况下调用
3. **示例驱动** - 提供可运行的示例
4. **渐进式** - 从简单到复杂
5. **可扩展** - 预留扩展空间

### 内容组织建议

```markdown
# Skill Title

[简介：一句话说明技能用途]

## 何时使用

[调用时机和场景]

## 核心功能

### 功能1
[说明 + 示例]

### 功能2
[说明 + 示例]

## 使用示例

### 示例1：基本用法
```
[代码]
```

### 示例2：高级用法
```
[代码]
```

## 参数说明

| 参数 | 类型 | 必需 | 说明 |
|------|------|------|------|
| param1 | string | 是 | 说明 |

## 注意事项

- [注意点1]
- [注意点2]

## 相关技能

- [skill-name](path) - 说明
```

---

**提示**: 使用此skill审查其他skill文件时，请提供具体的改进建议和修复示例。
