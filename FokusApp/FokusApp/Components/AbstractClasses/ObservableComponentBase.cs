using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace FokusApp.Components.AbstractClasses;

public abstract class ObservableComponentBase : ComponentBase, IDisposable
{
    private readonly List<INotifyPropertyChanged> _observedObjects = [];
    private bool _isDisposed;

    // True while a render is already queued. Accessed via Interlocked, because PropertyChanged can arrive on any thread.
    private bool _renderScheduled;

    protected void Observe(INotifyPropertyChanged observable)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this); 

        observable.PropertyChanged += OnObservedPropertyChanged;
        _observedObjects.Add(observable);
    }

    // Called once per PropertyChanged event. Rendering is coalesced by the base class and must not be triggered here.
    protected virtual Task OnObservedPropertyChangedAsync(PropertyChangedEventArgs args)
    {
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (var observable in _observedObjects)
            {
                observable.PropertyChanged -= OnObservedPropertyChanged;
            }

            _observedObjects.Clear();
        }

        _isDisposed = true;
    }

    private void OnObservedPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        _ = InvokeAsync(() => HandleObservedPropertyChangedAsync(args));

        if (!Interlocked.Exchange(ref _renderScheduled, true))
        {
            _ = InvokeAsync(RenderOnce);
        }
    }

    private async Task HandleObservedPropertyChangedAsync(PropertyChangedEventArgs args)
    {
        if (_isDisposed)
        {
            return;
        }

        await OnObservedPropertyChangedAsync(args);
    }

    private void RenderOnce()
    {
        // Reset first, so events arriving during StateHasChanged queue a new render.
        Interlocked.Exchange(ref _renderScheduled, false);

        if (_isDisposed)
        {
            return;
        }

        StateHasChanged();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
