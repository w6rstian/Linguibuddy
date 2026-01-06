using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Models
{
    public class UserAchievement
    {
        public int Id { get; set; }
        public int AchievementId { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime UnlockDate { get; set; }

        // public float progress { get; set;}
    }
}
