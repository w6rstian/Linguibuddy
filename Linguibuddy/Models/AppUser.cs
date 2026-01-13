using Linguibuddy.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Linguibuddy.Models
{
    /// <summary>
    /// AppUser is an extension to FirebaseAuth.User.
    /// It holds extra data related to the user that the app needs.
    /// </summary>
    public class AppUser
    {
        [Key]
        public required string Id { get; set; } // same thing as Firebase Uid

        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.A1;

        public int Points { get; set; } = 0;

        public int BestLearningStreak { get; set; } = 0;

        public ICollection<UserLearningDay> LearningDays { get; set; } = new List<UserLearningDay>();
        public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    }
}
