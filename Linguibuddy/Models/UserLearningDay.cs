using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Models
{
    public class UserLearningDay
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AppUserId { get; set; }
        [ForeignKey(nameof(AppUserId))]
        public AppUser AppUser { get; set; }

        public DateTime Date { get; set; }
        public bool Learned { get; set; } = true;
    }
}
