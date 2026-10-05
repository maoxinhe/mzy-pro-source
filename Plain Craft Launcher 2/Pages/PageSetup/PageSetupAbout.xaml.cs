using System.Windows;
using System.Windows.Input;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public partial class PageSetupAbout
{
    // 彩蛋
    private int clickCount;

    private new bool isLoaded;

    public PageSetupAbout()
    {
        InitializeComponent();
        Loaded += PageOtherAbout_Loaded;
    }

    private void PageOtherAbout_Loaded(object sender, RoutedEventArgs e)
    {
        // 重复加载部分
        PanBack.ScrollToHome();

        // 非重复加载部分
        if (isLoaded)
            return;
        isLoaded = true;

        // 梦之韵Pro：本页为纯静态（无任何网络请求，避免国内访问官方源卡死）
        // 版本信息由 DataContext 绑定 Basics.Metadata.Name 自动填充
    }
}
