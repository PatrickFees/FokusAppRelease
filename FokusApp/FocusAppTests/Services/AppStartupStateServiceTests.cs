using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FocusAppTests.Services;

public class AppStartupStateServiceTests
{
    private readonly AppStartupStateService _service = new();
    private int _stateChangedCount;

    public AppStartupStateServiceTests()
    {
        _service.StateChanged += (_, _) => _stateChangedCount++;
    }

    [Fact]
    public void Constructor_NewService_IsLoadingWithoutFailure()
    {
        Assert.Equal(AppStartupStatus.Loading, _service.Status);
        Assert.Null(_service.Failure);
    }

    [Fact]
    public void MarkReady_WhileLoading_SetsReadyAndRaisesStateChanged()
    {
        _service.MarkReady();

        Assert.Equal(AppStartupStatus.Ready, _service.Status);
        Assert.Null(_service.Failure);
        Assert.Equal(1, _stateChangedCount);
    }

    [Fact]
    public void MarkFailed_WhileLoading_SetsFailedWithExceptionAndRaisesStateChanged()
    {
        var failure = new InvalidOperationException("Start fehlgeschlagen");

        _service.MarkFailed(failure);

        Assert.Equal(AppStartupStatus.Failed, _service.Status);
        Assert.Same(failure, _service.Failure);
        Assert.Equal(1, _stateChangedCount);
    }

    [Fact]
    public void MarkFailed_NullException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _service.MarkFailed(null!));
    }

    [Fact]
    public void MarkFailed_AfterReady_IsIgnored()
    {
        var failure = new InvalidOperationException("Zu spät");
        _service.MarkReady();

        _service.MarkFailed(failure);

        Assert.Equal(AppStartupStatus.Ready, _service.Status);
        Assert.Null(_service.Failure);
        Assert.Equal(1, _stateChangedCount);
    }

    [Fact]
    public void MarkReady_AfterFailed_IsIgnored()
    {
        var failure = new InvalidOperationException("Start fehlgeschlagen");
        _service.MarkFailed(failure);

        _service.MarkReady();

        Assert.Equal(AppStartupStatus.Failed, _service.Status);
        Assert.Same(failure, _service.Failure);
        Assert.Equal(1, _stateChangedCount);
    }

    [Fact]
    public void MarkReady_CalledTwice_RaisesStateChangedOnce()
    {
        _service.MarkReady();
        _service.MarkReady();

        Assert.Equal(1, _stateChangedCount);
    }

    [Fact]
    public async Task WaitForReadyAsync_AlreadyReady_CompletesImmediately()
    {
        _service.MarkReady();

        var waitTask = _service.WaitForReadyAsync();
        await waitTask;

        Assert.True(waitTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task WaitForReadyAsync_WaitingWhileLoading_CompletesAfterMarkReady()
    {
        var waitTask = _service.WaitForReadyAsync();
        Assert.False(waitTask.IsCompleted);

        _service.MarkReady();
        await waitTask;

        Assert.True(waitTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task WaitForReadyAsync_AfterFailure_ThrowsOriginalException()
    {
        var failure = new InvalidOperationException("Start fehlgeschlagen");
        _service.MarkFailed(failure);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.WaitForReadyAsync());

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task WaitForReadyAsync_WaitingWhileLoading_ThrowsAfterMarkFailed()
    {
        var failure = new InvalidOperationException("Start fehlgeschlagen");
        var waitTask = _service.WaitForReadyAsync();

        _service.MarkFailed(failure);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => waitTask);
        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task WaitForReadyAsync_CancelledWhileLoading_ThrowsOperationCanceled()
    {
        using var cancellation = new CancellationTokenSource();
        var waitTask = _service.WaitForReadyAsync(cancellation.Token);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
        Assert.Equal(AppStartupStatus.Loading, _service.Status);
    }
}
