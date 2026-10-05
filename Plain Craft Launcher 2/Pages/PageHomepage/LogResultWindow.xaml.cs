using System.Windows;

namespace PCL;

public partial class LogResultWindow : Window
{
    public LogResultWindow(string title, string content)
    {
        InitializeComponent();
        Owner = ModMain.frmMain;
        TxtTitle.Text = title;
        TxtContent.Text = content;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
