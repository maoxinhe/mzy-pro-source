using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Text.Json.Serialization;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Utils;
using PCL.Modules.Minecraft;
using PCL.Network;

namespace PCL;

public partial class PageSetupService
{
    // 梦之韵Pro 服务端配置（可按需修改）
    public const string McServerAddress = "remv.top:11111";
    public const string WebsiteUrl = "https://www.camzy.tech";
    public const string ApiBase = "https://apc.camzy.uno";
    public const string Contact = "QQ 1096550598 / TG @maoxinhe";
    public const string PlayerPortalUrl = "https://player.camzy.uno/#account";
    public const string YggAuthServer = "https://ygg.camzy.uno";

    private bool isLoaded;

    public PageSetupService()
    {
        InitializeComponent();
        Loaded += PageSetupService_Loaded;
    }

    private void PageSetupService_Loaded(object sender, RoutedEventArgs e)
    {
        if (isLoaded)
            return;
        isLoaded = true;
        PanBack.ScrollToHome();
        LoadChangelogAsync();
        LoadRemoteConfigAsync();
        LoadNoticeAsync();
        LoadSrvStatusAsync();
    }

    private async void LoadSrvStatusAsync()
    {
        try
        {
            var baseApi = "https://apc.camzy.uno";
            var stTask = HttpRequest.GetJsonAsync<SrvStatusModel>(baseApi + "/api/status");
            var upTask = HttpRequest.GetJsonAsync<SrvUptimeModel>(baseApi + "/api/uptime");
            var rkTask = HttpRequest.GetJsonAsync<SrvRankingModel>(baseApi + "/api/ranking");
            await Task.WhenAll(stTask, upTask, rkTask);
            var st = stTask.Result; var up = upTask.Result; var rk = rkTask.Result;
            TxtSrvOnline.Text = st?.Ok == true ? (st.Online ? "在线" : "离线") : "未知";
            TxtSrvOnline.Foreground = new System.Windows.Media.SolidColorBrush(
                st?.Online == true
                    ? System.Windows.Media.Color.FromRgb(0x2e, 0x9e, 0x5b)
                    : System.Windows.Media.Color.FromRgb(0xc0, 0x39, 0x2b));
            TxtSrvPing.Text = st?.Ping != null ? st.Ping + " ms" : "-";
            TxtSrvUptime.Text = up != null ? FmtUptime(up.UptimeSeconds) : "-";
            TxtSrvPlayers.Text = up?.OnlineCount != null ? up.OnlineCount + " / " + (st?.PlayersMax?.ToString() ?? "?") : "-";
            var list = rk?.Ranking ?? new List<SrvRankItem>();
            TxtSrvRank.Text = list.Count > 0
                ? "在线时长排行：" + string.Join("  ", list.Take(8).Select((pp, i) => $"{i + 1}.{pp.Name} {FmtUptime(pp.Seconds)}"))
                : "在线时长排行：暂无数据（需服务器心跳上报）";
        }
        catch (Exception ex)
        {
            TxtSrvOnline.Text = "获取失败";
            ModBase.Log(ex, "[梦之韵Pro] 服务器状态加载失败");
        }
    }

    private static string FmtUptime(long sec)
    {
        if (sec <= 0) return "-";
        var d = sec / 86400; var h = (sec % 86400) / 3600; var m = (sec % 3600) / 60;
        return (d > 0 ? d + "天 " : "") + h + "时" + m + "分";
    }

    private void BtnSrvRefresh_Click(object sender, RoutedEventArgs e) => LoadSrvStatusAsync();

    private void BtnCopyServer_Click(object sender, MouseButtonEventArgs e)
    {
        ModBase.ClipboardSet(McServerAddress);
    }

    private void BtnWebsite_Click(object sender, EventArgs e)
    {
        ModBase.OpenWebsite(WebsiteUrl);
    }

    private void BtnYggPortal_Click(object sender, MouseButtonEventArgs e)
    {
        ModBase.OpenWebsite(PlayerPortalUrl);
    }

    private void BtnYggCopy_Click(object sender, MouseButtonEventArgs e)
    {
        ModBase.ClipboardSet(YggAuthServer);
    }

    private async void BtnMods_Click(object sender, MouseButtonEventArgs e)
    {
        if (!ModAutoUpdater.IsEnabled)
        {
            ModMain.MyMsgBox("模组更新功能当前未开放，请等待服务器维护完成。", "梦之韵Pro", isWarn: true);
            return;
        }

        // 定位当前选中的 Minecraft 实例
        var instance = ModInstanceList.McMcInstanceSelected;
        if (instance is null)
        {
            ModMain.MyMsgBox(Lang.Text("Setup.Service.Mods.NoInstance"), Lang.Text("Setup.Service.Mods.Title"));
            return;
        }

        var modsFolder = ModAutoUpdater.GetModsFolder(instance);

        // 更新 UI 状态
        BtnMods.IsEnabled = false;
        PanModUpdate.Visibility = Visibility.Visible;
        TextModUpdateStatus.Text = Lang.Text("Setup.Service.Mods.Fetching");
        ProgressModUpdate.Value = 0;
        try
        {
            var progress = new Progress<(int current, int total, string name, string status)>(p =>
            {
                TextModUpdateStatus.Text = Lang.Text("Setup.Service.Mods.Progress", p.current, p.total, p.name, p.status);
                ProgressModUpdate.Value = p.total == 0 ? 0 : (double)p.current / p.total * 100;
            });

            var result = await Task.Run(() => ModAutoUpdater.RunAsync(modsFolder, progress));

            // 汇总结果
            if (!string.IsNullOrEmpty(result.Error))
            {
                TextModUpdateStatus.Text = Lang.Text("Setup.Service.Mods.Error", result.Error);
                ModMain.MyMsgBox(result.Error, Lang.Text("Setup.Service.Mods.Title"), button2: Lang.Text("Common.Action.Confirm"), isWarn: true);
                return;
            }

            var summary = Lang.Text("Setup.Service.Mods.Summary",
                result.Missing.Count, result.Downloaded.Count, result.Failed.Count, result.Deleted.Count);
            TextModUpdateStatus.Text = summary;
            if (result.Failed.Count > 0)
                ModMain.MyMsgBox(summary + "\n\n" + string.Join("\n", result.Failed), Lang.Text("Setup.Service.Mods.Title"), button2: Lang.Text("Common.Action.Confirm"), isWarn: true);
            else
                ModMain.MyMsgBox(summary, Lang.Text("Setup.Service.Mods.Title"), button2: Lang.Text("Common.Action.Confirm"));
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 模组更新失败");
            TextModUpdateStatus.Text = Lang.Text("Setup.Service.Mods.Error", ex.Message);
            ModMain.MyMsgBox(Lang.Text("Setup.Service.Mods.Error", ex.Message), Lang.Text("Setup.Service.Mods.Title"), button2: Lang.Text("Common.Action.Confirm"), isWarn: true);
        }
        finally
        {
            BtnMods.IsEnabled = true;
        }
    }

    /// <summary>
    ///     一键安装整合包：从 API 拉取整合包列表 → 多源下载到启动器目录 → 重启启动器自动安装。
    /// </summary>
    private async void BtnPack_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            // 请求梦之韵官方整合包 API（地址随作者更新自动变化，每次现拉）
            var data = await HttpRequest.GetJsonAsync<LiminalPackModel>("https://apc.camzy.uno/mc/mzy?download");
            var link = data?.Link ?? "";
            var size = data?.Size ?? 0L;
            var type = string.IsNullOrEmpty(data?.Type) ? "zip" : data.Type;
            if (string.IsNullOrWhiteSpace(link))
            {
                ModMain.MyMsgBox("服务器暂未开放整合包，请稍后再试。", "梦之韵Pro", isWarn: true);
                return;
            }
            var fileName = System.IO.Path.GetFileName(new Uri(link).LocalPath);
            var sizeMb = size / 1024.0 / 1024.0;
            ModMain.MyMsgBox(
                $"官方整合包：{fileName}\n大小：{sizeMb:F1} MB\n类型：{type}\n\n点击开始下载，完成后下次启动启动器自动安装。",
                "梦之韵Pro · 一键下载整合包",
                button2: "开始下载",
                button2Action: () => _ = DownloadPackUrlAsync(link, fileName));
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 拉取整合包失败");
            ModMain.MyMsgBox("获取整合包信息失败：" + ex.Message, "梦之韵Pro", isWarn: true);
        }
    }

    private async Task DownloadPackUrlAsync(string url, string fileName)
    {
        var target = Path.Combine(ModBase.exePath, "modpack.zip");
        var partPath = target + ".part";
        try
        {
            File.Delete(partPath);
            await FileDownloader.DownloadAsync(url, partPath, useBrowserUserAgent: true).WaitAsync(TimeSpan.FromMinutes(30));
            if (File.Exists(partPath) && new FileInfo(partPath).Length > 0)
            {
                File.Move(partPath, target, true);
                var sizeMb = new FileInfo(target).Length / 1024.0 / 1024.0;
                // 梦之韵Pro：自动创建/复用「梦之韵Pro」实例，解压安装（一键，无需手动建实例）
                var inst = ModInstanceList.McMcInstanceSelected;
                var instDir = inst is null
                    ? Path.Combine(ModFolder.mcFolderSelected, "versions", "梦之韵Pro")
                    : inst.PathInstance;
                Directory.CreateDirectory(instDir);
                ZipFile.ExtractToDirectory(target, instDir, overwriteFiles: true);
                ModInstanceList.mcInstanceListForceRefresh = true; // 刷新实例列表，让新实例出现
                ModMain.MyMsgBox($"整合包 {fileName} 已安装到实例「{(inst is null ? "梦之韵Pro" : "当前实例")}」（{sizeMb:F1} MB）。\n回「启动」页选中该实例启动即可。",
                    "梦之韵Pro · 整合包安装", button2: "知道了");
            }
            else
                ModMain.MyMsgBox("整合包下载失败：文件为空。", "梦之韵Pro", isWarn: true);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 整合包下载失败");
            ModMain.MyMsgBox("整合包下载失败：" + ex.Message, "梦之韵Pro", isWarn: true);
        }
        finally
        {
            if (File.Exists(partPath)) { try { File.Delete(partPath); } catch { } }
        }
    }

    private async Task DownloadAndInstallPackAsync(PackInfoModel pack)
    {
        var target = Path.Combine(ModBase.exePath, "modpack.zip");
        var partPath = target + ".part";
        try
        {
            var sources = pack.Sources?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];
            if (sources.Count == 0) sources = [pack.Download];
            var ok = false;
            string? lastError = null;
            foreach (var source in sources)
            {
                try
                {
                    File.Delete(partPath);
                    await FileDownloader.DownloadAsync(source, partPath, useBrowserUserAgent: true).WaitAsync(TimeSpan.FromMinutes(30));
                    if (File.Exists(partPath) && new FileInfo(partPath).Length > 0)
                    {
                        File.Move(partPath, target, true);
                        ok = true;
                        break;
                    }
                    lastError = "下载文件为空";
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
                finally
                {
                    if (File.Exists(partPath))
                    {
                        try { File.Delete(partPath); } catch { /* 忽略 */ }
                    }
                }
            }

            if (!ok)
            {
                ModMain.MyMsgBox("整合包下载失败：" + lastError + "\n可稍后重试或到官网手动下载。", "梦之韵Pro", isWarn: true);
                return;
            }

            ModMain.MyMsgBox("整合包下载完成，将在下次启动启动器时自动安装。\n你也可以手动重启启动器立即安装。",
                "梦之韵Pro · 整合包安装",
                button2: "知道了");
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 整合包下载失败");
            ModMain.MyMsgBox("整合包下载失败：" + ex.Message, "梦之韵Pro", isWarn: true);
        }
    }

    private void BtnTutorial_Click(object sender, MouseButtonEventArgs e)
    {
        // 新手教程入口：打开玩家门户（登录/换肤/新手指引）
        ModBase.OpenWebsite("https://player.camzy.uno");
    }

    // ================= AI 助手 =================
    private async void BtnAiAsk_Click(object sender, MouseButtonEventArgs e) => await AiAskAsync(false);

    private async void BtnAiAnalyzeLog_Click(object sender, MouseButtonEventArgs e) => await AiAskAsync(true);

    /// <summary>
    ///     AI 助手：POST {ApiBase}/api/ai/chat，云端（CF Worker）转发智谱免费模型。
    ///     支持普通问答与错误日志分析。
    /// </summary>
    private async Task AiAskAsync(bool preferLog)
    {
        var question = InAiQuestion.Text?.Trim() ?? "";
        var log = InAiLog.Text?.Trim() ?? "";
        if (preferLog && log.Length == 0)
        {
            ModMain.MyMsgBox("请先在上方粘贴要分析的错误日志。", "梦之韵Pro", isWarn: true);
            return;
        }
        if (!preferLog && question.Length == 0 && log.Length == 0)
        {
            ModMain.MyMsgBox("请输入问题，或粘贴错误日志后点「分析错误日志」。", "梦之韵Pro", isWarn: true);
            return;
        }

        BtnAiAsk.IsEnabled = false;
        BtnAiAnalyzeLog.IsEnabled = false;
        TextAiResult.Text = "AI 正在思考，请稍候…";
        try
        {
            using var hc = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            var res = await hc.PostAsJsonAsync(ApiBase + "/api/ai/chat", new AiChatModel { Message = question, Log = log });
            var obj = await res.Content.ReadFromJsonAsync<AiChatResultModel>();
            if (obj is not null && !string.IsNullOrWhiteSpace(obj.Answer))
                TextAiResult.Text = obj.Answer;
            else
                TextAiResult.Text = "AI 暂时繁忙，请稍后再试。";
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] AI 助手请求失败");
            TextAiResult.Text = "网络异常，无法连接 AI 助手：" + ex.Message;
        }
        finally
        {
            BtnAiAsk.IsEnabled = true;
            BtnAiAnalyzeLog.IsEnabled = true;
        }
    }

    public class LiminalPackModel
    {
        [JsonPropertyName("link")] public string? Link { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
    }

    public class SrvStatusModel
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("online")] public bool Online { get; set; }
        [JsonPropertyName("ping")] public long? Ping { get; set; }
        [JsonPropertyName("players_max")] public long? PlayersMax { get; set; }
    }
    public class SrvUptimeModel
    {
        [JsonPropertyName("uptime_seconds")] public long UptimeSeconds { get; set; }
        [JsonPropertyName("online_count")] public long? OnlineCount { get; set; }
    }
    public class SrvRankingModel
    {
        [JsonPropertyName("ranking")] public List<SrvRankItem>? Ranking { get; set; }
    }
    public class SrvRankItem
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("seconds")] public long Seconds { get; set; }
    }

    public class AiChatModel
    {
        [JsonPropertyName("message")] public string? Message { get; set; }

        [JsonPropertyName("log")] public string? Log { get; set; }
    }

    public class AiChatResultModel
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }

        [JsonPropertyName("answer")] public string? Answer { get; set; }
    }

    /// <summary>
    ///     从轻量 API 仓库拉取更新日志并展示。
    ///     接口：GET {ApiBase}/changelog.json，返回 {"launcher":"...","changelog":[{"version":"...","date":"...","title":"...","content":["..."]}]}
    /// </summary>
    private async void LoadChangelogAsync()
    {
        try
        {
            var changelog = await HttpRequest.GetJsonAsync<ChangelogModel>(ApiBase + "/changelog.json");
            if (changelog is not null && changelog.Entries is { Count: > 0 } && !string.IsNullOrWhiteSpace(changelog.Entries[0].ContentText))
            {
                var entry = changelog.Entries[0];
                TextChangelogVersion.Text = Lang.Text("Setup.Service.Changelog.Version", entry.Version ?? "未知版本");
                TextChangelogContent.Text = entry.ContentText;
            }
            else
            {
                TextChangelogVersion.Text = Lang.Text("Setup.Service.Changelog.Unavailable");
                TextChangelogContent.Text = Lang.Text("Setup.Service.Changelog.UnavailableDesc");
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 拉取更新日志失败");
            TextChangelogVersion.Text = Lang.Text("Setup.Service.Changelog.Failed");
            TextChangelogContent.Text = Lang.Text("Setup.Service.Changelog.FailedDesc");
        }
    }

    /// <summary>
    ///     拉取远程配置：功能开关 + 版本更新检查。
    ///     接口：GET {ApiBase}/api/launcher/config
    /// </summary>
    private async void LoadRemoteConfigAsync()
    {
        try
        {
            var cfg = await HttpRequest.GetJsonAsync<LauncherConfigModel>(ApiBase + "/api/launcher/config");
            if (cfg is null)
                return;

            // 功能开关
            if (cfg.Features is not null)
            {
                ModAutoUpdater.IsEnabled = cfg.Features.ModUpdater;
                BtnPack.IsEnabled = cfg.Features.PackInstaller;
                if (!cfg.Features.ModUpdater)
                {
                    BtnMods.ToolTip = "模组更新功能暂未开放";
                }
            }

            // 版本更新检查
            var localVersion = GetLocalVersion();
            if (!string.IsNullOrWhiteSpace(cfg.Version) && !string.IsNullOrWhiteSpace(localVersion)
                && !cfg.Version.Equals(localVersion, StringComparison.OrdinalIgnoreCase)
                && cfg.Version.TrimStart('v', 'V') != localVersion.TrimStart('v', 'V'))
            {
                var url = cfg.ReleasePage;
                ModMain.MyMsgBox(
                    $"发现新版本 {cfg.Version}（当前 {localVersion}）。\n本次更新内容可在「服务」页更新日志查看。",
                    "梦之韵Pro 更新",
                    button2: "前往下载",
                    button2Action: () => { if (!string.IsNullOrWhiteSpace(url)) ModBase.OpenWebsite(url); });
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 拉取远程配置失败");
        }
    }

    /// <summary>
    ///     拉取弹窗公告并展示（每次启动一次）。
    ///     接口：GET {ApiBase}/api/notice
    /// </summary>
    private async void LoadNoticeAsync()
    {
        try
        {
            var notice = await HttpRequest.GetJsonAsync<NoticeModel>(ApiBase + "/api/notice");
            if (notice is null || !notice.Enabled || string.IsNullOrWhiteSpace(notice.Title) || string.IsNullOrWhiteSpace(notice.Content))
                return;

            var url = notice.ButtonUrl;
            ModMain.MyMsgBox(notice.Content, notice.Title,
                button2: string.IsNullOrWhiteSpace(notice.ButtonText) ? "确定" : notice.ButtonText,
                button2Action: () => { if (!string.IsNullOrWhiteSpace(url)) ModBase.OpenWebsite(url); });
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 拉取公告失败");
        }
    }

    private static string GetLocalVersion()
    {
        // 梦之韵Pro：与发布版本号保持一致（如 v2.15.11-mzy.11），避免更新误判
        try
        {
            var name = ModBase.versionBaseName;
            return string.IsNullOrWhiteSpace(name) ? "" : "v" + name;
        }
        catch
        {
            return "";
        }
    }

    public class ChangelogModel
    {
        [JsonPropertyName("launcher")] public string? Launcher { get; set; }

        [JsonPropertyName("changelog")] public List<ChangelogEntry>? Entries { get; set; }
    }

    public class ChangelogEntry
    {
        [JsonPropertyName("version")] public string? Version { get; set; }

        [JsonPropertyName("date")] public string? Date { get; set; }

        [JsonPropertyName("title")] public string? Title { get; set; }

        [JsonPropertyName("content")] public List<string>? Content { get; set; }

        [JsonIgnore]
        public string ContentText => Content is null ? string.Empty : string.Join("\n", Content);
    }

    public class LauncherConfigModel
    {
        [JsonPropertyName("version")] public string? Version { get; set; }

        [JsonPropertyName("minimum_version")] public string? MinimumVersion { get; set; }

        [JsonPropertyName("mandatory_update")] public bool MandatoryUpdate { get; set; }

        [JsonPropertyName("downloads")] public Dictionary<string, string>? Downloads { get; set; }

        [JsonPropertyName("release_page")] public string? ReleasePage { get; set; }

        [JsonPropertyName("features")] public FeatureFlagsModel? Features { get; set; }
    }

    public class FeatureFlagsModel
    {
        [JsonPropertyName("mod_updater")] public bool ModUpdater { get; set; } = true;

        [JsonPropertyName("notice_popup")] public bool NoticePopup { get; set; } = true;

        [JsonPropertyName("server_status")] public bool ServerStatus { get; set; } = true;

        [JsonPropertyName("pack_installer")] public bool PackInstaller { get; set; } = true;
    }

    public class NoticeModel
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; }

        [JsonPropertyName("title")] public string? Title { get; set; }

        [JsonPropertyName("content")] public string? Content { get; set; }

        [JsonPropertyName("button_text")] public string? ButtonText { get; set; }

        [JsonPropertyName("button_url")] public string? ButtonUrl { get; set; }

        [JsonPropertyName("once_per_session")] public bool OncePerSession { get; set; }
    }

    public class PackListModel
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }

        [JsonPropertyName("total")] public int Total { get; set; }

        [JsonPropertyName("packs")] public List<PackInfoModel>? Packs { get; set; }
    }

    public class PackInfoModel
    {
        [JsonPropertyName("id")] public string? Id { get; set; }

        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("version")] public string? Version { get; set; }

        [JsonPropertyName("minecraft")] public string? Minecraft { get; set; }

        [JsonPropertyName("loader")] public string? Loader { get; set; }

        [JsonPropertyName("download")] public string? Download { get; set; }

        [JsonPropertyName("sources")] public List<string>? Sources { get; set; }

        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}
