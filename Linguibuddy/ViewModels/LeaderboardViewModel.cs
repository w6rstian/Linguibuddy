using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Linguibuddy.Resources.Strings;

namespace Linguibuddy.ViewModels;

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
                UserName = string.IsNullOrEmpty(u.UserName) ? AppResources.Anonymous : u.UserName,
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
            Debug.WriteLine($"Error loading leaderboard: {ex.Message}");
            await ShowAlertAsync(AppResources.Error, AppResources.LeaderboardErrorMessage, AppResources.OK);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected virtual Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }
}

public class LeaderboardItem
{
    public int Rank { get; set; }
    public string UserName { get; set; }
    public int Points { get; set; }
    public bool IsTop3 => Rank <= 3;
}
