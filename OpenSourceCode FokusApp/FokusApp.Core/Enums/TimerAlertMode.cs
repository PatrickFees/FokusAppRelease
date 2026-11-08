namespace FokusApp.Core.Enums;

[Flags]
public enum TimerAlertMode
{

    None = 0,

    Sound = 1 << 0,

    Vibration = 1 << 1,

    AdaptToRingerMode = 1 << 3
}
