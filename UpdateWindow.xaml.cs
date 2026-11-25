using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Hushak;

public partial class UpdateWindow : Window
{
    private readonly NoteStore store;
    private UpdateInfo? currentUpdateInfo;
    private CancellationTokenSource? activeCts;
    private bool isBusy;

    public UpdateWindow()
    {
        InitializeComponent();
        store = ((App)Application.Current).Store;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await PerformCheckAsync();
    }

    public async Task PerformCheckAsync()
    {
        if (isBusy) return;
        isBusy = true;

        activeCts?.Cancel();
        activeCts?.Dispose();
        activeCts = new CancellationTokenSource();

        StatusTitleTextBlock.Text = LocalizationManager.Get("CheckingUpdatesStatus");
        VersionInfoTextBlock.Text = $"{LocalizationManager.Get("VersionPrefix")} {UpdateManager.GetCurrentVersion()}";
        ReleaseNotesTextBlock.Text = string.Empty;
        DownloadButton.Visibility = Visibility.Collapsed;
        DownloadButton.IsEnabled = false;
        DownloadProgressPanel.Visibility = Visibility.Collapsed;
        CheckAgainButton.IsEnabled = false;

        try
        {
            var result = await UpdateManager.CheckForUpdatesAsync(store.StoreAppId, activeCts.Token);
            currentUpdateInfo = result;

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                StatusTitleTextBlock.Text = LocalizationManager.Get("UpdateCheckFailed");
                ReleaseNotesTextBlock.Text = LocalizationManager.Format("UpdateFailedMsg", result.ErrorMessage);
                return;
            }

            if (result.IsUpdateAvailable)
            {
                StatusTitleTextBlock.Text = LocalizationManager.Format(
                    "UpdateAvailableMsg",
                    result.LatestVersion,
                    result.CurrentVersion);

                VersionInfoTextBlock.Text = string.IsNullOrWhiteSpace(result.ReleaseDate)
                    ? $"{LocalizationManager.Get("VersionPrefix")} {result.CurrentVersion} → {result.LatestVersion}"
                    : $"{LocalizationManager.Get("VersionPrefix")} {result.CurrentVersion} → {result.LatestVersion} ({result.ReleaseDate})";

                ReleaseNotesTextBlock.Text = result.ReleaseNotes;
                DownloadButton.Visibility = Visibility.Visible;
                DownloadButton.IsEnabled = true;
            }
            else
            {
                StatusTitleTextBlock.Text = LocalizationManager.Format("UpToDateMsg", result.CurrentVersion);
                ReleaseNotesTextBlock.Text = result.ReleaseNotes;
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            CheckAgainButton.IsEnabled = true;
            isBusy = false;
        }
    }

    private void OpenWebsiteButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateManager.OpenWebsite(currentUpdateInfo?.PageUrl);
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (isBusy || currentUpdateInfo == null) return;

        string downloadUrl = currentUpdateInfo.DownloadUrl;
        if (!downloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            UpdateManager.OpenWebsite(currentUpdateInfo.PageUrl);
            return;
        }

        isBusy = true;
        DownloadButton.IsEnabled = false;
        CheckAgainButton.IsEnabled = false;
        DownloadProgressPanel.Visibility = Visibility.Visible;
        DownloadProgressBar.Value = 0;
        DownloadStatusTextBlock.Text = LocalizationManager.Get("DownloadingStatus");

        activeCts?.Cancel();
        activeCts?.Dispose();
        activeCts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<int>(percent =>
            {
                DownloadProgressBar.Value = percent;
                DownloadStatusTextBlock.Text = $"{LocalizationManager.Get("DownloadingStatus")} {percent}%";
            });

            string installerPath = await UpdateManager.DownloadInstallerAsync(
                downloadUrl,
                currentUpdateInfo.Sha256,
                progress,
                activeCts.Token);

            UpdateManager.LaunchInstaller(installerPath);
            MessageBox.Show(
                this,
                LocalizationManager.Get("DownloadCompletePrompt"),
                LocalizationManager.Get("Information"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Close();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            DownloadProgressPanel.Visibility = Visibility.Collapsed;
            MessageBox.Show(
                this,
                LocalizationManager.Format("UpdateFailedMsg", ex.Message),
                LocalizationManager.Get("Error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            DownloadButton.IsEnabled = true;
            CheckAgainButton.IsEnabled = true;
            isBusy = false;
        }
    }

    private async void CheckAgainButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformCheckAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        activeCts?.Cancel();
        activeCts?.Dispose();
        base.OnClosed(e);
    }
}
