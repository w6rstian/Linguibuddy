using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public partial class WordCollectionPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private WordCollection _selectedCollection;
        [ObservableProperty]
        private IEnumerable<WordCollection> _collections;


        private readonly IPopupService _popupService;
        private readonly CollectionService _collectionService;

        public WordCollectionPopupViewModel(CollectionService collectionService, IPopupService popupService)
        {
            _collectionService = collectionService;
            _popupService = popupService;

            LoadCollectionsAsync();
        }

        private async Task LoadCollectionsAsync()
        {
            Collections = await _collectionService.GetUserCollectionsAsync();
        }

        [RelayCommand]
        private async Task CollectionSelected()
        {
            if (SelectedCollection != null)
            {
                await _popupService.ClosePopupAsync<WordCollection?>(Shell.Current, SelectedCollection);
            }
            else
            {
                await _popupService.ClosePopupAsync<WordCollection?>(Shell.Current, null);
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await _popupService.ClosePopupAsync<WordCollection?>(Shell.Current, null);
        }
    }
}
