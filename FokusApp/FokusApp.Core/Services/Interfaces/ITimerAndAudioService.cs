namespace FokusApp.Core.Services.Interfaces;

public interface ITimerAndAudioService
{
    bool IsTimerRunning { get; }
    bool IsTimerPaused { get; }
    TimeSpan RemainingTime { get; }
    event EventHandler<TimerChangedEventArgs>? TimerChanged;
    void StartTimer(int timeInSeconds);
    void PauseTimer();
    void ResumeTimer();
    void ClearTimer();
    Task PlaySoundAsync(string filePath);
}
