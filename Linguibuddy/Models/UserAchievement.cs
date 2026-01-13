using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linguibuddy.Models;

public class UserAchievement
{
    [Key] public int Id { get; set; }

    public string AppUserId { get; set; }

    [ForeignKey(nameof(AppUserId))] public AppUser AppUser { get; set; }

    public int AchievementId { get; set; }

    [ForeignKey(nameof(AchievementId))] public Achievement Achievement { get; set; }

    public bool IsUnlocked { get; set; }
    public DateTime UnlockDate { get; set; }

    public float Progress { get; set; }
}