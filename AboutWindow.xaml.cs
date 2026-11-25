using System.Windows;

namespace Hushak;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        UpdateVersionDisplay();
        LocalizationManager.LanguageChanged += UpdateVersionDisplay;
    }

    private void UpdateVersionDisplay()
    {
        bool isArmenian = string.Equals(LocalizationManager.Instance.CurrentLanguage, "hy", System.StringComparison.OrdinalIgnoreCase);

        if (isArmenian)
        {
            AppNameTextBlock.Text = "Հուշակ";
            AiNoticeBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            AppNameTextBlock.Text = "Hushak";
            AiNoticeBorder.Visibility = Visibility.Visible;
        }

        VersionTextBlock.Text = $"{LocalizationManager.Get("VersionPrefix")} {UpdateManager.GetCurrentVersion()}";
    }

    private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateWindow updateWindow = new UpdateWindow
        {
            Owner = this
        };
        updateWindow.ShowDialog();
    }

    private void WebsiteLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        UpdateManager.OpenWebsite(e.Uri.AbsoluteUri);
        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        LocalizationManager.LanguageChanged -= UpdateVersionDisplay;
        base.OnClosed(e);
    }
}
