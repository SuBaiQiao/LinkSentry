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

---

## 🏗️ 技术架构

```
LinkSentry/
├── Models/
│   └── NetworkInterfaceModel.cs    # 网卡数据模型 + 图表配置
├── ViewModels/
│   ├── MainViewModel.cs            # 主页逻辑（已连接/已断开分组）
│   └── DetailViewModel.cs          # 详情页逻辑（网卡操作命令）
├── Views/
│   └── DetailWindow.axaml(.cs)     # 详情窗口（左右分栏）
├── Services/
│   ├── INetworkService.cs          # 网络服务接口
│   └── NetworkService.cs           # 网络数据采集与流量统计
├── MainWindow.axaml(.cs)           # 主窗口（侧边栏 + 仪表盘）
├── App.axaml(.cs)                  # 应用入口 + DI 容器配置
└── Program.cs                      # 启动引导
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

- [x] 🖥️ **网卡列表**：自动发现所有网络接口，显示名称、描述、类型
- [x] 🟢 **状态分组**：已连接（绿色）与已断开（红色）分层显示
- [x] 🌐 **地址信息**：IPv4 / IPv6 / MAC 地址展示
- [x] 📋 **一键复制**：IPv4 地址旁的复制按钮，带"✓ 已复制"反馈
- [x] 📊 **实时流量图**：带面积填充的上传/下载双线折线图
- [x] 📈 **动态 Y 轴**：0-100 Kbps 基准，超出后自动扩展并显示最大值
- [x] 🕐 **1 分钟窗口**：X 轴固定展示最近 60 秒的流量趋势
- [x] 💡 **悬停提示**：鼠标悬停图表可查看精确的上传/下载速率
- [x] 📑 **详情页面**：左右分栏布局，含 IP/DNS/网关/DHCP 信息
- [x] ⚡ **快捷操作**：启用/禁用网卡、更新 DHCP、刷新 DNS
- [x] 🎨 **侧边导航栏**：垂直图标菜单，现代化 UI 风格
- [x] 🇨🇳 **简体中文**：全程中文界面，适配中国大陆用户

---

## ⚠️ 注意事项

1. **管理员权限**：启用/禁用网卡、更新 DHCP 等操作需要以管理员身份运行程序
2. **平台兼容性**：DHCP 状态检测 (`IsDhcpEnabled`) 仅在 Windows 上可用
3. **流量统计**：速率数据基于 `System.Net.NetworkInformation` 的字节计数差值计算，每 2 秒刷新一次

---

## 📄 开源协议

本项目基于 [MIT License](LICENSE) 开源。

---

<p align="center">
  <sub>🤖 This entire project was designed, coded, debugged and documented by AI.</sub><br/>
  <sub>Built with ❤️ by <b>Antigravity (Google DeepMind)</b></sub>
</p>
