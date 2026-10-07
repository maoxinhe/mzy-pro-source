# CMY-Pro 启动器 · 发布仓库

本仓库为 **梦之韵科技 CMY 启动器** 的**构建与发布专用仓库**（公开），源码托管于私有仓库 `mzy-pro-source-code`。

## 下载
- 最新版完整包：https://apc.camzy.uno/update/download （自动 302 到最新 Release）
- 版本信息：https://apc.camzy.uno/update/latest
- Release 直链：本仓库 Releases 页

## 发布流程
1. 源码 push 到私有仓 `mzy-pro-source-code` 的 `hmcl-dev` 分支
2. 自动触发本仓库 GitHub Actions：编译 → 签名 → 按规范改名 `CMY-Pro_打包日期_架构_版本.exe` → 发布 Release → 同步云端版本号
