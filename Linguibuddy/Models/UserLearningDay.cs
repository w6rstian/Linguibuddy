using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linguibuddy.Models;

public class UserLearningDay
{
    [Key] public int Id { get; set; }

    [Required] public string AppUserId { get; set; }

    [ForeignKey(nameof(AppUserId))] public AppUser AppUser { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;
    public bool Learned { get; set; } = true;
}