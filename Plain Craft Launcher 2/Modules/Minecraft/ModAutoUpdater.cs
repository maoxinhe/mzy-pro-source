using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Network;

namespace PCL.Modules.Minecraft;

/// <summary>
///     模组自动更新器。
///     移植自 ModAutoUpdater（作者 liminalily），用于在用户 mods 文件夹内下载/更新模组：
///     1. 从 API 平台拉取模组清单（files 元素可为字符串或 {name, sources} 对象）；
///     2. 对比本地 mods 目录，下载缺失模组（多源依次尝试）；
///     3. 删除清单中标记移除的多余模组。
/// </summary>
public static class ModAutoUpdater
{
    // ===== 服务端配置（可按需修改）=====
    public const string ApiBase = "https://apc.camzy.uno"; // API 平台地址
    public const string ManifestPath = "/api/mods"; // 模组清单接口（含多源下载直链）

    /// <summary>模组更新功能总开关（由远程配置控制）。</summary>
    public static bool IsEnabled { get; set; } = true;

    public const int Concurrency = 5; // 并发下载数
    public const int MaxRetries = 3; // 单源重试次数

    /// <summary>更新结果。</summary>
    public class UpdateResult
    {
        public List<string> Missing { get; } = []; // 待下载
        public List<string> Downloaded { get; } = []; // 下载成功
        public List<string> Failed { get; } = []; // 下载失败
        public List<string> Deleted { get; } = []; // 删除成功
        public List<string> DeleteFailed { get; } = []; // 删除失败
        public string? Error { get; set; } // 致命错误
    }

    /// <summary>清单中的单个模组条目。</summary>
    private class ModEntry
    {
        public string Name { get; init; } = "";
        public List<string> Sources { get; init; } = [];
    }

    /// <summary>获取实例对应的 mods 文件夹（应用版本隔离）。</summary>
    public static string GetModsFolder(McInstance instance)
    {
        return Path.Combine(instance.PathIndie, "mods");
    }

    /// <summary>
    ///     执行一次模组更新。
    /// </summary>
    /// <param name="modsFolder">目标 mods 文件夹。</param>
    /// <param name="progress">进度回调（当前文件数, 总数, 当前文件名, 状态描述）。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task<UpdateResult> RunAsync(string modsFolder,
        IProgress<(int current, int total, string name, string status)>? progress = null,
        CancellationToken ct = default)
    {
        var result = new UpdateResult();

        // 1. 拉取清单
        string manifest;
        try
        {
            var url = $"https://{ApiBase}{ManifestPath}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            manifest = await HttpRequest.GetStringAsync(url).WaitAsync(TimeSpan.FromSeconds(45), ct);
        }
        catch (Exception ex)
        {
            result.Error = Lang.Text("Setup.Service.Mods.FetchManifestFailed") + "\n" + ex.Message;
            return result;
        }

        if (string.IsNullOrWhiteSpace(manifest))
        {
            result.Error = Lang.Text("Setup.Service.Mods.ManifestEmpty");
            return result;
        }

        // 2. 解析清单（兼容：纯字符串数组 / {"files":[...], "delete":[...]}，files 元素可为字符串或对象）
        List<ModEntry> remoteFiles = [];
        List<string> deleteFiles = [];
        try
        {
            var node = JsonNode.Parse(manifest);
            if (node is JsonArray arr)
            {
                remoteFiles = arr.Select(x => ParseEntry(x)).Where(x => x is not null).Cast<ModEntry>().ToList();
            }
            else if (node is JsonObject obj)
            {
                if (obj["files"] is JsonArray fArr)
                    remoteFiles = fArr.Select(x => ParseEntry(x)).Where(x => x is not null).Cast<ModEntry>().ToList();
                foreach (var key in new[] { "delete", "remove" })
                {
                    if (obj[key] is JsonArray dArr)
                        deleteFiles = dArr.Select(x => x?.GetValue<string>() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            result.Error = Lang.Text("Setup.Service.Mods.ManifestInvalid") + "\n" + ex.Message;
            return result;
        }

        if (remoteFiles.Count == 0 && deleteFiles.Count == 0)
        {
            result.Error = Lang.Text("Setup.Service.Mods.ManifestEmpty");
            return result;
        }

        // 3. 对比本地
        Directory.CreateDirectory(modsFolder);
        foreach (var entry in remoteFiles)
        {
            var p = Path.Combine(modsFolder, entry.Name);
            if (!File.Exists(p))
                result.Missing.Add(entry.Name);
        }

        // 4. 下载缺失模组（并发，多源依次尝试）
        if (result.Missing.Count > 0)
        {
            using var sem = new SemaphoreSlim(Concurrency);
            var tasks = result.Missing.Select(async name =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var entry = remoteFiles.FirstOrDefault(x => x.Name == name);
                    var sources = entry?.Sources is { Count: > 0 }
                        ? entry.Sources
                        : [GetDefaultSource(name)];
                    var destPath = Path.Combine(modsFolder, name);
                    var partPath = destPath + ".part";
                    var ok = false;
                    string? lastError = null;

                    foreach (var source in sources)
                    {
                        if (ct.IsCancellationRequested)
                            break;
                        for (var attempt = 0; attempt < MaxRetries && !ok; attempt++)
                        {
                            if (attempt > 0)
                                await Task.Delay(TimeSpan.FromSeconds(1 << (attempt - 1)), ct);
                            try
                            {
                                File.Delete(partPath);
                                await FileDownloader.DownloadAsync(source, partPath, useBrowserUserAgent: true).WaitAsync(TimeSpan.FromMinutes(15), ct);
                                if (File.Exists(partPath) && new FileInfo(partPath).Length > 0)
                                {
                                    File.Move(partPath, destPath, true);
                                    ok = true;
                                    break;
                                }
                                lastError = Lang.Text("Setup.Service.Mods.DownloadEmpty");
                            }
                            catch (Exception ex)
                            {
                                lastError = ex.Message;
                            }
                            finally
                            {
                                if (File.Exists(partPath))
                                {
                                    try { File.Delete(partPath); }
                                    catch { /* 忽略清理错误 */ }
                                }
                            }
                        }
                        if (ok)
                            break;
                    }

                    if (ok)
                    {
                        result.Downloaded.Add(name);
                        progress?.Report((result.Downloaded.Count, result.Missing.Count, name, Lang.Text("Setup.Service.Mods.Status.Done")));
                    }
                    else
                    {
                        result.Failed.Add(name + (lastError is null ? "" : "：" + lastError));
                        progress?.Report((result.Downloaded.Count + result.Failed.Count, result.Missing.Count, name, Lang.Text("Setup.Service.Mods.Status.Failed")));
                    }
                }
                finally
                {
                    sem.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }

        // 5. 删除多余模组
        if (deleteFiles.Count > 0)
        {
            var rootFull = Path.GetFullPath(modsFolder);
            foreach (var name in deleteFiles)
            {
                var p = Path.Combine(modsFolder, name);
                var pFull = Path.GetFullPath(p);

                // 安全校验：仅允许删除 mods 文件夹内的普通文件
                if (!pFull.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                {
                    result.DeleteFailed.Add(name);
                    continue;
                }

                try
                {
                    if (File.Exists(pFull))
                    {
                        File.Delete(pFull);
                        result.Deleted.Add(name);
                    }
                }
                catch (Exception ex)
                {
                    result.DeleteFailed.Add(name + "：" + ex.Message);
                }
            }
        }

        return result;
    }

    /// <summary>解析清单中的单个条目（字符串或 {name, sources} 对象）。</summary>
    private static ModEntry? ParseEntry(JsonNode? x)
    {
        if (x is null)
            return null;
        if (x is JsonValue val)
        {
            var s = val.GetValue<string>();
            return string.IsNullOrWhiteSpace(s) ? null : new ModEntry { Name = s };
        }
        if (x is JsonObject obj)
        {
            var name = obj["name"]?.GetValue<string>() ?? "";
            if (string.IsNullOrWhiteSpace(name))
                return null;
            var sources = new List<string>();
            if (obj["sources"] is JsonArray sArr)
            {
                foreach (var s in sArr)
                {
                    var url = s?.GetValue<string>() ?? "";
                    if (!string.IsNullOrWhiteSpace(url) && !sources.Contains(url))
                        sources.Add(url);
                }
            }
            return new ModEntry { Name = name, Sources = sources };
        }
        return null;
    }

    /// <summary>无 sources 时的默认下载地址（GitHub raw，与清单数据仓库一致）。</summary>
    private static string GetDefaultSource(string name)
    {
        return "https://cdn.jsdelivr.net/gh/maoxinhe/mzy-api@main/mods/" + Uri.EscapeDataString(name);
    }
}
