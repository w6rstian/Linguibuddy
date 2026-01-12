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

        public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

        public DifficultyLevel PrefferedDifficulty { get; set; } = DifficultyLevel.A1;
    }
}
