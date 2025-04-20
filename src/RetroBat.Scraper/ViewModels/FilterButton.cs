using CommunityToolkit.Mvvm.ComponentModel;

namespace RetroBat.Scraper.ViewModels;

public partial class FilterButton : ObservableObject
{
    [ObservableProperty]
    private String _text = "";

    [ObservableProperty]
    private FilterState _state = FilterState.Undefined;

    public FilterType Type { get; set; }
}

public enum FilterState
{
    Undefined,
    Active,
    Inactive
}

public enum FilterType
{
    AllTags,
    Language,
    Region,
    Tag,
    Aftermarket,
    Beta,
    Demo,
    Kiosk,
    Unlicensed,
    Prototype,
    Test
}