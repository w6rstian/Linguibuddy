using Linguibuddy.Interfaces;
using Linguibuddy.Models;

namespace Linguibuddy.Services;

public class SpacedRepetitionService : ISpacedRepetitionService
{
    // grade: 0-5 (jakość odpowiedzi)
    public void ProcessResult(Flashcard card, int grade)
    {
        // Aktualizacja Interwału i Liczby Powtórzeń
        if (grade >= SuperMemoGrade.PassingThreshold)
        {
            if (card.Repetitions == 0)
                card.Interval = 1;
            else if (card.Repetitions == 1)
                card.Interval = 6;
            else
                card.Interval = (int)Math.Round(card.Interval * card.EaseFactor);

            card.Repetitions++;
        }
        else
        {
            card.Repetitions = 0;
            card.Interval = 1;
        }

        // Wzór: EF = EF + (0.1 - (5 - grade) * (0.08 + (5 - grade) * 0.02))
        card.EaseFactor = card.EaseFactor + (0.1 - (5 - grade) * (0.08 + (5 - grade) * 0.02));

        if (card.EaseFactor < 1.3) card.EaseFactor = 1.3;

        card.LastReviewDate = DateTime.UtcNow;
        card.NextReviewDate = DateTime.UtcNow.AddDays(card.Interval);
    }
}