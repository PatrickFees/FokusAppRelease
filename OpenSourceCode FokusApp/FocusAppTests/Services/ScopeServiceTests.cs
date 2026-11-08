using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class ScopeServiceTests
{
    private readonly ScopeService _service = new(CreateSettings(DefaultScope.Both));

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

    [Theory]
    [InlineData(DefaultScope.Work, true)]
    [InlineData(DefaultScope.Private, false)]
    public void Constructor_FixedDefaultScope_StartsInThatScope(DefaultScope fixedScope, bool expectedWork)
    {
        var service = new ScopeService(CreateSettings(fixedScope));

        Assert.Equal(expectedWork, service.IsWorkScope);
        Assert.False(service.CanSwitch);
    }

    [Theory]
    [InlineData(DefaultScope.Work)]
    [InlineData(DefaultScope.Private)]
    public void ToggleScope_FixedDefaultScope_DoesNothing(DefaultScope fixedScope)
    {
        var service = new ScopeService(CreateSettings(fixedScope));
        var before = service.IsWorkScope;
        var raised = 0;
        service.ScopeChanged += (_, _) => raised++;

        service.ToggleScope();

        Assert.Equal(before, service.IsWorkScope);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void ToggleScope_BothDefaultScope_Switches()
    {
        var service = new ScopeService(CreateSettings(DefaultScope.Both));

        service.ToggleScope();

        Assert.True(service.CanSwitch);
        Assert.False(service.IsWorkScope);
    }

    [Fact]
    public void DefaultScopeChanged_ToPrivate_MovesToPrivateAndRaisesScopeChanged()
    {
        var settings = CreateSettings(DefaultScope.Both);
        var service = new ScopeService(settings);
        var raised = 0;
        service.ScopeChanged += (_, _) => raised++;
        settings.DefaultScope.Returns(DefaultScope.Private);

        settings.DefaultScopeChanged += Raise.Event();

        Assert.False(service.IsWorkScope);
        Assert.False(service.CanSwitch);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void DefaultScopeChanged_AlreadyInThatScope_RaisesNoScopeChanged()
    {
        var settings = CreateSettings(DefaultScope.Both);
        var service = new ScopeService(settings);
        var raised = 0;
        service.ScopeChanged += (_, _) => raised++;
        settings.DefaultScope.Returns(DefaultScope.Work);

        settings.DefaultScopeChanged += Raise.Event();

        Assert.True(service.IsWorkScope);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void DefaultScopeChanged_BackToBoth_KeepsTheScopeAndFreesTheSwitch()
    {
        var settings = CreateSettings(DefaultScope.Private);
        var service = new ScopeService(settings);
        settings.DefaultScope.Returns(DefaultScope.Both);

        settings.DefaultScopeChanged += Raise.Event();
        service.ToggleScope();

        Assert.True(service.CanSwitch);
        Assert.True(service.IsWorkScope);
    }

    private static IDefaultScopeSettingsService CreateSettings(DefaultScope scope)
    {
        var settings = Substitute.For<IDefaultScopeSettingsService>();
        settings.DefaultScope.Returns(scope);
        return settings;
    }
}
