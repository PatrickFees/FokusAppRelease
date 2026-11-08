using System.ComponentModel;

namespace FokusApp.Components.WhatsNew;

public interface IWhatsNewViewModel : INotifyPropertyChanged
{
    bool IsOpen { get; }

    Task InitializeAsync();

    void Dismiss();
}
