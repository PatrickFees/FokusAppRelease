using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class ScopeServiceTests
{
    private readonly ScopeService _service = new();

    [Fact]
    public void IsWorkScope_NewService_IsTrue()
    {
        Assert.True(_service.IsWorkScope);
    }

    [Fact]
    public void ToggleScope_FromWork_SwitchesToPrivate()
    {
        _service.ToggleScope();

        Assert.False(_service.IsWorkScope);
    }

    [Fact]
    public void ToggleScope_CalledTwice_ReturnsToWork()
    {
        _service.ToggleScope();
        _service.ToggleScope();

        Assert.True(_service.IsWorkScope);
    }

    [Fact]
    public void ToggleScope_Always_RaisesScopeChangedOnceWithServiceAsSender()
    {
        var raisedCount = 0;
        object? sender = null;
        _service.ScopeChanged += (eventSender, _) =>
        {
            raisedCount++;
            sender = eventSender;
        };

        _service.ToggleScope();

        Assert.Equal(1, raisedCount);
        Assert.Same(_service, sender);
    }

    [Fact]
    public void ToggleScope_EventHandlerReadsScope_SeesNewValue()
    {
        bool? scopeInHandler = null;
        _service.ScopeChanged += (_, _) => scopeInHandler = _service.IsWorkScope;

        _service.ToggleScope();

        Assert.False(scopeInHandler);
    }

    [Fact]
    public void ToggleScope_WithoutSubscribers_DoesNotThrow()
    {
        var exception = Record.Exception(_service.ToggleScope);

        Assert.Null(exception);
    }
}
