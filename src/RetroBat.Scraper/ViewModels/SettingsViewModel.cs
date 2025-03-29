using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RetroBatScraper.Services;
using ScreenScraperFR;

namespace RetroBatScraper.ViewModels;

public partial class SettingsViewModel(SettingsService settingsService) : ObservableObject
{
    private readonly WeakReference<ICloseWindow> _windowReference = new(null!);

    [ObservableProperty]
    private String _retroBatPath = String.Empty;

    [ObservableProperty]
    private String _screenScraperDevId = String.Empty;

    [ObservableProperty]
    private String _screenScraperDevPassword = String.Empty;

    [ObservableProperty]
    private String _screenScraperUserName = String.Empty;

    [ObservableProperty]
    private String _screenScraperUserPassword = String.Empty;

    [ObservableProperty]
    private Boolean _isSaving;

    [ObservableProperty]
    private Boolean _isTesting;

    [ObservableProperty]
    private String? _testResult;

    public void SetWindow(ICloseWindow window)
    {
        _windowReference.SetTarget(window);
    }

    [RelayCommand]
    private async Task LoadSettings()
    {
        var settings = await settingsService.GetAllSettingsAsync();

        RetroBatPath = settings.First(s => s.Key == "RetroBatPath").Value;
        ScreenScraperDevId = settings.First(s => s.Key == "ScreenScraperDevId").Value;
        ScreenScraperDevPassword = settings.First(s => s.Key == "ScreenScraperDevPassword").Value;
        ScreenScraperUserName = settings.First(s => s.Key == "ScreenScraperUserName").Value;
        ScreenScraperUserPassword = settings.First(s => s.Key == "ScreenScraperUserPassword").Value;
    }

    [RelayCommand]
    private async Task Test()
    {
        if (IsTesting)
        {
            return;
        }

        TestResult = null;
        IsTesting = true;

        try
        {
            var client = new ScreenScraperFRClient("RetroBatScraper",
                                                   ScreenScraperDevId,
                                                   ScreenScraperDevPassword,
                                                   ScreenScraperUserName,
                                                   ScreenScraperUserPassword);

            var userInfo = await client.GetUserInfo() ?? throw new("Invalid credentials");

            TestResult = $"Successfully connected as: {userInfo.Username}";
        }
        catch (Exception ex)
        {
            TestResult = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        IsSaving = true;

        try
        {
            await SaveSetting("RetroBatPath", RetroBatPath);
            await SaveSetting("ScreenScraperDevId", ScreenScraperDevId);
            await SaveSetting("ScreenScraperDevPassword", ScreenScraperDevPassword);
            await SaveSetting("ScreenScraperUserName", ScreenScraperUserName);
            await SaveSetting("ScreenScraperUserPassword", ScreenScraperUserPassword);

            // Close the window
            if (_windowReference.TryGetTarget(out var window))
            {
                window.Close();
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task SaveSetting(String key, String value)
    {
        await settingsService.SaveSettingAsync(new()
        {
            SettingId = Guid.NewGuid(),
            Key = key,
            Type = "String",
            Value = value
        });
    }
}