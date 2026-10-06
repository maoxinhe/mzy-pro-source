using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.Utils;
using PCL.Network;

namespace PCL;

/// <summary>
/// 梦之韵Pro 云端 API 客户端 —— 集中调用 apc.camzy.uno 所有端点。
/// 所有方法异步，失败返回 null/空列表，不影响启动器主流程。
/// </summary>
public static class MzyApi
{
    private const string Base = "https://apc.camzy.uno";

    // ---- 服务器状态 ----
    public class ServerStatus
    {
        public bool Online; public int Players; public int Max; public int Ping; public string Motd = "";
    }
    public static async Task<ServerStatus?> GetServerStatusAsync()
    {
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/status", RequestParam.WithRetry);
            if (j == null) return null;
            return new ServerStatus
            {
                Online = j["online"]?.GetValue<bool>() ?? false,
                Players = j["players_online"]?.GetValue<int>() ?? 0,
                Max = j["players_max"]?.GetValue<int>() ?? 0,
                Ping = j["ping"]?.GetValue<int>() ?? 0
            };
        }
        catch { return null; }
    }

    // ---- 服务器详情 ----
    public class ServerInfo
    {
        public string Name = "", Address = "", Version = "", Mode = "", OpenSince = "";
    }
    public static async Task<ServerInfo?> GetServerInfoAsync()
    {
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/server/info", RequestParam.WithRetry);
            if (j == null) return null;
            return new ServerInfo
            {
                Name = j["name"]?.ToString() ?? "",
                Address = j["address"]?.ToString() ?? "",
                Version = j["version"]?.ToString() ?? "",
                Mode = j["mode"]?.ToString() ?? "",
                OpenSince = j["open_since"]?.ToString() ?? ""
            };
        }
        catch { return null; }
    }

    // ---- 公告 ----
    public class Announcement
    {
        public string Title = "", Content = "", Date = "";
    }
    public static async Task<Announcement?> GetActiveAnnounceAsync()
    {
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/announce/active", RequestParam.WithRetry);
            var cur = j?["current"];
            if (cur == null) return null;
            return new Announcement
            {
                Title = cur["title"]?.ToString() ?? "",
                Content = cur["content"]?.ToString() ?? "",
                Date = cur["start"]?.ToString() ?? ""
            };
        }
        catch { return null; }
    }

    // ---- 更新日志 ----
    public class ChangeLog
    {
        public string Version = "", Date = "", Notes = "";
    }
    public static async Task<List<ChangeLog>> GetChangeLogAsync()
    {
        var list = new List<ChangeLog>();
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/changelog", RequestParam.WithRetry);
            var arr = j?["items"] as JsonArray;
            if (arr == null) return list;
            foreach (var item in arr)
            {
                list.Add(new ChangeLog
                {
                    Version = item["version"]?.ToString() ?? "",
                    Date = item["date"]?.ToString() ?? "",
                    Notes = item["notes"]?.ToString() ?? ""
                });
            }
        }
        catch { }
        return list;
    }

    // ---- FAQ ----
    public class FaqItem { public string Q = "", A = ""; }
    public static async Task<List<FaqItem>> GetFaqAsync()
    {
        var list = new List<FaqItem>();
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/faq", RequestParam.WithRetry);
            var arr = j?["items"] as JsonArray;
            if (arr == null) return list;
            foreach (var item in arr)
                list.Add(new FaqItem { Q = item["q"]?.ToString() ?? "", A = item["a"]?.ToString() ?? "" });
        }
        catch { }
        return list;
    }

    // ---- 快捷链接 ----
    public class Links
    {
        public string Qq = "", QqGroup = "", Telegram = "", Website = "", Forum = "";
    }
    public static async Task<Links?> GetLinksAsync()
    {
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/links", RequestParam.WithRetry);
            var l = j?["links"];
            if (l == null) return null;
            return new Links
            {
                Qq = l["qq"]?.ToString() ?? "",
                QqGroup = l["qq_group"]?.ToString() ?? "",
                Telegram = l["telegram"]?.ToString() ?? "",
                Website = l["website"]?.ToString() ?? "",
                Forum = l["forum"]?.ToString() ?? ""
            };
        }
        catch { return null; }
    }

    // ---- 在线玩家列表 ----
    public static async Task<(int online, int max, List<string> players)> GetOnlinePlayersAsync()
    {
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/players/online", RequestParam.WithRetry);
            var list = new List<string>();
            var arr = j?["list"] as JsonArray;
            if (arr != null) foreach (var p in arr) list.Add(p.ToString());
            return (j?["online"]?.GetValue<int>() ?? 0, j?["max"]?.GetValue<int>() ?? 0, list);
        }
        catch { return (0, 0, new List<string>()); }
    }

    // ---- 新玩家欢迎 ----
    public class Welcome { public string Title = "", Message = ""; public List<string> QuickStart = new(); }
    public static async Task<Welcome?> GetWelcomeAsync()
    {
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/welcome", RequestParam.WithRetry);
            if (j == null) return null;
            var w = new Welcome { Title = j["title"]?.ToString() ?? "", Message = j["message"]?.ToString() ?? "" };
            var arr = j?["quick_start"] as JsonArray;
            if (arr != null) foreach (var s in arr) w.QuickStart.Add(s.ToString());
            return w;
        }
        catch { return null; }
    }

    // ---- 常用命令 ----
    public static async Task<List<(string cmd, string desc)>> GetCommandsAsync()
    {
        var list = new List<(string, string)>();
        try
        {
            var j = await Requester.FetchJsonAsync<JsonObject>(Base + "/api/server/commands", RequestParam.WithRetry);
            var arr = j?["commands"] as JsonArray;
            if (arr == null) return list;
            foreach (var c in arr) list.Add((c["cmd"]?.ToString() ?? "", c["desc"]?.ToString() ?? ""));
        }
        catch { }
        return list;
    }

    // ---- 计数器（页面访问）----
    public static async void BumpCounter(string name)
    {
        try { await Requester.FetchJsonAsync<JsonObject>(Base + "/api/counter/" + name, RequestParam.WithRetry); }
        catch { }
    }

    // ---- 错误上报 ----
    public static async void ReportError(string error, string version)
    {
        try
        {
            await Requester.FetchJsonAsync<JsonObject>(Base + "/error/report",
                new RequestParam { Method = "POST", Body = $"{{\"error\":\"{error}\",\"version\":\"{version}\"}}" });
        }
        catch { }
    }
}
