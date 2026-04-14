# Git Worktree 使用指南

Git worktree允许你在同一个仓库中维护多个工作目录，每个目录可以检出不同的分支。

## 使用场景

- **紧急修复**：正在开发新功能时，需要紧急修复生产环境问题
- **代码审查**：同时审查多个PR，每个worktree检出一个PR分支
- **长期任务**：有一个需要长时间运行的任务，不想影响日常开发
- **避免stash**：不想用stash保存当前工作状态

## 常用命令

### 查看所有worktree

```bash
git worktree list
```

### 创建新worktree

```bash
# 基于当前分支创建新分支并建立worktree
git worktree add ../project-fix fix-branch

# 创建worktree并检出已有分支
git worktree add ../project-hotfix hotfix-branch

# 创建worktree并立即锁定（防止被prune）
git worktree add --lock ../project-temp temp-branch
```

### 移除worktree

```bash
# 正常移除
git worktree remove ../project-fix

# 强制移除（有未提交更改时）
git worktree remove --force ../project-fix
```

### 清理和修复

```bash
# 清理无效的worktree记录
git worktree prune

# 修复移动的worktree
git worktree repair
```

## 最佳实践

### 命名规范

```bash
# 使用有意义的目录名
../project-name-feature/   # 功能开发
../project-name-hotfix/    # 紧急修复
../project-name-review/    # 代码审查
```

### 工作流程示例

**场景**：正在开发feature-A，需要紧急修复main分支的bug

```bash
# 1. 在main分支创建hotfix worktree
git worktree add ../project-hotfix main
cd ../project-hotfix

# 2. 创建修复分支并修复
git checkout -b hotfix/critical-bug
# ... 修复代码 ...
git commit -m "fix: 修复关键bug"
git push origin hotfix/critical-bug

# 3. 回到原项目继续开发
cd ../project
git worktree remove ../project-hotfix
```

### 注意事项

- 不能在同一路径创建多个worktree
- main worktree不能被移除
- 默认worktree之间共享同一个 `.git` 目录
- 可以使用 `--lock` 防止worktree被意外清理

## 配置建议

```bash
# 使用相对路径（便于移动项目）
git config worktree.useRelativePaths true
```
