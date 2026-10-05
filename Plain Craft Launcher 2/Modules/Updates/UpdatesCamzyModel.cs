using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using PCL.Core.Utils;
using PCL.Network;

namespace PCL;

/// <summary>
///     梦之韵Pro 自家更新源：公告与更新检测全部走自家 Cloudflare Worker（apc.camzy.uno），
///     不再访问任何境外更新服务器（GitHub / pysio / naids 等）。
///     注意：梦之韵Pro 的实际更新下载由「软件更新」页（PageSetupUpdate → apc/launcher.json）完成，
///     本模型仅用于消除启动时的境外访问、并让 PCL 原生更新检查返回"已最新"。
/// </summary>
public class UpdatesCamzyModel : IUpdateSource
{
    private const string ApiBase = "https://apc.camzy.uno";

    public string SourceName { get; set; } = "梦之韵";

    public bool IsAvailable() => true;

    public bool RefreshCache() => true;

    public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
    {
        // 不访问境外：始终返回当前版本
        return new VersionDataModel
        {
            VersionName = ModBase.versionBaseName,
            VersionCode = ModBase.versionCode,
            Sha256 = "",
            Source = SourceName,
            Changelog = ""
        };
    }

    public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
    {
        // 梦之韵Pro 更新走自家 apc/launcher.json，这里恒返回"已最新"，避免触发 PCL 原生境外更新下载
        return true;
    }

    public VersionAnnouncementDataModel GetAnnouncementList()
    {
        // 从自家 apc 拉取公告（国内可达），失败返回空公告
        try
        {
            var json = Requester.FetchJson<JsonObject>(ApiBase + "/api/notice", RequestParam.WithRetry);
            var enabled = json?["enabled"]?.GetValue<bool>() ?? false;
            if (!enabled)
                return new VersionAnnouncementDataModel { Content = new List<VersionAnnouncementContentModel>() };

            var title = json["title"]?.ToString();
            var content = json["content"]?.ToString();
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                return new VersionAnnouncementDataModel { Content = new List<VersionAnnouncementContentModel>() };

            var btnText = json["button_text"]?.ToString();
            var btnUrl = json["button_url"]?.ToString();

            return new VersionAnnouncementDataModel
            {
                Content = new List<VersionAnnouncementContentModel>
                {
                    new()
                    {
                        Title = title,
                        Detail = content,
                        Id = "camzy-notice",
                        Date = DateTime.Now.ToString("yyyy-MM-dd"),
                        Btn1 = string.IsNullOrWhiteSpace(btnUrl)
                            ? null
                            : new AnnouncementBtnInfoModel
                            {
                                Text = string.IsNullOrWhiteSpace(btnText) ? "查看" : btnText,
                                Command = "OpenUrl",
                                CommandParameter = btnUrl
                            }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 拉取自家公告失败");
            return new VersionAnnouncementDataModel { Content = new List<VersionAnnouncementContentModel>() };
        }
    }

    public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
    {
        // 不提供 PCL 原生更新下载，返回空
        return new List<ModLoader.LoaderBase>();
    }
}
