# Git 常见问题处理指南

常见Git问题及解决方案。

## 提交相关

### 忘记添加文件到提交

```bash
# 添加遗漏的文件
git add forgotten-file
git commit --amend --no-edit
```

### 提交信息写错了

```bash
# 修改上次提交信息
git commit --amend -m "正确的提交信息"
```

### 提交到错误分支

```bash
# 撤销提交但保留更改
git reset --soft HEAD~1

# 切换到正确分支
git switch correct-branch

# 重新提交
git commit -m "提交信息"
```

### 提交了敏感信息

```bash
# 撤销提交并彻底删除（仅限未推送）
git reset --hard HEAD~1

# 如果已推送，需要强制推送（谨慎使用）
git push origin HEAD --force-with-lease
```

## 合并冲突

### 解决合并冲突

```bash
# 查看冲突文件
git status

# 编辑冲突文件，解决冲突后标记为已解决
git add .

# 完成合并
git commit -m "fix: 解决合并冲突"

# 或放弃合并
git merge --abort
```

### 冲突标记格式

```
<<<<<<< HEAD
当前分支的内容
=======
合并分支的内容
>>>>>>> feature-branch
```

## 撤销操作

### 撤销工作区修改

```bash
# 撤销单个文件
git checkout -- filename
git restore filename

# 撤销所有文件
git checkout -- .
git restore .
```

### 撤销暂存区文件

```bash
# 取消暂存单个文件
git reset HEAD filename
git restore --staged filename

# 取消暂存所有文件
git reset HEAD
git restore --staged .
```

### 回退到指定版本

```bash
# 软回退（保留更改到暂存区）
git reset --soft HEAD~3

# 混合回退（保留更改到工作区）
git reset --mixed HEAD~3

# 硬回退（丢弃所有更改）
git reset --hard HEAD~3

# 回退到指定提交
git reset --hard commit-id
```

## 分支问题

### 误删分支

```bash
# 查看reflog找到分支的commit-id
git reflog

# 恢复分支
git checkout -b deleted-branch-name commit-id
```

### 强制删除未合并分支

```bash
git branch -D branch-name
```

### 重命名分支

```bash
# 重命名当前分支
git branch -m new-name

# 重命名指定分支
git branch -m old-name new-name

# 推送重命名后的分支并删除旧分支
git push origin -u new-name
git push origin --delete old-name
```

## 远程问题

### 推送被拒绝

```bash
# 先拉取更新再推送
git pull origin master
git push origin master

# 或使用rebase方式
git pull --rebase origin master
git push origin master
```

### 强制推送（谨慎使用）

```bash
# 安全的强制推送
git push origin HEAD --force-with-lease

# 强制推送（会覆盖远程历史）
git push origin HEAD --force
```

### 关联远程分支

```bash
# 设置上游分支
git branch -u origin/branch-name

# 或推送时设置
git push -u origin branch-name
```

## 其他问题

### 忽略文件不生效

```bash
# 清除缓存后重新添加
git rm -r --cached .
git add .
git commit -m "fix: 更新.gitignore"
```

### 文件权限变更

```bash
# 忽略文件权限变更
git config core.filemode false
```

### 行尾符问题（Windows/Linux混合开发）

```bash
# 自动处理行尾符
git config core.autocrlf true    # Windows
git config core.autocrlf input   # Linux/Mac
```

### 大文件误提交

```bash
# 从历史中彻底删除大文件（使用filter-repo或filter-branch）
git filter-repo --path large-file --invert-paths

# 或使用BFG Repo-Cleaner
bfg --delete-files large-file
```

## 救命命令

```bash
# 查看所有操作历史（可以找回丢失的提交）
git reflog

# 查看详细的引用日志
git reflog show HEAD

# 查看特定分支的reflog
git reflog show branch-name
```
