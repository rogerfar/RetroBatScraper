using CommunityToolkit.Mvvm.ComponentModel;

namespace RetroBatScraper.ViewModels;

public partial class UserInfoViewModel : ObservableObject
{
    [ObservableProperty]
    public String _username = "";

    [ObservableProperty]
    public Int32 _userId = 0;

    [ObservableProperty]
    public Int32 _userLevel;

    [ObservableProperty]
    public Int32 _contributionLevel;

    [ObservableProperty]
    public Int32 _platformMediaUploads;

    [ObservableProperty]
    public Int32 _infoTextUploads;

    [ObservableProperty]
    public Int32 _romAssociations;

    [ObservableProperty]
    public Int32 _gameMediaUploads;

    [ObservableProperty]
    public Int32? _approvedProposals;

    [ObservableProperty]
    public Int32? _rejectedProposals;

    [ObservableProperty]
    public Int32? _rejectionRate;

    [ObservableProperty]
    public Int32 _maxThreads;

    [ObservableProperty]
    public Int32 _maxDownloadSpeed;

    [ObservableProperty]
    public Int32 _requestsToday;

    [ObservableProperty]
    public Int32 _failedRequestsToday;

    [ObservableProperty]
    public Int32 _maxRequestsPerMinute;

    [ObservableProperty]
    public Int32 _maxRequestsPerDay;

    [ObservableProperty]
    public Int32 _maxFailedRequestsPerDay;

    [ObservableProperty]
    public Int32 _visitCount;

    [ObservableProperty]
    public DateTimeOffset? _lastVisitDate;

    [ObservableProperty]
    public String? _favoriteRegion;
}