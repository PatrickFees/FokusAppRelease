namespace FokusApp.Services;

public interface IUiBackPressService
{
    IDisposable RegisterHandler(Func<bool> handler);
    bool TryHandleBackPress();
}
