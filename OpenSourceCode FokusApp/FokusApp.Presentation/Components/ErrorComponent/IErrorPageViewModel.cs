using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FokusApp.Components.ErrorComponent;

public interface IErrorPageViewModel : INotifyPropertyChanged
{
    ObservableCollection<ErrorPageItem> Items { get; }
    string LoadErrorMessage { get; }

    Task InitializeAsync();
    Task ReloadAsync();
}
