using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.Json.Serialization;
using PCL.Core.App;
using PCL.Core.Utils;
using PCL.Core.App.Localization;
using PCL.Core.Utils.OS;
using PCL.Core.IO.Net.Http;

namespace PCL;

public partial class PageSetupUpdate
{
    // 梦之韵Pro：更新源全部走自建 Cloudflare Worker（apc），不再访问官方 GitHub 源
    public const string ApiBase = "https://apc.camzy.uno";
    public const string ReleasePage = "https://github.com/maoxinhe/mzy-pro/releases";
    private LauncherInfoModel _latestInfo;

    public PageSetupUpdate()
    {
        InitializeComponent();
        Loaded += (_, _) => Init();
    }

    private void Init()
    {
        ModAnimation.AniControlEnabled += 1;
        TextMirrorCDK.Password = Config.Update.MirrorChyanKey;

        ComboSystemUpdateChannel.SelectedIndex = (int)Config.Update.UpdateChannel;
        ComboSystemUpdateMode.SelectedIndex = (int)Config.Update.UpdateMode;

        TextCurrentVersion.Text = "梦之韵Pro " + VersionNameFormat(ModBase.versionBaseName);
        ModAnimation.AniControlEnabled -= 1;
        CheckUpdate();
    }

    public class LauncherInfoModel
    {
        [JsonPropertyName("latest")] public string? Latest { get; set; }
        [JsonPropertyName("minimum")] public string? Minimum { get; set; }
        [JsonPropertyName("mandatory")] public bool Mandatory { get; set; }
        [JsonPropertyName("urls")] public Dictionary<string, string>? Urls { get; set; }
        [JsonPropertyName("release_page")] public string? ReleasePage { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    private async Task<UpdateStatus> IsLatestAsync()
    {
        try
        {
            var info = await HttpRequest.GetJsonAsync<LauncherInfoModel>(ApiBase + "/launcher.json");
            _latestInfo = info;
            if (info is null || string.IsNullOrWhiteSpace(info.Latest))
                return UpdateStatus.Error;

            var latestClean = info.Latest.TrimStart('v', 'V');
            var localClean = ModBase.versionBaseName.TrimStart('v', 'V');
            ModBase.Log("[Update] 远程最新: " + info.Latest + " / 本地: " + ModBase.versionBaseName);
            return latestClean == localClean ? UpdateStatus.Latest : UpdateStatus.Available;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, Lang.Text("Setup.Update.Error.NetworkFailed"), ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Setup.Update.Error.NetworkFailed"));
            return UpdateStatus.Error;
        }
    }

    public async void CheckUpdate()
    {
        ModBase.Log("[Update] 开始检查更新（数据源: " + ApiBase + "）");
        CardUpdate.Visibility = Visibility.Collapsed;
        CardCheck.Visibility = Visibility.Visible;
        TextCurrentDesc.Text = Lang.Text("Setup.Update.Checking");
        BtnCheckAgain.IsEnabled = false;
        switch (await IsLatestAsync())
        {
            case UpdateStatus.Available:
            {
                TextUpdateName.Text = "梦之韵Pro " + VersionNameFormat(_latestInfo.Latest!);
                var note = _latestInfo.Note ?? "";
                TextChangelog.Text = string.IsNullOrWhiteSpace(note) ? Lang.Text("Setup.Update.Changelog.Empty") : note;
                BtnCheckAgain.IsEnabled = true;
                BtnUpdate.Text = Lang.Text("Setup.Update.Install");
                BtnUpdate.IsEnabled = true;
                CardUpdate.Visibility = Visibility.Visible;
                CardCheck.Visibility = Visibility.Collapsed;
                break;
            }
            case UpdateStatus.Latest:
            {
                CardUpdate.Visibility = Visibility.Collapsed;
                CardCheck.Visibility = Visibility.Visible;
                BtnCheckAgain.IsEnabled = true;
                TextCurrentDesc.Text = Lang.Text("Setup.Update.Latest");
                break;
            }
            case UpdateStatus.Error:
            {
                CardUpdate.Visibility = Visibility.Collapsed;
                CardCheck.Visibility = Visibility.Visible;
                BtnCheckAgain.IsEnabled = true;
                TextCurrentDesc.Text = Lang.Text("Setup.Update.CheckFailed");
                break;
            }
        }
    }

    private void BtnUpdate_Click(object sender, MouseButtonEventArgs e)
    {
        // 梦之韵Pro：更新走 OneDrive 国内直链（Worker 自动刷新直链）
        var url = "https://apc.camzy.uno/update/download";
        ModBase.OpenWebsite(url);
    }

    private void BtnChangelogDetail_Click(object sender, EventArgs e)
    {
        ModBase.OpenWebsite(ReleasePage);
    }

    private void ComboSystemUpdateMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled == 0)
            Config.Update.UpdateMode = (LauncherAutoUpdateBehavior)ComboSystemUpdateMode.SelectedIndex;
    }

    private void ComboSystemUpdateBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;

        var isCancelled = false;
        switch (ComboSystemUpdateChannel.SelectedIndex)
        {
            case 0:
            {
                break;
            }
            case 1:
            {
                if (ModMain.MyMsgBox(Lang.Text("Setup.Update.Channel.Beta.Warning.Message"),
                        Lang.Text("Setup.Update.Channel.Common.Warning.Title"),
                        Lang.Text("Setup.Update.Channel.Common.Warning.Confirm"),
                        Lang.Text("Common.Action.Cancel"), isWarn: true) == 2)
                    isCancelled = true;
                else
                    CheckUpdate();
                break;
            }
            case 2:
            {
                if (ModMain.MyMsgBox(Lang.Text("Setup.Update.Channel.Dev.Warning.Message"),
                        Lang.Text("Setup.Update.Channel.Common.Warning.Title"),
                        Lang.Text("Setup.Update.Channel.Common.Warning.Confirm"),
                        Lang.Text("Common.Action.Cancel"), isWarn: true) == 2)
                {
                    isCancelled = true;
                    break;
                }

                var confirmText = Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.ExpectedInput");
                var ret = ModMain.MyMsgBoxInput(
                    Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.Title"),
                    Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.Message", confirmText),
                    button1: Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.Submit"),
                    button2: Lang.Text("Common.Action.Cancel"), isWarn: true);
    
                if (ret == confirmText)
                {
                    CheckUpdate();
                }
                else
                {
                    HintService.Hint(Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.WrongInput"));
                    isCancelled = true;
                }
                break;
            }
        }

        if (isCancelled)
        {
            ModAnimation.AniControlEnabled += 1;
            ComboSystemUpdateChannel.SelectedItem = e.RemovedItems[0];
            ModAnimation.AniControlEnabled -= 1;
        }
        else
        {
            Config.Update.UpdateChannel = (Core.App.UpdateChannel)ComboSystemUpdateChannel.SelectedIndex;
        }
    }

    private void TextMirrorCDK_PasswordChanged(object sender, EventArgs e)
    {
        Config.Update.MirrorChyanKey = TextMirrorCDK.Password;
    }

    private void BtnGetMirrorCDK_Click(object sender, MouseButtonEventArgs e)
    {
        ModBase.OpenWebsite("https://mirrorchyan.com/");
    }

    private void BtnChangelog_Click(object sender, MouseButtonEventArgs e)
    {
        ModBase.OpenWebsite(ReleasePage);
    }

    public string VersionNameFormat(string str)
    {
        str = str.Replace("v", "");
        if (!str.Contains("-"))
            return str;
        var add = str.AfterLast("-");
        str = str.BeforeLast("-");
        return $"{str} {add.Replace(".", " ").Replace("beta", "Beta").Replace("rc", "RC")}";
    }

    private void BtnCheckAgain_OnClick(object sender, MouseButtonEventArgs e)
    {
        CheckUpdate();
    }

    private enum UpdateStatus
    {
        Checking = 0,
        Available = 1,
        Error = 2,
        Latest = 3
    }
}
