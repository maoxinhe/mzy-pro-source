using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.Json.Serialization;
using PCL.Network;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageSetupFeedback
{
    public enum TagId : long
    {
        Processing = 6820804544L,
        WaitingProcess = 6820804546L,
        Completed = 6820804547L,
        Decline = 6820804539L,
        Ignored = 8064650117L,
        Duplicate = 6820804541L,
        Wait = 8743070786L,
        Pause = 8558220235L,
        Upnext = 8550609020L
    }

    private bool _isLoaded;

    public ModLoader.LoaderTask<bool, List<Feedback>> Loader;

    private static readonly Dictionary<string, (Func<PageSetupFeedback, StackPanel> Panel, string Icon)> TagMap = new()
    {
        [((long)TagId.Processing).ToString()] = (p => p.PanListProcessing, "Blocks/CommandBlock.png"),
        [((long)TagId.WaitingProcess).ToString()] = (p => p.PanListWaitingProcess, "Blocks/RedstoneBlock.png"),
        [((long)TagId.Wait).ToString()] = (p => p.PanListWait, "Blocks/Anvil.png"),
        [((long)TagId.Pause).ToString()] = (p => p.PanListPause, "Blocks/RedstoneLampOff.png"),
        [((long)TagId.Upnext).ToString()] = (p => p.PanListUpnext, "Blocks/RedstoneLampOn.png"),
        [((long)TagId.Completed).ToString()] = (p => p.PanListCompleted, "Blocks/Grass.png"),
        [((long)TagId.Decline).ToString()] = (p => p.PanListDecline, "Blocks/CobbleStone.png"),
        [((long)TagId.Ignored).ToString()] = (p => p.PanListIgnored, "Blocks/CobbleStone.png"),
        [((long)TagId.Duplicate).ToString()] = (p => p.PanListDuplicate, "Blocks/CobbleStone.png"),
    };

    public PageSetupFeedback()
    {
        InitializeComponent();
        Loader = new ModLoader.LoaderTask<bool, List<Feedback>>("FeedbackList", FeedbackListGet);
        Loaded += PageOtherFeedback_Loaded;
    }

    private void PageOtherFeedback_Loaded(object sender, RoutedEventArgs e)
    {
        // 梦之韵Pro：反馈统一走玩家门户工单系统，不再加载官方 GitHub Issues（国内访问慢）
        // 重复加载部分
        PanBack.ScrollToHome();
        // 非重复加载部分
        if (_isLoaded)
            return;
        _isLoaded = true;
        PanContent.Visibility = Visibility.Collapsed;
    }

    public void FeedbackListGet(ModLoader.LoaderTask<bool, List<Feedback>> task)
    {
        // 梦之韵Pro：反馈统一走玩家门户工单系统，不再访问官方 GitHub Issues（避免国内访问卡死）。
        // 页面已折叠反馈列表，引导用户前往 player.camzy.uno/#tickets 提交工单。
        task.output = new List<Feedback>();
    }

    private MyListItem CreateFeedbackItem(Feedback item, string logo)
    {
        var li = new MyListItem
        {
            Title = item.Title,
            Type = MyListItem.CheckType.Clickable,
            Info = $"{item.User} | {Lang.Date(item.Time)}",
            Logo = ModBase.pathImage + logo,
            Tags = item.Type
        };

        li.Click += (_, _) => ShowFeedbackDetail(item);

        return li;
    }

    private void ShowFeedbackDetail(Feedback item)
    {
        var timeSpanText = Lang.TimeSpan(item.Time - DateTime.Now);
        switch (ModMain.MyMsgBoxMarkdown(
                    Lang.Text("Setup.Feedback.Item.Submitter", item.User, timeSpanText) + "\n" +
                    Lang.Text("Setup.Feedback.Item.Type", item.Type) + "\n\n" +
                    item.Content,
                    $"#{item.Id} {item.Title}", button2: Lang.Text("Setup.Feedback.Item.ViewDetail")))
        {
            case 2:
            {
                ModBase.OpenWebsite(item.Url);
                break;
            }
        }
    }

    private void SetPanelVisibility(StackPanel panel, MyCard card)
    {
        card.Visibility = panel.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    public void RefreshList()
    {
        PanListProcessing.Children.Clear();
        PanListWaitingProcess.Children.Clear();
        PanListWait.Children.Clear();
        PanListPause.Children.Clear();
        PanListUpnext.Children.Clear();
        PanListCompleted.Children.Clear();
        PanListDecline.Children.Clear();
        PanListIgnored.Children.Clear();
        PanListDuplicate.Children.Clear();

        foreach (var item in Loader.output)
        {
            var tag = item.Tags.Find(TagMap.ContainsKey);
            if (tag is not null)
            {
                var (panel, icon) = TagMap[tag];
                panel(this).Children.Add(CreateFeedbackItem(item, icon));
            }
        }

        SetPanelVisibility(PanListProcessing, PanContentProcessing);
        SetPanelVisibility(PanListWaitingProcess, PanContentWaitingProcess);
        SetPanelVisibility(PanListWait, PanContentWait);
        SetPanelVisibility(PanListPause, PanContentPause);
        SetPanelVisibility(PanListUpnext, PanContentUpnext);
        SetPanelVisibility(PanListCompleted, PanContentCompleted);
        SetPanelVisibility(PanListDecline, PanContentDecline);
        SetPanelVisibility(PanListIgnored, PanContentIgnored);
        SetPanelVisibility(PanListDuplicate, PanContentDuplicate);
    }

    private void Feedback_Click(object sender, MouseButtonEventArgs e)
    {
        // 梦之韵Pro：打开玩家门户工单中心（反馈/举报/建议统一走工单系统）
        ModBase.OpenWebsite("https://player.camzy.uno/#tickets");
    }

    public class Feedback
    {
        [JsonIgnore]
        public string User { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;
        [JsonPropertyName("created_at")]
        public DateTime Time { get; init; }
        [JsonPropertyName("body")]
        public string Content { get; init; } = string.Empty;
        [JsonPropertyName("html_url")]
        public string Url { get; init; } = string.Empty;
        [JsonIgnore]
        public string Id { get; set; } = string.Empty;
        public List<string> Tags { get; } = new();
        public string Type { get; set; } = string.Empty;
    }
}