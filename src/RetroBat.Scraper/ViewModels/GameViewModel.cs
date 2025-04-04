﻿using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using RetroBatScraper.Models;

namespace RetroBatScraper.ViewModels;

public partial class GameViewModel(Game game) : ObservableObject
{
    public Guid GameId { get; } = game.GameId;
    public Boolean InitialSelected { get; } = game.IsSelected;

    [ObservableProperty]
    private Boolean _isSelected;

    public String Name => game.Name;
    public String FileName => game.FileNameWithExtension;
    public String FileNameWithoutExtension => game.FileNameWithoutExtension;
    public String? Url => GameLink?.Url;
    public String? ScreenScraperData => game.ScreenScraperData;
    public String? GameLinkData => game.GameLinkData;

    public String ScrapeStatus
    {
        get
        {
            return game.ScrapeStatus switch
            {
                GameScrapeStatus.NotScraped => "Not scraped",
                GameScrapeStatus.InProgress => "In progress",
                GameScrapeStatus.Success => "Success",
                GameScrapeStatus.Error => "Error",
                GameScrapeStatus.NotFound => "Not found",
                _ => "Unknown"
            };
        }
    }

    public ScreenScraperFR.Game? ScreenScraperGame
    {
        get
        {
            if (ScreenScraperData != null)
            {
                return JsonSerializer.Deserialize<ScreenScraperFR.Game>(ScreenScraperData);
            }

            return null;
        }
    }

    public GameLink? GameLink
    {
        get
        {
            if (GameLinkData != null)
            {
                try
                {
                    return JsonSerializer.Deserialize<GameLink>(GameLinkData);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }

    public String Languages
    {
        get
        {
            if (GameLink != null)
            {
                return String.Join(", ", GameLink.Languages);
            }

            return "";
        }
    }

    public String Regions
    {
        get
        {
            if (GameLink != null)
            {
                return String.Join(", ", GameLink.Regions);
            }

            return "";
        }
    }

    public String Tags
    {
        get
        {
            if (GameLink != null)
            {
                return String.Join(", ", GameLink.Tags);
            }

            return "";
        }
    }
}