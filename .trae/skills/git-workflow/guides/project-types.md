# 项目类型适配指南

根据项目类型自动配置相应的Git忽略规则和Scope建议。

## 检测项目类型

通过项目文件自动识别：

- **.csproj** + **App.xaml** → WPF项目
- **.csproj** (无App.xaml) → .NET类库/控制台
- **package.json** → Node.js项目
- **pom.xml / build.gradle** → Java项目
- **Cargo.toml** → Rust项目
- **go.mod** → Go项目
- **pubspec.yaml** → Flutter项目

## 各类型配置建议

### .NET / WPF 项目

**Scope建议：**
- `Views` / `ViewModels` / `Models` / `Services` / `Utils`
- `UI` / `Core` / `Data` / `Config`

**.gitignore：**
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

### Node.js 项目

**Scope建议：**
- `api` / `ui` / `components` / `utils` / `config`
- `deps` / `test` / `docs`

**.gitignore：**
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

### Java 项目

**Scope建议：**
- `controller` / `service` / `dao` / `entity` / `config`
- `api` / `impl` / `utils`

**.gitignore：**
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

### Python 项目

**Scope建议：**
- `models` / `views` / `controllers` / `utils` / `config`
- `api` / `cli` / `core`

**.gitignore：**
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

### 通用.gitignore（适用于所有项目）

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

## 自动生成Scope建议

根据项目结构自动生成Scope：

```bash
# 扫描项目文件夹生成Scope建议
# .NET项目
ls -d */ | grep -E "^(Views|ViewModels|Models|Services)"

# Node.js项目
ls -d */ | grep -E "^(src|components|pages|api|utils)"

# 通用：取前5个主要目录作为Scope建议
ls -d */ | head -5
```

## 项目结构模板

### .NET / WPF

```
Project/
├── .git/
├── .gitignore
├── README.md
├── LICENSE
├── Project.sln
├── src/
│   ├── Project/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Models/
│   │   └── Services/
│   └── Project.Tests/
└── docs/
```

### Node.js / React

```
project/
├── .git/
├── .gitignore
├── README.md
├── package.json
├── src/
│   ├── components/
│   ├── pages/
│   ├── utils/
│   └── api/
├── public/
└── tests/
```

### Java / Spring Boot

```
project/
├── .git/
├── .gitignore
├── README.md
├── pom.xml / build.gradle
├── src/
│   ├── main/
│   │   ├── java/
│   │   │   └── com/example/
│   │   │       ├── controller/
│   │   │       ├── service/
│   │   │       └── dao/
│   │   └── resources/
│   └── test/
└── docs/
```
