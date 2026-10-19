using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;

namespace FokusApp.Core.Services;

/// <summary>
/// Erzeugt die Startdaten der App (Klingeltöne, Einstellung, Beispielaufgaben). Wird beim ersten Start
/// und beim Zurücksetzen auf die Startdaten verwendet.
/// </summary>
public static class StartupDataFactory
{
    public const string DefaultRingtoneAsset = "Raw/Happy with Clap.mp3";

    private static readonly string[] RingtoneAssets =
    [
        DefaultRingtoneAsset,
        "Raw/Hello Sunshine.mp3",
        "Raw/Kurz nervig.mp3",
        "Raw/Motivation Rock.mp3"
    ];

    public static IEnumerable<RingtonesEntity> CreateRingtones() =>
        RingtoneAssets.Select(assetPath => new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = assetPath
        });

    public static SettingEntity CreateSetting(RingtonesEntity defaultRingtone) =>
        new()
        {
            Id = Guid.NewGuid(),
            Theme = 0,
            Language = 0,
            RingtoneId = defaultRingtone.Id,
            Ringtone = defaultRingtone
        };

    public static RingtonesEntity SelectDefaultRingtone(IReadOnlyCollection<RingtonesEntity> ringtones) =>
        ringtones.FirstOrDefault(ringtone =>
            string.Equals(
                ringtone.RingtoneFilePath,
                DefaultRingtoneAsset,
                StringComparison.OrdinalIgnoreCase))
        ?? ringtones.FirstOrDefault()
        ?? throw new InvalidOperationException("No default ringtone could be initialized.");

    public static IEnumerable<TaskEntity> CreateStartingTasks(DateTimeOffset now) =>
    [
        new TaskEntity
        {
            Id = Guid.NewGuid(),
            IsDone = false,
            Note = "So kann dein Aufgabe aussehen. Swipe nach links um Aufgaben zu Bearbeiten oder zu Löschen.",
            IsRegularTask = false,
            IsWorkTask = true,
            IsPositive = false,
            CreationDate = now.DateTime,
            TaskDate = now.Date,
            Priority = TaskPriority.Dringend,
            BreakMinutes = 5
        },
        new TaskEntity
        {
            Id = Guid.NewGuid(),
            IsDone = true,
            Note = "Hier sind alle beendeten Aufgaben. Du kansnt diese ebenfalls bearbeiten oder löschen.",
            IsRegularTask = false,
            IsWorkTask = true,
            IsPositive = false,
            CreationDate = now.DateTime,
            TaskDate = now.Date,
            Priority = TaskPriority.Wenig,
            BreakMinutes = 5
        }
    ];
}
