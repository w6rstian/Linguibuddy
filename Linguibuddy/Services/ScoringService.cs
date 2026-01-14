using System.Diagnostics;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;

namespace Linguibuddy.Services;

public class ScoringService : IScoringService
{
    private readonly IAppUserService _appUserService;

    private readonly Dictionary<GameType, int> _basePoints = new()
    {
        { GameType.AudioQuiz, 10 },
        { GameType.ImageQuiz, 10 },
        { GameType.SentenceQuiz, 20 }, // Bonus za poziom trudności
        { GameType.SpeakingQuiz, 20 }, // Bonus za poziom trudności
        { GameType.Hangman, 50 }
    };

    private readonly ICollectionService _collectionService;

    public ScoringService(ICollectionService collectionService, IAppUserService appUserService)
    {
        _collectionService = collectionService;
        _appUserService = appUserService;
    }

    /// <summary>
    ///     Oblicza punkty za pojedynczą poprawną odpowiedź.
    /// </summary>
    public int CalculatePoints(GameType gameType, DifficultyLevel difficulty)
    {
        if (!_basePoints.TryGetValue(gameType, out var points))
            points = 10;

        if (gameType == GameType.SentenceQuiz || gameType == GameType.SpeakingQuiz)
        {
            var bonusFactor = GetDifficultyBonus(difficulty);

            points = points + (int)(points * bonusFactor);
        }

        return points;
    }

    /// <summary>
    ///     Zapisuje wynik do kolekcji oraz dodaje punkty do użytkownika.
    /// </summary>
    public async Task SaveResultsAsync(WordCollection collection, GameType gameType, int correctAnswers,
        int totalQuestions, int totalPointsEarned)
    {
        if (totalQuestions == 0) return;

        var ratio = (double)correctAnswers / totalQuestions;
        var now = DateTime.UtcNow;

        switch (gameType)
        {
            case GameType.AudioQuiz:
                collection.AudioLastScore = ratio;
                if (ratio > collection.AudioBestScore) collection.AudioBestScore = ratio;
                collection.AudioLastPlayed = now;
                break;

            case GameType.ImageQuiz:
                collection.ImageLastScore = ratio;
                if (ratio > collection.ImageBestScore) collection.ImageBestScore = ratio;
                collection.ImageLastPlayed = now;
                break;

            case GameType.SentenceQuiz:
                collection.SentenceLastScore = ratio;
                if (ratio > collection.SentenceBestScore) collection.SentenceBestScore = ratio;
                collection.SentenceLastPlayed = now;
                break;

            case GameType.SpeakingQuiz:
                collection.SpeakingLastScore = ratio;
                if (ratio > collection.SpeakingBestScore) collection.SpeakingBestScore = ratio;
                collection.SpeakingLastPlayed = now;
                break;

            case GameType.Hangman:
                collection.HangmanLastScore = ratio;
                if (ratio > collection.HangmanBestScore) collection.HangmanBestScore = ratio;
                collection.HangmanLastPlayed = now;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null);
        }

        try
        {
            await _collectionService.UpdateCollectionAsync(collection);

            if (totalPointsEarned > 0) await _appUserService.AddUserPointsAsync(totalPointsEarned);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd zapisu wyników: {ex.Message}");
        }
    }

    private double GetDifficultyBonus(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.A1 => 0.0, // Bez bonusu
            DifficultyLevel.A2 => 0.1, // +10%
            DifficultyLevel.B1 => 0.3, // +30%
            DifficultyLevel.B2 => 0.5, // +50%
            DifficultyLevel.C1 => 0.8, // +80%
            DifficultyLevel.C2 => 1.0, // +100%
            _ => 0.0
        };
    }
}