using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface ISpacedRepetitionService
{
    void ProcessResult(Flashcard card, int grade);
}