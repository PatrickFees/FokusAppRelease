
#pragma warning disable IDE0130
#pragma warning disable CA1050
#pragma warning disable IDE1006

namespace Plugin.Maui.Audio
{
    public interface IAudioPlayer : IDisposable
    {
        bool IsPlaying { get; }

        event EventHandler? PlaybackEnded;

        void Play();

        void Stop();
    }

    public interface IAudioManager
    {
        IAudioPlayer CreatePlayer(Stream audioStream);
    }
}

public static class MainThread
{
    public static Func<Action, Task> InvokeHandler { get; set; } = action =>
    {
        action();
        return Task.CompletedTask;
    };

    public static Task InvokeOnMainThreadAsync(Action action)
    {
        return InvokeHandler(action);
    }
}

public static class FileSystem
{
    public static string CacheDirectory { get; set; } = Path.GetTempPath();

    public static Func<string, Task<Stream>> OpenAppPackageFileHandler { get; set; } =
        _ => Task.FromResult<Stream>(new MemoryStream());

    public static Task<Stream> OpenAppPackageFileAsync(string filename)
    {
        return OpenAppPackageFileHandler(filename);
    }
}

public sealed class DevicePlatform : IEquatable<DevicePlatform>
{
    private readonly string _name;

    private DevicePlatform(string name)
    {
        _name = name;
    }

    public static DevicePlatform WinUI { get; } = new("WinUI");

    public static DevicePlatform Android { get; } = new("Android");

    public static DevicePlatform iOS { get; } = new("iOS");

    public static DevicePlatform MacCatalyst { get; } = new("MacCatalyst");

    public static bool operator ==(DevicePlatform? left, DevicePlatform? right) => Equals(left, right);

    public static bool operator !=(DevicePlatform? left, DevicePlatform? right) => !Equals(left, right);

    public bool Equals(DevicePlatform? other) => other is not null && _name == other._name;

    public override bool Equals(object? obj) => obj is DevicePlatform other && Equals(other);

    public override int GetHashCode() => _name.GetHashCode();

    public override string ToString() => _name;
}

public interface IDeviceInfo
{
    DevicePlatform Platform { get; }

    string VersionString { get; }
}

public static class DeviceInfo
{
    public static IDeviceInfo Current { get; set; } = null!;
}

public interface IClipboard
{
    Task SetTextAsync(string? text);
}

public static class Clipboard
{
    public static IClipboard Default { get; set; } = null!;
}

public sealed class ShareTextRequest
{
    public string? Title { get; set; }

    public string? Text { get; set; }
}

public interface IShare
{
    Task RequestAsync(ShareTextRequest request);

    Task RequestAsync(ShareFileRequest request);
}

public static class Share
{
    public static IShare Default { get; set; } = null!;
}

public sealed class ShareFile(string fullPath)
{
    public string FullPath { get; } = fullPath;
}

public sealed class ShareFileRequest
{
    public string? Title { get; set; }

    public ShareFile? File { get; set; }
}

public interface IAppInfo
{
    string VersionString { get; }

    string BuildString { get; }
}

public static class AppInfo
{
    public static IAppInfo Current { get; set; } = null!;
}

public sealed class FilePickerFileType(IDictionary<DevicePlatform, IEnumerable<string>> values)
{
    public IDictionary<DevicePlatform, IEnumerable<string>> Values { get; } = values;
}

public sealed class PickOptions
{
    public string? PickerTitle { get; set; }

    public FilePickerFileType? FileTypes { get; set; }
}

public class FileResult(Func<Task<Stream>> openRead)
{
    public virtual Task<Stream> OpenReadAsync() => openRead();
}

public interface IFilePicker
{
    Task<FileResult?> PickAsync(PickOptions options);
}

public static class FilePicker
{
    public static IFilePicker Default { get; set; } = null!;
}

public enum AppTheme
{
    Unspecified,
    Light,
    Dark
}

public class Application
{
    public static Application? Current { get; set; }

    public AppTheme RequestedTheme { get; set; }

    public event EventHandler? RequestedThemeChanged;

    public void RaiseRequestedThemeChanged()
    {
        RequestedThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class MauiProgram
{
}
