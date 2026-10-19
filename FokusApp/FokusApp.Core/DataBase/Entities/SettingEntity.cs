using System.ComponentModel.DataAnnotations;

namespace FokusApp.Core.DataBase.Entities;

public class SettingEntity
{
    [Key]
    public Guid Id { get; set; }
    public int Theme { get; set; }
    public int Language { get; set; }
    public Guid RingtoneId { get; set; }
    public RingtonesEntity Ringtone { get; set; } = null!;
}
