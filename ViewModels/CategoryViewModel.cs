using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShinySuite.ViewModels;

public partial class CategoryViewModel : ObservableObject
{
    public string Name { get; }

    [ObservableProperty] private bool _isActive;

    public ObservableCollection<PokemonTileViewModel> Tiles { get; } = [];

    public Action? ActivateRequested { get; set; }

    public CategoryViewModel(string name) => Name = name;

    [RelayCommand]
    private void Activate() => ActivateRequested?.Invoke();
}
