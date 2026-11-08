using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.WeeklyReviewComponent;

public sealed class WeeklyReviewViewModel : ObservableObject, IWeeklyReviewViewModel, IDisposable
{
    private const int DaysInWeek = 7;
    private const double FullPercent = 100;

    private readonly IBalanceForYouToDoService _balanceService;
    private readonly IRewardingSettingsService _rewardingSettingsService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly IDayChangeService _dayChangeService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly ITaskCacheService _taskCacheService;
    private readonly TimeProvider _timeProvider;

    private int _weekOffset;
    private int _loadVersion;
    private WeekBalance? _week;

    public WeeklyReviewViewModel(
        IBalanceForYouToDoService balanceService,
        IRewardingSettingsService rewardingSettingsService,
        IAppStartupStateService appStartupStateService,
        IDayChangeService dayChangeService,
        IAppErrorCaptureService appErrorCaptureService,
        ITaskCacheService taskCacheService,
        TimeProvider timeProvider)
    {
        _balanceService = balanceService;
        _rewardingSettingsService = rewardingSettingsService;
        _appStartupStateService = appStartupStateService;
        _dayChangeService = dayChangeService;
        _appErrorCaptureService = appErrorCaptureService;
        _taskCacheService = taskCacheService;
        _timeProvider = timeProvider;

        _rewardingSettingsService.RewardingChanged += OnRewardingChanged;
        _dayChangeService.DayChanged += OnDayChanged;
        _taskCacheService.NormalTasksChanged += OnTasksChanged;
        _taskCacheService.DoneTasksChanged += OnTasksChanged;
        _taskCacheService.TemplatesChanged += OnTasksChanged;
    }

    public bool IsReviewEnabled => _rewardingSettingsService.Rewarding.ReviewsEnabled;

    public bool IsLoaded { get; private set; }

    public DateTime WeekStart => BalanceForYouToDoRules.GetWeekStart(Today).AddDays(_weekOffset * DaysInWeek);

    public DateTime WeekEnd => WeekStart.AddDays(DaysInWeek - 1);

    public bool IsCurrentWeek => _weekOffset == 0;

    public bool CanShowNextWeek => _weekOffset < 0;

    public bool IsEmpty => CreatedForYou + CreatedToDo + DoneForYou + DoneToDo == 0;

    public int GoalForYou => GoalParts.ForYou;

    public int GoalToDo => GoalParts.ToDo;

    public int CreatedForYou => _week?.Days.Sum(day => day.PlannedForYou) ?? 0;

    public int CreatedToDo => _week?.Days.Sum(day => day.PlannedToDo) ?? 0;

    public GoalProgress? CreatedProgress => ProgressOf(CreatedForYou, CreatedToDo);

    public int DoneForYou => _week?.DoneForYou ?? 0;

    public int DoneToDo => _week?.DoneToDo ?? 0;

    public GoalProgress? DoneProgress => ProgressOf(DoneForYou, DoneToDo);

    public IReadOnlyList<WeeklyReviewDay> Days { get; private set; } = [];

    public WeeklyReviewPeak? MostForYouDays => PeakOf(day => day.DoneForYou);

    public WeeklyReviewPeak? MostToDoDays => PeakOf(day => day.DoneToDo);

    private DateTime Today => _timeProvider.GetLocalNow().Date;

    private (int ForYou, int ToDo) GoalParts
    {
        get
        {
            var rewarding = _rewardingSettingsService.Rewarding;
            return BalanceForYouToDoRules.GetParts(rewarding.BalanceGoal, rewarding.CustomForYou, rewarding.CustomToDo);
        }
    }

    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();
        await LoadAsync();
    }

    public Task ShowPreviousWeekAsync()
    {
        _weekOffset--;
        return LoadAsync();
    }

    public Task ShowNextWeekAsync()
    {
        if (!CanShowNextWeek)
        {
            return Task.CompletedTask;
        }

        _weekOffset++;
        return LoadAsync();
    }

    public Task ShowCurrentWeekAsync()
    {
        _weekOffset = 0;
        return LoadAsync();
    }

    public void Dispose()
    {
        _taskCacheService.NormalTasksChanged -= OnTasksChanged;
        _taskCacheService.DoneTasksChanged -= OnTasksChanged;
        _taskCacheService.TemplatesChanged -= OnTasksChanged;
        _rewardingSettingsService.RewardingChanged -= OnRewardingChanged;
        _dayChangeService.DayChanged -= OnDayChanged;
    }

    private async Task LoadAsync()
    {

        var loadVersion = ++_loadVersion;

        try
        {
            var week = await _balanceService.GetWeekAsync(WeekStart);
            if (loadVersion != _loadVersion)
            {
                return;
            }

            _week = week;
            Days = BuildDays(week);
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(WeeklyReviewViewModel)}.{nameof(LoadAsync)}");
        }

        if (loadVersion == _loadVersion)
        {
            NotifyWeekDataChanged();
        }
    }

    private GoalProgress? ProgressOf(int forYou, int toDo)
    {
        if (forYou + toDo == 0)
        {
            return null;
        }

        var rewarding = _rewardingSettingsService.Rewarding;
        return BalanceForYouToDoRules.GetProgress(forYou, toDo, rewarding.BalanceGoal, rewarding.CustomForYou, rewarding.CustomToDo);
    }

    private WeeklyReviewPeak? PeakOf(Func<WeeklyReviewDay, int> count)
    {
        var highest = Days.Count == 0 ? 0 : Days.Max(count);
        if (highest == 0)
        {
            return null;
        }

        return new WeeklyReviewPeak(highest, Days.Where(day => count(day) == highest).Select(day => day.Date).ToList());
    }

    private static List<WeeklyReviewDay> BuildDays(WeekBalance week)
    {
        var highest = Math.Max(1, week.Days.Max(day => day.DoneTotal));

        return week.Days
            .Select(day => new WeeklyReviewDay(
                day.Date,
                day.DoneForYou,
                day.DoneToDo,
                day.DoneForYou * FullPercent / highest,
                day.DoneToDo * FullPercent / highest))
            .ToList();
    }

    private void OnTasksChanged(object? sender, EventArgs e)
    {
        _ = LoadAsync();
    }

    private void OnRewardingChanged(object? sender, EventArgs e)
    {

        _ = LoadAsync();
    }

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {

        if (e.CurrentDay.DayOfWeek == DayOfWeek.Monday && _weekOffset < 0)
        {
            _weekOffset--;
        }

        _ = LoadAsync();
    }

    private void NotifyWeekDataChanged()
    {
        OnPropertyChanged(nameof(IsReviewEnabled));
        OnPropertyChanged(nameof(IsLoaded));
        OnPropertyChanged(nameof(WeekStart));
        OnPropertyChanged(nameof(WeekEnd));
        OnPropertyChanged(nameof(IsCurrentWeek));
        OnPropertyChanged(nameof(CanShowNextWeek));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(GoalForYou));
        OnPropertyChanged(nameof(GoalToDo));
        OnPropertyChanged(nameof(CreatedForYou));
        OnPropertyChanged(nameof(CreatedToDo));
        OnPropertyChanged(nameof(CreatedProgress));
        OnPropertyChanged(nameof(DoneForYou));
        OnPropertyChanged(nameof(DoneToDo));
        OnPropertyChanged(nameof(DoneProgress));
        OnPropertyChanged(nameof(Days));
        OnPropertyChanged(nameof(MostForYouDays));
        OnPropertyChanged(nameof(MostToDoDays));
    }
}
