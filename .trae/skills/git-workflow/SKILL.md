---
name: git-workflow
description: |
  提供个人开发的Git工作流规范和最佳实践。
  当用户询问Git工作流、提交规范、分支策略或需要Git操作指导时调用。
---

# Git工作流规范 (个人开发)

本skill提供针对个人开发者的Git工作流规范、提交信息标准和常用操作指南。

## 何时使用

- 用户询问Git工作流或分支策略
- 用户需要提交信息格式建议
- 用户需要Git命令操作指导
- 用户需要配置.gitignore
- 用户需要Git别名配置建议
- 用户遇到Git常见问题（冲突、撤销等）
- 需要根据项目类型自动适配Git配置

## 输入与输出

### 输入

- 用户问题或需求描述
- 项目类型（可选，用于适配建议）
- 当前Git操作场景（可选）

### 输出

- Git命令示例和解释
- 提交信息格式建议
- 分支策略指导
- 配置文件模板（.gitignore、别名等）
- 问题解决方案
- 相关文档链接

## 快速开始

### 提交信息规范

```bash
# 基本格式
<type>[optional scope]: <description>

# 示例
feat(Parser): 添加自然语言日期解析
fix(UI): 修复按钮点击无响应问题
docs: 更新README使用说明
```

**类型说明：**
| 类型 | 用途 |
|------|------|
| `feat` | 新功能 |
| `fix` | 修复bug |
| `refactor` | 重构代码 |
| `style` | 样式调整 |
| `docs` | 文档更新 |
| `test` | 添加测试 |
| `chore` | 杂项/配置 |
| `perf` | 性能优化 |

**⚠️ 注意：范围(Scope)是完全可选的！** 详见 [提交规范指南](guides/commit-guide.md)。

### 分支工作流程

```bash
# 1. 开始新功能
git switch master
git pull
git switch -c feature/smart-parser

# 2. 开发并提交
git add .
git commit -m "feat(Parser): 添加自然语言日期解析"

# 3. 合并回master
git switch master
git merge feature/smart-parser
git branch -d feature/smart-parser

# 4. 发布版本
git tag -a v1.2.0 -m "发布v1.2.0"
git push origin master --tags
```

## 文档导航

### 📚 指南 (Guides)

| 文档 | 内容 |
|------|------|
| [提交规范指南](guides/commit-guide.md) | Conventional Commits规范、类型说明、Scope建议、提交示例 |
| [分支策略指南](guides/branch-guide.md) | 分支结构、工作流程、命名规范、版本标签管理 |
| [Worktree指南](guides/worktree-guide.md) | 多工作树使用场景、命令、最佳实践 |
| [项目类型适配](guides/project-types.md) | 各类型项目的.gitignore和Scope建议 |

### 📖 参考 (References)

| 文档 | 内容 |
|------|------|
| [Git别名配置](references/aliases.md) | 常用别名配置及使用方法 |
| [.gitignore模板](references/gitignore-templates.md) | .NET、Node.js、Java、Python等项目模板 |
| [官方文档引用](references/official-docs.md) | Git官方文档、Conventional Commits等链接 |

### 📝 速查表 (Cheatsheets)

| 文档 | 内容 |
|------|------|
| [常用命令速查](cheatsheets/common-commands.md) | 日常开发、分支、撤销、历史查看等命令 |
| [问题处理指南](cheatsheets/troubleshooting.md) | 常见Git问题及解决方案 |

---

**提示**: 根据具体需求查阅对应的详细文档。如需快速参考，请查看速查表；如需深入理解，请查看指南文档。
