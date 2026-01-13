using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linguibuddy.Models;

public class Flashcard
{
    [Key] public int Id { get; set; }

    public int CollectionItemId { get; set; }

    [ForeignKey(nameof(CollectionItemId))] public CollectionItem? Item { get; set; }

    public DateTime NextReviewDate { get; set; } = DateTime.UtcNow;

    public DateTime LastReviewDate { get; set; } = DateTime.UtcNow;

    // Do algorytmu SM-2 (SuperMemo 2) (jest ich jakoś 18 wzięto wersję OG)
    public int Interval { get; set; } = 0;

    public int Repetitions { get; set; } = 0;

    public double EaseFactor { get; set; } = 2.5;
}