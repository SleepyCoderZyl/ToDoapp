# Git 常用命令速查表

快速参考最常用的Git命令。

## 日常开发

```bash
# 查看状态
git status
git status -s          # 简洁模式

# 添加文件到暂存区
git add filename       # 添加指定文件
git add .              # 添加所有更改
git add -p             # 交互式添加（选择部分更改）

# 提交更改
git commit -m "提交信息"
git commit -am "提交信息"    # 添加并提交（仅对已跟踪文件）
git commit --amend -m "新信息"  # 修改上次提交

# 推送更改
git push origin master
git push origin --tags      # 推送所有标签

# 拉取更新
git pull origin master
git pull --rebase origin master  # 使用rebase方式拉取
```

## 分支操作

```bash
# 查看分支
git branch              # 本地分支
git branch -r           # 远程分支
git branch -a           # 所有分支
git branch -v           # 带最新提交的分支列表

# 创建分支
git branch feature-name
git switch -c feature-name      # 创建并切换（Git 2.23+）
git checkout -b feature-name    # 传统方式

# 切换分支
git switch branch-name          # Git 2.23+
git checkout branch-name        # 传统方式
git switch -                    # 切换到上一个分支

# 合并分支
git switch master
git merge feature-name
git merge --no-ff feature-name  # 禁用快进合并

# 删除分支
git branch -d feature-name      # 已合并
git branch -D feature-name      # 强制删除
```

## 撤销操作

```bash
# 撤销工作区的修改
git checkout -- filename
git restore filename            # Git 2.23+

# 撤销暂存区的文件
git reset HEAD filename
git restore --staged filename   # Git 2.23+

# 撤销上次提交（保留更改）
git reset --soft HEAD~1

# 撤销上次提交（丢弃更改）
git reset --hard HEAD~1

# 修改上次提交
git commit --amend -m "新的提交信息"
git commit --amend --no-edit    # 保留提交信息，添加新文件
```

## 查看历史

```bash
# 简洁日志
git log --oneline
git log --oneline -20           # 最近20条

# 图形化日志
git log --oneline --graph --all

# 查看文件修改历史
git log -p filename
git log --follow -p filename    # 跟踪重命名

# 查看某次提交的详情
git show commit-id

# 查看文件在某次提交中的状态
git show commit-id:filename
```

## Stash（临时保存）

```bash
# 保存当前更改
git stash
git stash push -m "描述信息"

# 查看stash列表
git stash list

# 恢复stash
git stash pop           # 恢复并删除
git stash apply         # 恢复但不删除

# 删除stash
git stash drop stash@{0}
git stash clear         # 清空所有stash
```

## 标签管理

```bash
# 创建标签
git tag -a v1.0.0 -m "发布版本 1.0.0"
git tag v1.0.0          # 轻量标签

# 推送标签
git push origin v1.0.0
git push origin --tags  # 推送所有标签

# 删除标签
git tag -d v1.0.0
git push origin --delete v1.0.0
```

## 远程操作

```bash
# 查看远程仓库
git remote -v

# 添加远程仓库
git remote add origin <url>

# 修改远程仓库URL
git remote set-url origin <new-url>

# 获取远程分支（不合并）
git fetch origin

# 拉取远程分支到本地
git checkout -b local-branch origin/remote-branch
```

## Worktree

```bash
# 查看worktree列表
git worktree list

# 创建worktree
git worktree add ../project-fix fix-branch

# 移除worktree
git worktree remove ../project-fix

# 清理无效worktree
git worktree prune
```
