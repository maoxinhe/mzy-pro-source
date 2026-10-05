# 梦之韵Pro - AI 交接文档

> 给下一个接手的 AI：完整项目状态、关键路径、发布流程、注意事项。

## 项目概览

梦之韵Pro 是一个基于 PCL-CE 二次开发的 Minecraft 启动器，配套完整的云端服务（论坛/后台/API/更新分发）。

## 仓库

| 仓库 | 地址 | 说明 |
|---|---|---|
| 启动器源码 | github.com/maoxinhe/mzy-pro-source | C# WPF .NET 10 |
| 云端服务 | github.com/maoxinhe/mzy-cloud | Cloudflare Workers 全套 |
| 发布产品 | github.com/maoxinhe/mzy-pro/releases | exe 发布 |

**均为私有仓库。**

## 关键凭据

```
CF Token:    cfut_aoOoTb3ir3YUiNDQQMT3bxxGd0mIl1Izb8VVPKOe4b73fc50
CF Account:  93fe0753b1dce058535acb744bb78794
GitHub:      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
智谱:        ec0af966f18b4fec8e3686ad0b5faba6.gGElFHtvh0TgpJHK
Resend:      re_JuroxRKM_pr6mXai5g3YMePCuosqdQ4oX
SSO admin:   ak_bA636FVtGVqWTIgY1BBYWWrcLgd-eCMr
```

## Azure 应用

| 用途 | Client ID | Tenant ID |
|---|---|---|
| OneDrive 上传 | 56f7493a-ac31-4a2a-b300-6b731f58c99e | b3d47cd3-2f3f-487b-9fee-9b415566acb4 |
| 正版登录 | 22e33790-2127-4140-ab13-849fcd8be0ee | 同上 |

## 域名

| 域名 | 用途 |
|---|---|
| www.camzy.tech | 官网 |
| forum.camzy.uno | 论坛 |
| admin.camzy.uno | 管理后台 |
| apc.camzy.uno | API / 更新 / AI / 错误上报 |

## 发布流程（必须严格遵守）

```
1. 改代码
2. 改 metadata.json 版本号（不是 csproj！）
3. dotnet publish 编译
4. 改名 MZY-Pro_YYMMDD_x64_N.exe
5. 改 Worker /update/check 里 latest.version
6. wrangler deploy
7. 传 OneDrive（覆盖 MengZhiYunPro-x64.exe）
8. GitHub Release 上传 exe
9. git push 源码
```

**版本号在 `Plain Craft Launcher 2/metadata.json`，不是 csproj！**

## 启动器关键文件

| 文件 | 作用 |
|---|---|
| Plain Craft Launcher 2/metadata.json | 版本号定义 |
| Plain Craft Launcher 2/FormMain.xaml.cs | CheckUpdateAsync 更新检测 |
| Plain Craft Launcher 2/Application.xaml.cs | 全局异常捕获 + 错误上报 |
| PCL.Core/App/Secrets.cs | OAuth ClientId |
| Plain Craft Launcher 2/Images/icon.ico | 启动器图标 |
| Plain Craft Launcher 2/Images/Backgrounds/default_bg.jpg | 默认背景 |

## 云端 API（apc.camzy.uno）

| 路径 | 方法 | 说明 |
|---|---|---|
| /update/check?v=版本 | GET | 启动器上报版本，AI判断是否更新 |
| /update/download | GET | OneDrive 直链 302 |
| /api/ai/chat | POST | 智谱 AI 助手 |
| /error/report | POST | 错误上报 → AI分析 → 邮件 |
| /mc/mzy | GET | 整合包 API 反向代理 |

## 编译命令

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet publish "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" \
  -c Release -r win-x64 --self-contained true -o /tmp/mzy_out
```

## 已知坑

1. **版本号在 metadata.json**，不是 csproj AssemblyVersion
2. **PCL 自带更新已禁用**（注释掉了 UpdateManager.Start）
3. **OneDrive 匿名分享被企业版禁止**，用 Graph API client_credentials
4. **raw.githubusercontent.com 国内不通**，用 jsdelivr CDN
5. **exe 没代码签名**，SmartScreen 会警告（发行商显示苏州梦之韵科技）

## 当前版本

- 启动器：2.15.31
- Worker：已部署最新
- 最新 exe：MZY-Pro_261005_x64_31.exe
