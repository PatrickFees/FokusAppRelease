using System.ComponentModel.DataAnnotations;

namespace FokusApp.Core.DataBase.Entities;

public class RingtonesEntity
{
    [Key]
    public Guid Id { get; set; }

    public string? RingtoneFilePath { get; set; }
}
