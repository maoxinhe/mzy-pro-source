# 梦之韵Pro - Minecraft 启动器

基于 PCL-CE 二次开发的 Minecraft 启动器，专为「梦之韵Pro」MC 服务器打造。

## 特性

- **淡粉色主题**：梦之韵专属 UI 配色
- **云端驱动更新**：启动器上报版本 → 智谱AI判断 → 自动推送更新
- **错误自动上报**：玩家报错自动上传云端 → AI分析 → 邮件通知开发者
- **AI 智能助手**：内置智谱 glm-4-flash，支持问答 + 错误日志分析
- **内置论坛**：一键跳转 forum.camzy.uno
- **正版登录**：Microsoft OAuth 支持
- **QQ 头像**：SSO 登录自动获取玩家 QQ 头像
- **模组自动更新**：国内 CDN 加速下载
- **整合包一键安装**：国内反向代理加速

## 技术栈

| 组件 | 技术 |
|---|---|
| 启动器 | C# / WPF / .NET 10 |
| 云端 API | Cloudflare Workers |
| AI 大脑 | 智谱 glm-4-flash |
| 更新分发 | OneDrive 企业版 + GitHub Releases |
| 错误通知 | Resend 邮件 |

## 快速开始

1. 下载最新版 `MZY-Pro_YYMMDD_x64_N.exe`
2. 双击运行，无需安装
3. 登录正版或离线账号
4. 选择实例 → 启动游戏

## 项目结构

```
mzy-pro-source/
├── Plain Craft Launcher 2/    # WPF 主程序
├── PCL.Core/                  # 核心库
├── Plain Craft Launcher 2.sln # 解决方案
└── README.md
```

## 编译

```bash
dotnet publish "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" \
  -c Release -r win-x64 --self-contained true
```

## 相关链接

- 官网：https://www.camzy.tech
- 论坛：https://forum.camzy.uno
- 管理后台：https://admin.camzy.uno
- API：https://apc.camzy.uno

## 版权

Copyright © 苏州梦之韵科技. All Rights Reserved.
基于 PCL-CE (Plain Craft Launcher Community Edition) 二次开发。
