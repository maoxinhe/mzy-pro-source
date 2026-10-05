using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using PCL.Core.Minecraft.Profile;

namespace PCL;

public partial class AiToolsWindow : Window
{
    private const string ApiBase = "https://apc.camzy.uno";
    private readonly HttpClient _hc = new() { Timeout = TimeSpan.FromSeconds(90) };

    public AiToolsWindow()
    {
        InitializeComponent();
        Owner = ModMain.frmMain;
    }

    // ================= AI 问答 =================
    private async void BtnSend_Click(object sender, RoutedEventArgs e) => await AskAsync(InQuestion.Text);

    private async void InQuestion_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await AskAsync(InQuestion.Text);
    }

    private async System.Threading.Tasks.Task AskAsync(string raw)
    {
        var message = (raw ?? "").Trim();
        if (message.Length == 0 || message == "问我任何问题：报错、模组、服务器…")
        {
            ModMain.MyMsgBox("请输入要问的问题。", "梦之韵Pro", isWarn: true);
            return;
        }
        SetBusy(true, "AI 正在思考，请稍候…");
        try
        {
            var res = await _hc.PostAsJsonAsync(ApiBase + "/api/ai/chat", new AiChatBody { Message = message });
            var obj = await res.Content.ReadFromJsonAsync<AiChatResult>();
            TxtResult.Text = obj is not null && !string.IsNullOrWhiteSpace(obj.Answer)
                ? obj.Answer
                : "AI 暂时繁忙，请稍后再试。";
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] AI 助手请求失败");
            TxtResult.Text = "网络异常，无法连接 AI 助手：" + ex.Message;
        }
        finally
        {
            SetBusy(false, "");
        }
    }

    // ================= 模组冲突检测 =================
    private async void BtnModCheck_Click(object sender, RoutedEventArgs e) => await RunModCheckAsync();

    public async System.Threading.Tasks.Task RunModCheck() => await RunModCheckAsync();

    private async System.Threading.Tasks.Task RunModCheckAsync()
    {
        var mods = FindMods();
        if (mods.Length == 0)
        {
            ModMain.MyMsgBox("未在当前版本找到模组文件夹（mods）。请先选择安装过模组的版本。", "梦之韵Pro", isWarn: true);
            return;
        }
        SetBusy(true, $"检测到 {mods.Length} 个模组，AI 正在分析冲突…");
        try
        {
            var res = await _hc.PostAsJsonAsync(ApiBase + "/api/tools/modcheck", new ModCheckBody { Mods = mods });
            var obj = await res.Content.ReadFromJsonAsync<ToolResult>();
            TxtResult.Text = obj is not null && !string.IsNullOrWhiteSpace(obj.Analysis)
                ? $"检测到 {obj.Total} 个模组，AI 分析结果：\n\n" + obj.Analysis
                : "AI 暂时繁忙，请稍后再试。";
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 模组检测请求失败");
            TxtResult.Text = "网络异常：" + ex.Message;
        }
        finally
        {
            SetBusy(false, "");
        }
    }

    private string[] FindMods()
    {
        try
        {
            var mcFolder = GetMcFolder();
            if (string.IsNullOrEmpty(mcFolder) || !Directory.Exists(mcFolder)) return [];
            var modsDir = Path.Combine(mcFolder, "mods");
            if (!Directory.Exists(modsDir)) return [];
            return Directory.GetFiles(modsDir, "*.jar")
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Take(400)
                .ToArray()!;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 读取模组列表失败");
            return [];
        }
    }

    private static string? GetMcFolder()
    {
        try
        {
            var st = (dynamic)typeof(ModMain).Assembly.GetType("PCL.States")!
                .GetProperty("Game")!.GetValue(null)!;
            var folder = st.SelectedFolder?.ToString();
            if (string.IsNullOrEmpty(folder)) return null;
            return folder.Replace("$", ModBase.exePath);
        }
        catch
        {
            return null;
        }
    }

    // ================= 内存优化建议 =================
    private async void BtnMemAdvice_Click(object sender, RoutedEventArgs e) => await RunMemAdviceAsync();

    public async System.Threading.Tasks.Task RunMemAdvice() => await RunMemAdviceAsync();

    private async System.Threading.Tasks.Task RunMemAdviceAsync()
    {
        var sysMb = GetSystemMemoryMb();
        if (sysMb <= 0)
        {
            ModMain.MyMsgBox("无法读取本机内存。", "梦之韵Pro", isWarn: true);
            return;
        }
        var modCount = FindMods().Length;
        var version = GetGameVersion();
        SetBusy(true, "AI 正在分析内存配置…");
        try
        {
            var res = await _hc.PostAsJsonAsync(ApiBase + "/api/tools/memadvice", new MemAdviceBody
            {
                SystemMb = sysMb,
                AllocatedMb = -1,
                ModCount = modCount,
                Version = version
            });
            var obj = await res.Content.ReadFromJsonAsync<ToolResult>();
            TxtResult.Text = obj is not null && !string.IsNullOrWhiteSpace(obj.Analysis)
                ? $"本机内存 {sysMb} MB · 当前模组 {modCount} 个 · 版本 {version}\n\n" + obj.Analysis
                : "AI 暂时繁忙，请稍后再试。";
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 内存建议请求失败");
            TxtResult.Text = "网络异常：" + ex.Message;
        }
        finally
        {
            SetBusy(false, "");
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    private static long GetSystemMemoryMb()
    {
        try
        {
            var st = new MemoryStatusEx { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MemoryStatusEx>() };
            if (GlobalMemoryStatusEx(ref st)) return (long)(st.ullTotalPhys / 1024 / 1024);
            return 0;
        }
        catch { return 0; }
    }

    private static string GetGameVersion()
    {
        try
        {
            var cfg = (dynamic)typeof(ModMain).Assembly.GetType("PCL.States")!
                .GetProperty("Game")!.GetValue(null)!;
            var ver = cfg.SelectedVersion?.ToString();
            return string.IsNullOrEmpty(ver) ? "1.21.1" : ver;
        }
        catch { return "1.21.1"; }
    }

    // ================= 通用 =================
    private void SetBusy(bool busy, string tip)
    {
        BtnSend.IsEnabled = !busy;
        BtnModCheck.IsEnabled = !busy;
        BtnMemAdvice.IsEnabled = !busy;
        TxtStatus.Text = busy ? tip : "";
        Cursor = busy ? Cursors.Wait : Cursors.Arrow;
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        TxtResult.Text = "";
        InQuestion.Text = "";
    }

    public class AiChatBody
    {
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    public class AiChatResult
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("answer")] public string? Answer { get; set; }
    }

    public class ModCheckBody
    {
        [JsonPropertyName("mods")] public string[] Mods { get; set; } = [];
    }

    public class MemAdviceBody
    {
        [JsonPropertyName("system_mb")] public long SystemMb { get; set; }
        [JsonPropertyName("allocated_mb")] public int AllocatedMb { get; set; }
        [JsonPropertyName("mod_count")] public int ModCount { get; set; }
        [JsonPropertyName("version")] public string? Version { get; set; }
    }

    public class ToolResult
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("analysis")] public string? Analysis { get; set; }
    }
}
