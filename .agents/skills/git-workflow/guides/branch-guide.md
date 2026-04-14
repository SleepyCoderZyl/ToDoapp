# Git 分支策略指南

本指南介绍个人开发者的Git分支管理策略和工作流程。

## 推荐分支结构

```
master (主分支，稳定版本)
  │
  ├── feature/xxx (功能分支)
  ├── fix/xxx (修复分支)
  └── release/v1.x (发布分支，可选)
```

## 工作流程

### 现代Git命令（推荐，Git 2.23+）

```bash
# 1. 开始新功能
git switch master
git pull
git switch -c feature/smart-parser

# 2. 开发过程中多次提交
git add .
git commit -m "feat(Parser): 添加自然语言日期解析"
git add .
git commit -m "feat(Parser): 支持节假日识别"

# 3. 功能完成，合并回master（推荐用rebase保持线性历史）
git switch master
git merge feature/smart-parser
git branch -d feature/smart-parser

# 4. 发布版本
git tag -a v1.2.0 -m "发布v1.2.0：智能解析功能"
git push origin master --tags
```

### 传统命令方式

```bash
# 1. 开始新功能
git checkout master
git pull
git checkout -b feature/smart-parser

# 2. 开发过程中多次提交
git add .
git commit -m "feat(Parser): 添加自然语言日期解析"

# 3. 功能完成，合并回master
git checkout master
git merge feature/smart-parser
git branch -d feature/smart-parser
```

## 命令对比

| 操作 | 传统命令 | 现代命令（Git 2.23+） |
|------|---------|---------------------|
| 切换分支 | `git checkout master` | `git switch master` |
| 创建并切换 | `git checkout -b feature` | `git switch -c feature` |
| 切换到上一个分支 | `git checkout -` | `git switch -` |

## 分支命名规范

### 功能分支

```bash
feature/<功能名称>
# 示例：
feature/smart-parser
feature/dark-mode
feature/user-auth
```

### 修复分支

```bash
fix/<bug描述>
# 示例：
fix/tray-icon-disappear
fix/memory-leak
fix/startup-crash
```

### 发布分支（可选）

```bash
release/v<版本号>
# 示例：
release/v1.2.0
release/v2.0.0-beta
```

### 热修复分支（可选）

```bash
hotfix/<问题描述>
# 示例：
hotfix/critical-security-fix
hotfix/data-loss-bug
```

## 分支操作流程

### 查看分支

```bash
# 本地分支
git branch

# 远程分支
git branch -r

# 所有分支
git branch -a
```

### 创建分支

```bash
# 创建新分支（基于当前分支）
git branch feature-name

# 创建并切换到新分支
git switch -c feature-name
# 或传统方式
git checkout -b feature-name
```

### 切换分支

```bash
# 切换到指定分支
git switch branch-name
# 或
git checkout branch-name

# 切换到上一个分支
git switch -
# 或
git checkout -
```

### 合并分支

```bash
# 切换到目标分支
git switch master

# 合并功能分支
git merge feature-name

# 使用rebase保持线性历史（推荐）
git switch feature-name
git rebase master
git switch master
git merge feature-name
```

### 删除分支

```bash
# 删除已合并的分支
git branch -d feature-name

# 强制删除分支（未合并）
git branch -D feature-name

# 删除远程分支
git push origin --delete feature-name
```

## 版本标签管理

### 创建标签

```bash
# 创建附注标签（推荐）
git tag -a v1.0.0 -m "发布版本 1.0.0"

# 创建轻量标签
git tag v1.0.0
```

### 推送标签

```bash
# 推送单个标签到远程
git push origin v1.0.0

# 推送所有标签
git push origin --tags
```

### 删除标签

```bash
# 删除本地标签
git tag -d v1.0.0

# 删除远程标签
git push origin --delete v1.0.0
```

### 语义化版本号规则

- `v1.0.0` - 主版本（重大更新，不兼容改动）
- `v1.1.0` - 次版本（新功能，向后兼容）
- `v1.1.1` - 修订版本（bug修复）

## 最佳实践

1. **保持master分支稳定** - 只合并经过测试的代码
2. **功能分支及时删除** - 合并后及时清理，避免分支过多
3. **使用有意义的命名** - 分支名应清晰表达其目的
4. **定期同步master** - 长期功能分支定期rebase或merge master
5. **小步快跑** - 功能分支生命周期不宜过长，尽快合并
