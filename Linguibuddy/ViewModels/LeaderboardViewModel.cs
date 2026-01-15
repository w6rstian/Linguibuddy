using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using System.Collections.ObjectModel;

namespace Linguibuddy.ViewModels;

//TODO: backend for leaderboard or remove this page
public partial class LeaderboardViewModel : ObservableObject
{
    private readonly IAppUserService _appUserService;

    [ObservableProperty]
    private ObservableCollection<LeaderboardItem> _leaderboardItems = new();

    [ObservableProperty]
    private bool _isLoading;

    public LeaderboardViewModel(IAppUserService appUserService)
    {
        _appUserService = appUserService;
    }

    [RelayCommand]
    public async Task LoadLeaderboardAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var topUsers = await _appUserService.GetLeaderboardAsync();
            var items = topUsers.Select((u, index) => new LeaderboardItem
            {
                Rank = index + 1,
                UserName = string.IsNullOrEmpty(u.UserName) ? "Anonim" : u.UserName,
                Points = u.Points
            }).ToList();

            LeaderboardItems.Clear();
            foreach (var item in items)
            {
                LeaderboardItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", "Nie udało się pobrać rankingu.", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class LeaderboardItem
{
    public int Rank { get; set; }
    public string UserName { get; set; }
    public int Points { get; set; }
    public bool IsTop3 => Rank <= 3;
}
