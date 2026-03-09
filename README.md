<p align="center">
  <h1 align="center">🌐 NetPulse — 连线卫士</h1>
  <p align="center">
    <b>一款完全由 AI 开发的跨平台网络诊断与监控工具</b><br/>
    <sub>Powered by Avalonia UI · .NET 8 · LiveCharts2</sub>
  </p>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet" />
  <img src="https://img.shields.io/badge/Avalonia_UI-11.3-blue?logo=data:image/svg+xml;base64," />
  <img src="https://img.shields.io/badge/Language-C%2312-green" />
  <img src="https://img.shields.io/badge/Built_by-AI-ff6f00?logo=robot" />
  <img src="https://img.shields.io/badge/License-MIT-yellow" />
</p>

---

> **⚠️ AI 开发声明**
> 
> 本项目**100% 由 AI（Antigravity / Google DeepMind）**生成代码、编写架构、设计 UI 并完成调试。人类开发者仅提供需求描述和功能反馈，未手动编写任何一行代码。

---

## 📸 功能预览

### 主面板 — 仪表盘
- 左侧垂直图标导航栏
- 已连接 / 已断开网卡分层展示
- 每张网卡卡片包含：IP 信息、实时上传/下载速度、带面积填充的实时流量折线图
- IPv4 地址一键复制（带 ✓ 已复制 提示）

### 详情页
- 左右分栏布局：左侧展示详细网络信息，右侧展示大尺寸实时流量图
- 快捷操作：启用/禁用网卡、更新 DHCP、刷新 DNS 缓存

### 安全与端口
- [x] 🛡️ **安全与端口**：新增安全中心，集成防火墙监控与端口分析
- [x] 🔥 **防火墙状态**：实时显示 Windows Defender 各配置文件的状态与规则
- [x] 🔍 **连接追踪**：深度扫描 TCP/UDP 端口使用情况，自动关联进程
- [x] 🔪 **进程管理**：支持查找程序位置与强制终止可疑进程
- [x] ⚡ **高性能 UI**：端口列表使用 DataGrid 虚拟化加载，支持大规模连接展示
- [x] 🎨 **视图切换**：侧边栏图标导航，Dashboard 与 Security 模块平滑切换

---

## 🏗️ 技术架构

```
LinkSentry/
├── Models/
│   ├── NetworkInterfaceModel.cs    # 网卡数据模型 + 图表配置
│   ├── FirewallProfileInfo.cs      # 防火墙配置文件状态模型
│   └── PortConnectionInfo.cs       # TCP/UDP 连接信息模型
├── ViewModels/
│   ├── MainViewModel.cs            # 主页逻辑（导航 + 网卡分组）
│   ├── DetailViewModel.cs          # 详情页逻辑（网卡操作命令）
│   └── SecurityViewModel.cs        # 安全页逻辑（防火墙 + 端口监控）
├── Views/
│   ├── DashboardView.axaml(.cs)    # 仪表盘视图
│   ├── DetailWindow.axaml(.cs)     # 详情窗口（左右分栏）
│   └── SecurityView.axaml(.cs)     # 安全与端口视图
├── Services/
│   ├── INetworkService.cs          # 网络服务接口
│   ├── NetworkService.cs           # 网络数据采集与流量统计
│   ├── IFirewallService.cs         # 防火墙服务接口
│   ├── FirewallService.cs          # 通过 netsh 获取防火墙状态
│   ├── IPortService.cs             # 端口扫描服务接口
│   ├── PortService.cs              # P/Invoke iphlpapi.dll 获取连接表
│   └── DiagnosticLogger.cs         # 诊断日志服务（线程安全文件写入）
├── MainWindow.axaml(.cs)           # 主窗口（侧边栏 + 页面容器）
├── App.axaml(.cs)                  # 应用入口 + DI 容器配置
└── Program.cs                      # 启动引导 + 全局异常处理
```

### 设计模式

| 模式 | 说明 |
|------|------|
| **MVVM** | 使用 CommunityToolkit.Mvvm 实现 ViewModel 与 View 的完全解耦 |
| **依赖注入** | 通过 Microsoft.Extensions.DependencyInjection 管理服务生命周期 |
| **响应式数据绑定** | Avalonia 编译时绑定 (`x:DataType`) 保证类型安全与性能 |
| **观察者模式** | ObservableCollection + ObservableProperty 驱动 UI 实时刷新 |

---

## 🛠️ 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| **运行时** | .NET | 8.0 |
| **UI 框架** | Avalonia UI | 11.3.11 |
| **主题** | Avalonia.Themes.Fluent | 11.3.11 |
| **MVVM 工具包** | CommunityToolkit.Mvvm | 8.4.0 |
| **图表库** | LiveChartsCore.SkiaSharpView.Avalonia | 2.0.0-rc6.1 |
| **依赖注入** | Microsoft.Extensions.DependencyInjection | 10.0.3 |
| **日志** | Microsoft.Extensions.Logging.Console | 10.0.3 |
| **网络 API** | System.Net.NetworkInformation | 内置 |
| **端口扫描** | iphlpapi.dll (P/Invoke) | Windows API |
| **防火墙检测** | netsh advfirewall | Windows 内置 |

---

## 🚀 快速开始

### 环境要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本
- Windows / macOS / Linux（Avalonia 跨平台支持）

### 克隆与运行

```bash
# 克隆仓库
git clone https://github.com/YOUR_USERNAME/LinkSentry.git
cd LinkSentry

# 还原依赖
dotnet restore

# 运行程序
dotnet run
```

### 发布独立可执行文件

```bash
# Windows 独立发布
dotnet publish -c Release -r win-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

发布后的文件位于 `bin/Release/net8.0/{RID}/publish/` 目录。

---

## 📋 功能清单

- [x] 🛡️ **安全与端口**：新增安全中心，集成防火墙监控与端口分析
- [x] 🔥 **防火墙状态**：实时显示 Windows Defender 各配置文件的状态与规则
- [x] 🔍 **连接追踪**：深度扫描 TCP/UDP 端口使用情况，自动关联进程
- [x] 🔪 **进程管理**：支持查找程序位置与强制终止可疑进程
- [x] ⚡ **高性能 UI**：端口列表使用 DataGrid 虚拟化加载，支持大规模连接展示
- [x] 🎨 **视图切换**：侧边栏图标导航，Dashboard 与 Security 模块平滑切换

---

## ⚠️ 注意事项

1. **管理员权限**：启用/禁用网卡、更新 DHCP、结束进程等操作需要以管理员身份运行程序
2. **平台兼容性**：DHCP 状态检测 (`IsDhcpEnabled`)、防火墙监控 (`netsh`)、端口扫描 (`iphlpapi.dll`) 仅在 Windows 上可用
3. **流量统计**：速率数据基于 `System.Net.NetworkInformation` 的字节计数差值计算，每 2 秒刷新一次
4. **诊断日志**：安全模块启动时会生成诊断日志，路径显示在页面底部，用于排查加载异常
5. **崩溃日志**：全局异常处理会将崩溃信息写入 `%LOCALAPPDATA%/LinkSentry/crash.log`

---

## 📄 开源协议

本项目基于 [MIT License](LICENSE) 开源。

---

<p align="center">
  <sub>🤖 This entire project was designed, coded, debugged and documented by AI.</sub><br/>
  <sub>Built with ❤️ by <b>Antigravity (Google DeepMind)</b></sub>
</p>
