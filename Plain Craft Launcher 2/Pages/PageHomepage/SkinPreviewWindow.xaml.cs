using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PCL;

public partial class SkinPreviewWindow : Window
{
    private readonly HttpClient _hc = new() { Timeout = TimeSpan.FromSeconds(10) };

    public SkinPreviewWindow()
    {
        InitializeComponent();
        Owner = ModMain.frmMain;
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TxtStatus.Text = "请输入MC名字";
            return;
        }
        TxtStatus.Text = "查询中...";
        ImgSkin.Visibility = Visibility.Collapsed;
        try
        {
            // 1. 拿 UUID
            var profile = await _hc.GetFromJsonAsync<MojangProfile>($"https://api.mojang.com/users/profiles/minecraft/{name}");
            if (profile?.Id is null)
            {
                TxtStatus.Text = "玩家不存在";
                return;
            }
            // 2. 显示头像（用 crafatar API）
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri($"https://crafatar.com/avatars/{profile.Id}?size=120&overlay");
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            ImgSkin.Source = img;
            ImgSkin.Visibility = Visibility.Visible;
            TxtStatus.Text = $"{profile.Name}";
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[梦之韵Pro] 皮肤查询失败");
            TxtStatus.Text = "网络异常，查询失败";
        }
    }

    public class MojangProfile
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
