using System.ComponentModel;

namespace FokusApp.Components.SharedUI;

public interface IConfettiViewModel : INotifyPropertyChanged
{

    bool IsVisible { get; }

    int BurstNumber { get; }

    void ShowBurst();
}
