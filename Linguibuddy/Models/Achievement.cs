using System.ComponentModel.DataAnnotations;

namespace Linguibuddy.Models
{
    public class Achievement
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string LockedIconUrl { get; set; } = "lock_100dp_light.png";
        //public Dictionary<string, object> Requirements { get; set; } = [];
        public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    }
}
