using System.ComponentModel;

namespace FocusAppTests.TestSupport;

public static class WaitForAsyncHelper
{
    public static async Task WaitForAsync(Func<bool> condition, int delayMs = 10, int maxAttempts = 100)
    {
        for (var attempt = 0; attempt < maxAttempts && !condition(); attempt++)
        {
            await Task.Delay(delayMs);
        }
    }

    public static async Task WaitForAsync(INotifyPropertyChanged source, Func<bool> condition, TimeSpan? timeout = null)
    {
        if (condition())
        {
            return;
        }

        var completionSource = new TaskCompletionSource();

        void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (condition())
            {
                completionSource.TrySetResult();
            }
        }

        source.PropertyChanged += OnPropertyChanged;
        try
        {
            var timeoutTask = Task.Delay(timeout ?? TimeSpan.FromSeconds(5));
            var completed = await Task.WhenAny(completionSource.Task, timeoutTask);
            if (completed != completionSource.Task)
            {
                throw new TimeoutException("Die erwartete Bedingung wurde nicht erfüllt.");
            }
        }
        finally
        {
            source.PropertyChanged -= OnPropertyChanged;
        }
    }
}
