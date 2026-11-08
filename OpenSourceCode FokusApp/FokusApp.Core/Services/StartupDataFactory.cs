using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.Services;

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
}
