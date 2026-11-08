using FokusApp.Services;

namespace FocusAppTests.Services;

public class UiBackPressServiceTests
{
    private readonly UiBackPressService _service = new();

    [Fact]
    public void TryHandleBackPress_NoHandlers_ReturnsFalse()
    {
        var handled = _service.TryHandleBackPress();

        Assert.False(handled);
    }

    [Fact]
    public void RegisterHandler_NullHandler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _service.RegisterHandler(null!));
    }

    [Fact]
    public void TryHandleBackPress_HandlerHandlesIt_ReturnsTrue()
    {
        _service.RegisterHandler(() => true);

        var handled = _service.TryHandleBackPress();

        Assert.True(handled);
    }

    [Fact]
    public void TryHandleBackPress_HandlerDoesNotHandleIt_ReturnsFalse()
    {
        _service.RegisterHandler(() => false);

        var handled = _service.TryHandleBackPress();

        Assert.False(handled);
    }

    [Fact]
    public void TryHandleBackPress_SeveralHandlers_AsksTheNewestFirstAndStopsAtTheFirstThatHandlesIt()
    {
        var calls = new List<string>();
        _service.RegisterHandler(() => { calls.Add("alt"); return true; });
        _service.RegisterHandler(() => { calls.Add("mittel"); return true; });
        _service.RegisterHandler(() => { calls.Add("neu"); return false; });

        var handled = _service.TryHandleBackPress();

        Assert.True(handled);
        Assert.Equal(["neu", "mittel"], calls);
    }

    [Fact]
    public void TryHandleBackPress_NoHandlerHandlesIt_AsksAllOfThem()
    {
        var calls = new List<string>();
        _service.RegisterHandler(() => { calls.Add("alt"); return false; });
        _service.RegisterHandler(() => { calls.Add("neu"); return false; });

        var handled = _service.TryHandleBackPress();

        Assert.False(handled);
        Assert.Equal(["neu", "alt"], calls);
    }

    [Fact]
    public void Dispose_Registration_RemovesTheHandler()
    {
        var calls = 0;
        var registration = _service.RegisterHandler(() => { calls++; return true; });

        registration.Dispose();
        var handled = _service.TryHandleBackPress();

        Assert.False(handled);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Dispose_RegistrationTwice_RemovesTheHandlerOnlyOnce()
    {
        var calls = 0;
        Func<bool> handler = () => { calls++; return true; };
        var first = _service.RegisterHandler(handler);
        _service.RegisterHandler(handler);

        first.Dispose();
        first.Dispose();
        var handled = _service.TryHandleBackPress();

        Assert.True(handled);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Dispose_HandlerRemovesItselfWhileBackPressIsHandled_DoesNotBreakTheLoop()
    {
        IDisposable? registration = null;
        registration = _service.RegisterHandler(() =>
        {
            registration!.Dispose();
            return false;
        });
        var secondCalled = false;
        _service.RegisterHandler(() => { secondCalled = true; return false; });

        var handled = _service.TryHandleBackPress();

        Assert.False(handled);
        Assert.True(secondCalled);
    }
}
