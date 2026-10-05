using System.Windows;
using System.Windows.Controls;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

/// <summary>
///     梦之韵Pro 顶级「关于」页左侧导航（与启动/下载/设置/工具并排）。
///     子页面复用设置页内已有页面：服务 / 软件信息 / 软件更新 / 反馈。
/// </summary>
public partial class PageAboutLeft
{
    private bool isLoad;
    private bool isPageSwitched; // 如果在 Loaded 前切换到其他页面，会导致触发 Loaded 时再次切换一次

    public PageAboutLeft()
    {
        InitializeComponent();
        AnimatedControl = PanItem;
        Loaded += PageAboutLeft_Loaded;
        Unloaded += PageAboutLeft_Unloaded;
    }

    private void PageAboutLeft_Loaded(object sender, RoutedEventArgs e)
    {
        if (isLoad)
            return;
        isLoad = true;
        if (isPageSwitched)
            return;
        // 默认选中「软件信息」（开发者/版本），点开顶级「关于」页签直接看到关于内容
        ItemAbout.SetChecked(true, false, false);
    }

    private void PageAboutLeft_Unloaded(object sender, RoutedEventArgs e)
    {
        isPageSwitched = false;
    }

    #region 页面切换

    /// <summary>
    ///     当前页面的编号。
    /// </summary>
    public FormMain.PageSubType pageID = FormMain.PageSubType.SetupAbout;

    /// <summary>
    ///     勾选事件改变页面。
    /// </summary>
    private void PageCheck(object senderRaw, ModBase.RouteEventArgs e)
    {
        var sender = (MyListItem)senderRaw;
        if (sender.Tag is not null)
            PageChange((FormMain.PageSubType)ModBase.Val(sender.Tag));
    }

    public object PageGet(FormMain.PageSubType? id = null)
    {
        var targetID = id ?? pageID;
        switch (targetID)
        {
            case FormMain.PageSubType.SetupService:
            {
                if (ModMain.frmSetupService is null)
                    ModMain.frmSetupService = new PageSetupService();
                return ModMain.frmSetupService;
            }
            case FormMain.PageSubType.SetupAbout:
            {
                if (ModMain.frmSetupAbout is null)
                    ModMain.frmSetupAbout = new PageSetupAbout();
                return ModMain.frmSetupAbout;
            }
            case FormMain.PageSubType.SetupUpdate:
            {
                if (ModMain.frmSetupUpdate is null)
                    ModMain.frmSetupUpdate = new PageSetupUpdate();
                return ModMain.frmSetupUpdate;
            }
            case FormMain.PageSubType.SetupFeedback:
            {
                if (ModMain.frmSetupFeedback is null)
                    ModMain.frmSetupFeedback = new PageSetupFeedback();
                return ModMain.frmSetupFeedback;
            }
            default:
            {
                throw new Exception("未知的关于子页面种类：" + (int)targetID);
            }
        }
    }

    /// <summary>
    ///     切换现有页面。
    /// </summary>
    public void PageChange(FormMain.PageSubType id)
    {
        if (pageID == id)
            return;
        ModAnimation.AniControlEnabled += 1;
        isPageSwitched = true;
        try
        {
            PageChangeRun((MyPageRight)PageGet(id));
            pageID = id;
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"切换关于子页面失败（ID {(int)id}）",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Tools.Error.OperationFailed"));
        }
        finally
        {
            ModAnimation.AniControlEnabled -= 1;
        }
    }

    private static void PageChangeRun(MyPageRight target)
    {
        ModAnimation.AniStop("FrmMain PageChangeRight"); // 停止主页面的右页面切换动画，防止它与本动画一起触发多次 PageOnEnter
        if (target.Parent is not null)
            target.SetValue(ContentPresenter.ContentProperty, null);
        ModMain.frmMain.pageRight = target;
        ((MyPageRight)ModMain.frmMain.PanMainRight.Child).PageOnExit();
        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaCode(() =>
            {
                ((MyPageRight)ModMain.frmMain.PanMainRight.Child).PageOnForceExit();
                ModMain.frmMain.PanMainRight.Child = ModMain.frmMain.pageRight;
                ModMain.frmMain.pageRight.Opacity = 0d;
            }, 130),
            ModAnimation.AaCode(() =>
            {
                // 延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                ModMain.frmMain.pageRight.Opacity = 1d;
                ModMain.frmMain.pageRight.PageOnEnter();
            }, 30, true)
        }, "PageLeft PageChange");
    }

    #endregion
}
