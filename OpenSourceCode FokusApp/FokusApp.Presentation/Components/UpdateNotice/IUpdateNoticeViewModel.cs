using System.ComponentModel;

namespace FokusApp.Components.UpdateNotice;

public interface IUpdateNoticeViewModel : INotifyPropertyChanged
{
    bool IsOpen { get; }

    string? LatestVersion { get; }

    Task InitializeAsync();

    Task DismissAsync();

    Task DownloadAsync();
}
