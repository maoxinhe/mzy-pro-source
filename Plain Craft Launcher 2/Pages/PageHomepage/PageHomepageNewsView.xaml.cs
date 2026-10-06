using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PCL.Core.Minecraft.Profile;
using PCL.Core.ViewModel.Homepage;

namespace PCL;

public partial class PageHomepageNewsView : MyPageRight
{
    private const string ApiBase = "https://apc.camzy.uno";
    private readonly HttpClient _hc = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer _timer;

    public PageHomepageNewsView()
    {
        InitializeComponent();
        DataContext = new NewsViewModel();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _timer.Tick += (_, _) => LoadStatus();
        Loaded += (_, _) =>
        {
            LoadStatus();
            _timer.Start();
        };
        Unloaded += (_, _) => _timer.Stop();

        BtnRefreshStatus.Click += (_, _) => LoadStatus();
        BtnOpenAi.Click += (_, _) => new AiToolsWindow().ShowDialog();
        BtnOpenModCheck.Click += (_, _) => OpenTool("modcheck");
        BtnOpenMemAdvice.Click += (_, _) => OpenTool("memadvice");
        BtnCheckin.Click += async (_, _) => await DoCheckin();
    }

    // ================= 每日签到 =================
    private async System.Threading.Tasks.Task DoCheckin()
    {
        try
        {
            var name = ProfileService.Current?.UserName ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                ModMain.MyMsgBox("请先登录正版账号再签到。", "梦之韵Pro");
                return;
            }
            var res = await _hc.PostAsJsonAsync(ApiBase + "/api/checkin", new { name });
            var obj = await res.Content.ReadFromJsonAsync<CheckinResult>();
            if (obj?.Ok == true)
            {
                ModMain.MyMsgBox($"签到成功！连续签到 {obj.Streak} 天。奖励已发放到游戏内，记得进服领取哦~", "梦之韵Pro");
            }
            else
            {
                ModMain.MyMsgBox(obj?.Error ?? "今天已经签到过啦", "梦之韵Pro");
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 签到失败");
            ModMain.MyMsgBox("网络异常，签到失败。", "梦之韵Pro");
        }
    }

    public class CheckinResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("ok")] public bool Ok { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("streak")] public int Streak { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("error")] public string? Error { get; set; }
    }

    // ================= 服务器状态 + 玩家数据 =================
    private async void LoadStatus()
    {
        try
        {
            // 并行拉取服务器状态、开服信息、玩家数据
            var statusTask = _hc.GetFromJsonAsync<StatusResult>(ApiBase + "/api/status");
            var uptimeTask = _hc.GetFromJsonAsync<UptimeResult>(ApiBase + "/api/uptime");
            var name = ProfileService.Current?.UserName ?? "";
            var statsTask = !string.IsNullOrWhiteSpace(name)
                ? _hc.GetFromJsonAsync<PlayerStatsResult>(ApiBase + "/api/player/stats?name=" + Uri.EscapeDataString(name))
                : null;

            var status = await statusTask;
            var uptime = await uptimeTask;
            var stats = statsTask is null ? null : await statsTask;

            TxtOnlineCount.Text = status?.Online == true ? $"{status.PlayersOnline} / {status.PlayersMax}" : "离线";
            TxtPing.Text = status?.Ping is > 0 ? status.Ping + " ms" : "--";
            TxtUptime.Text = uptime is not null && uptime.UptimeSeconds > 0
                ? FormatDuration(uptime.UptimeSeconds)
                : "--";

            // 在线玩家列表
            var players = uptime?.OnlinePlayers ?? [];
            PanOnlinePlayers.Children.Clear();
            TxtNoPlayers.Visibility = players.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            foreach (var p in players.Take(12))
            {
                PanOnlinePlayers.Children.Add(new TextBlock
                {
                    Text = p,
                    Margin = new Thickness(0, 0, 10, 4),
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B3665C")),
                    FontWeight = FontWeights.SemiBold
                });
            }

            // 玩家数据
            if (stats is not null)
            {
                TxtPlayerName.Text = stats.Name;
                TxtPlayTime.Text = FormatDuration(stats.PlaytimeSeconds);
                TxtWhitelist.Text = stats.Whitelisted ? "已通过" : "未申请";
                TxtWhitelist.Foreground = stats.Whitelisted
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7BA05B"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B3665C"));
                TxtOnlineState.Text = stats.Online ? "在线" : "离线";
                TxtOnlineState.Foreground = stats.Online
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7BA05B"))
                    : (Brush)FindResource("ColorBrushGray4");
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 首页状态拉取失败");
        }
    }

    // ================= 快捷工具 =================
    private void OpenTool(string mode)
    {
        var win = new AiToolsWindow();
        if (mode == "modcheck")
            win.RunModCheck();
        else if (mode == "memadvice")
            win.RunMemAdvice();
        win.ShowDialog();
    }

    private static string FormatDuration(long seconds)
    {
        if (seconds < 60) return seconds + " 秒";
        var h = seconds / 3600;
        var m = seconds % 3600 / 60;
        if (h > 0) return h + " 小时 " + m + " 分";
        return m + " 分钟";
    }

    // ================= 数据模型 =================
    public class StatusResult
    {
        [JsonPropertyName("online")] public bool Online { get; set; }
        [JsonPropertyName("players_online")] public int PlayersOnline { get; set; }
        [JsonPropertyName("players_max")] public int PlayersMax { get; set; }
        [JsonPropertyName("ping")] public long? Ping { get; set; }
    }

    public class UptimeResult
    {
        [JsonPropertyName("uptime_seconds")] public long UptimeSeconds { get; set; }
        [JsonPropertyName("online_players")] public string[] OnlinePlayers { get; set; } = [];
    }

    public class PlayerStatsResult
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("playtime_seconds")] public long PlaytimeSeconds { get; set; }
        [JsonPropertyName("whitelisted")] public bool Whitelisted { get; set; }
        [JsonPropertyName("online")] public bool Online { get; set; }
    }
}
