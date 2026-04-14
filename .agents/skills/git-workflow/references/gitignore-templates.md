# .gitignore 模板参考

各类型项目的.gitignore模板集合。

## .NET / WPF

```gitignore
# Visual Studio
.vs/
*.user
*.suo
*.userosscache

# Build outputs
bin/
obj/
out/
publish/

# NuGet
*.nupkg
packages/

# WPF特定
*.baml
GeneratedFiles/

# 本地配置
appsettings.Development.json
*.local.json
```

## Node.js

```gitignore
# Dependencies
node_modules/
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Build outputs
dist/
build/
.next/
.nuxt/

# Environment
.env
.env.local
.env.*.local

# IDE
.vscode/
.idea/

# OS
.DS_Store
Thumbs.db
```

## Java

```gitignore
# Compiled files
*.class
target/
build/

# IDE
.idea/
*.iml
.classpath
.project
.settings/

# Maven/Gradle
.mvn/
.gradle/

# Logs
*.log
```

## Python

```gitignore
# Byte-compiled
__pycache__/
*.py[cod]
*$py.class

# Virtual environments
venv/
env/
ENV/

# IDE
.vscode/
.idea/

# Environment
.env
.env.local

# Distribution
dist/
build/
*.egg-info/
```

## Rust

```gitignore
# Build
target/
Cargo.lock

# IDE
.idea/
.vscode/
*.iml

# OS
.DS_Store
Thumbs.db
```

## Go

```gitignore
# Binaries
*.exe
*.dll
*.so
*.dylib

# Test binary
*.test

# Output of go coverage tool
*.out

# Dependency directories
vendor/

# IDE
.idea/
.vscode/
```

## Flutter

```gitignore
# Flutter/Dart
.dart_tool/
.flutter-plugins
.flutter-plugins-dependencies
coverage/
lib/generated_plugin_registrant.dart

# Build
build/
ios/Pods/
android/.gradle/

# IDE
.idea/
.vscode/

# OS
.DS_Store
Thumbs.db
```

## 通用（所有项目适用）

```gitignore
# OS generated files
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db

# IDE
.vscode/
.idea/
*.swp
*.swo
*~

# Logs
*.log
logs/

# Local configuration
*.local
.env.local
config.local.*
```
