# ECH Workers Manager 构建说明

## 项目结构

创建以下文件结构:

```
EchWorkersManager/
├── EchWorkersManager.csproj
├── Program.cs
├── Form1.cs
└── README.md
```

## 构建步骤

### 方法1: 使用 Visual Studio 2019/2022

1. 打开 Visual Studio
2. 选择 "创建新项目"
3. 选择 "Windows 窗体应用(.NET)" 
4. 项目名称输入: `EchWorkersManager`
5. 创建项目后,将提供的三个文件内容替换到对应文件中:
   - `Form1.cs` (替换整个文件)
   - `Program.cs` (替换整个文件)
   - `EchWorkersManager.csproj` (替换整个文件)
6. 按 `Ctrl+Shift+B` 或点击 "生成" -> "生成解决方案"
7. 编译后的 exe 在 `bin\Debug\net6.0-windows\` 或 `bin\Release\net6.0-windows\` 目录

### 方法2: 使用命令行编译 (需要安装 .NET 6 SDK)

1. 下载并安装 [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)

2. 创建项目文件夹并保存文件:
```bash
mkdir EchWorkersManager
cd EchWorkersManager
```

3. 将三个代码文件保存到该文件夹

4. 编译项目:
```bash
dotnet build -c Release
```

5. 编译后的 exe 在: `bin\Release\net6.0-windows\EchWorkersManager.exe`

6. 或者直接运行:
```bash
dotnet run
```

### 方法3: 发布独立应用 (不需要安装 .NET 运行时)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

发布后的单文件 exe 在: `bin\Release\net6.0-windows\win-x64\publish\`

## 使用说明

### 快速开始(3步即可)
1. **启动服务**: 点击"启动服务"按钮 → 自动启动 SOCKS5 和 HTTP 代理
2. **启用系统代理**: 点击"启用系统代理"按钮 → 所有浏览器立即生效
3. **开始上网**: 打开任何浏览器即可使用代理

### 详细说明
1. **配置参数**: 填写域名、IP、Token、本地SOCKS5地址
2. **HTTP代理端口**: 默认10809(可修改)
3. **保存配置**: 点击"保存配置"保存设置
4. **禁用代理**: 点击"禁用系统代理"取消系统代理
5. **停止服务**: 点击"停止服务"停止所有代理服务

## 🎯 核心功能说明

### 自动 SOCKS5 → HTTP 代理转换
程序**内置了代理转换器**,工作流程如下:

```
浏览器 → HTTP代理(127.0.0.1:10809) → SOCKS5代理(127.0.0.1:30000) → ech-workers → 目标网站
```

### 一键启用系统代理
1. 点击"启动服务"按钮
2. 点击"启用系统代理"按钮
3. ✅ **所有浏览器(Chrome/Firefox/Edge)立即生效!**

**无需安装任何插件!** 就像使用 v2rayN 一样简单!

## 注意事项

1. **ech-workers.exe 位置**: 程序会在当前目录查找 `ech-workers.exe`,请确保将编译后的管理器和 ech-workers.exe 放在同一目录
2. **管理员权限**: 某些情况下修改系统代理可能需要管理员权限
3. **.NET 运行时**: 如果使用 Debug/Release 编译,运行时需要安装 .NET 6 运行时。使用"发布独立应用"方式可以避免此要求
4. **配置保存**: 配置保存在注册表 `HKEY_CURRENT_USER\Software\EchWorkersManager` 中

## 功能特性

- ✅ 可视化配置界面
- ✅ 启动/停止服务
- ✅ **内置 SOCKS5 → HTTP 代理转换器**
- ✅ **一键启用系统代理,所有浏览器立即生效**
- ✅ **无需安装浏览器插件**
- ✅ 配置自动保存/加载
- ✅ 退出时安全提示
- ✅ 状态实时显示
- ✅ 完全模拟 v2rayN 的使用体验

## 系统要求

- Windows 7 SP1 或更高版本
- .NET 6.0 运行时 (或使用独立发布版本)

## 故障排除

**问题**: 点击启动服务提示找不到 ech-workers.exe
**解决**: 将 ech-workers.exe 复制到管理器程序所在目录

**问题**: 设置代理失败
**解决**: 尝试以管理员身份运行程序

**问题**: 程序无法启动
**解决**: 确保已安装 .NET 6.0 运行时或使用独立发布版本