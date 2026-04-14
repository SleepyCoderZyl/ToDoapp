# Git 别名配置参考

常用Git别名配置，添加到 `~/.gitconfig` 文件中。

## 基础别名

```ini
[alias]
    # 快速查看状态
    s = status -s
    
    # 快速提交
    cm = commit -m
    
    # 查看简洁日志
    lg = log --oneline --graph --decorate -20
    
    # 查看所有分支日志
    lga = log --oneline --graph --decorate --all -20
    
    # 快速切换分支
    co = checkout
    
    # 创建并切换分支
    cob = checkout -b
    
    # 快速推送
    ps = push
    
    # 快速拉取
    pl = pull
    
    # 查看修改
    d = diff
    
    # 撤销上次提交但保留更改
    undo = reset --soft HEAD~1
    
    # 查看最近的提交
    last = log -1 HEAD --stat
```

## 高级别名

```ini
[alias]
    # 查看分支列表（带最新提交）
    lb = branch -v
    
    # 查看远程分支
    rb = branch -r
    
    # 快速暂存
    a = add
    aa = add --all
    
    # 快速提交所有
    ca = commit -am
    
    # 查看文件历史
    fh = log --follow -p --
    
    # 查看谁修改了某行
    blame = blame -w -M -C
    
    # 清理已合并分支
    cleanup = !git branch --merged | grep -v \"\\*\" | xargs -n 1 git branch -d
    
    # 查看仓库大小
    size = !git count-objects -vH
```

## 配置方法

### 编辑配置文件

```bash
# 打开全局配置文件
git config --global --edit

# 或直接编辑文件
nano ~/.gitconfig
```

### 命令行添加

```bash
# 添加单个别名
git config --global alias.s "status -s"
git config --global alias.lg "log --oneline --graph --decorate -20"
```

## 使用示例

```bash
# 使用别名查看状态
git s

# 使用别名提交
git cm "feat: 添加新功能"

# 使用别名查看日志
git lg

# 使用别名创建并切换分支
git cob feature/new-feature
```
