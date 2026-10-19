using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace FokusApp.Components.Layout;

public interface INavMenuViewModel : INotifyPropertyChanged
{
    IRelayCommand ToggleTaskScopeCommand { get; }
    IRelayCommand ToggleNavigationMenuCommand { get; }
    IRelayCommand ExpandNavigationMenuCommand { get; }
    IRelayCommand CollapseNavigationMenuCommand { get; }
    IRelayCommand<string> NavigateToCommand { get; }
    bool IsNavigationExpanded { get; }
    bool IsWorkScope { get; }
    string CurrentRoute { get; }
}
