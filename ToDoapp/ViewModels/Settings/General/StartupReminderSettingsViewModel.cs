using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.ViewModels.Settings.General;

/// <summary>
/// "弹窗提示" 设置面板 ViewModel。
/// 含两个 Tab：启动弹窗（一次性）、定时弹窗（每天）。
/// </summary>
public class StartupReminderSettingsViewModel : SettingsPageViewModel
{
    public override string Name => "弹窗提示";
    public override string Description => "配置启动与定时弹出的提醒内容";
    public override SettingCategory Category => SettingCategory.General;

    private readonly AppSettings _settings;

    public ObservableCollection<StartupReminderEntry> StartupReminderItems { get; }
    public ObservableCollection<StartupReminderEntry> ScheduledReminderItems { get; }

    public string StartupEmptyStateText => "还没有启动弹窗内容，新增一条试试。";
    public string ScheduledEmptyStateText => "还没有定时提示内容，新增一条试试。";
    public string StartupHintText => "提示窗口会按这里的顺序展示已启用的提醒项。";
    public string ScheduledHintText => "同一分钟的多条提醒会合并到一个弹窗；如果错过当天时间点，不会补发。";

    public string StartupDescription => "开机自启拉起时弹出你配置的提醒内容。";
    public string ScheduledDescription => "应用运行或驻留托盘时，每条提醒会在各自设置的时间每天弹出一次。";

    public string StartupStatusText => _isStartupReminderEnabled ? "已启用" : "已禁用";
    public string ScheduledStatusText => _isScheduledReminderEnabled ? "已启用" : "已禁用";

    private bool _isStartupReminderEnabled;
    public bool IsStartupReminderEnabled
    {
        get => _isStartupReminderEnabled;
        set
        {
            if (SetField(ref _isStartupReminderEnabled, value))
            {
                _settings.ShowStartupReminderOnAutoStart = value;
                SettingsService.Instance.SaveSettings();
                OnPropertyChanged(nameof(StartupStatusText));
            }
        }
    }

    private bool _isScheduledReminderEnabled;
    public bool IsScheduledReminderEnabled
    {
        get => _isScheduledReminderEnabled;
        set
        {
            if (SetField(ref _isScheduledReminderEnabled, value))
            {
                _settings.ShowScheduledReminderDaily = value;
                SettingsService.Instance.SaveSettings();
                OnPropertyChanged(nameof(ScheduledStatusText));
            }
        }
    }

    private string _scheduledTime;
    public string ScheduledTime
    {
        get => _scheduledTime;
        set
        {
            if (SetField(ref _scheduledTime, value))
            {
                CommitScheduledTime();
            }
        }
    }

    public RelayCommand<string> AddStartupReminderCommand { get; }
    public RelayCommand<string> AddScheduledReminderCommand { get; }
    public RelayCommand<StartupReminderEntry> RemoveStartupReminderCommand { get; }
    public RelayCommand<StartupReminderEntry> RemoveScheduledReminderCommand { get; }
    public RelayCommand<StartupReminderEntry> ToggleStartupReminderCommand { get; }
    public RelayCommand<StartupReminderEntry> ToggleScheduledReminderCommand { get; }
    public RelayCommand<(StartupReminderEntry Entry, string Time)> CommitScheduledItemTimeCommand { get; }

    public StartupReminderSettingsViewModel()
    {
        _settings = SettingsService.Instance.Settings;
        _settings.Normalize();

        _isStartupReminderEnabled = _settings.ShowStartupReminderOnAutoStart;
        _isScheduledReminderEnabled = _settings.ShowScheduledReminderDaily;
        _scheduledTime = string.IsNullOrWhiteSpace(_settings.ScheduledReminderTime) ? "09:00" : _settings.ScheduledReminderTime;

        StartupReminderItems = new ObservableCollection<StartupReminderEntry>(_settings.StartupReminderItems);
        ScheduledReminderItems = new ObservableCollection<StartupReminderEntry>(_settings.ScheduledReminderItems);

        // 列表变更时同步到 settings 并持久化
        StartupReminderItems.CollectionChanged += OnStartupCollectionChanged;
        ScheduledReminderItems.CollectionChanged += OnScheduledCollectionChanged;

        // 每条目属性变化（如 IsEnabled / ScheduledTime）也持久化
        foreach (var item in StartupReminderItems)
        {
            item.PropertyChanged += OnStartupItemPropertyChanged;
        }
        foreach (var item in ScheduledReminderItems)
        {
            item.PropertyChanged += OnScheduledItemPropertyChanged;
        }

        AddStartupReminderCommand = new RelayCommand<string>(text => AddStartupReminder(text));
        AddScheduledReminderCommand = new RelayCommand<string>(text => AddScheduledReminder(text));
        RemoveStartupReminderCommand = new RelayCommand<StartupReminderEntry>(entry => RemoveStartupReminder(entry));
        RemoveScheduledReminderCommand = new RelayCommand<StartupReminderEntry>(entry => RemoveScheduledReminder(entry));
        ToggleStartupReminderCommand = new RelayCommand<StartupReminderEntry>(entry => { if (entry != null) entry.IsEnabled = !entry.IsEnabled; });
        ToggleScheduledReminderCommand = new RelayCommand<StartupReminderEntry>(entry => { if (entry != null) entry.IsEnabled = !entry.IsEnabled; });
        CommitScheduledItemTimeCommand = new RelayCommand<(StartupReminderEntry Entry, string Time)>(tuple => CommitScheduledItemTime(tuple.Entry, tuple.Time));
    }

    private void OnStartupCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (StartupReminderEntry item in e.NewItems)
            {
                item.PropertyChanged += OnStartupItemPropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (StartupReminderEntry item in e.OldItems)
            {
                item.PropertyChanged -= OnStartupItemPropertyChanged;
            }
        }
        SyncStartupItems();
        SettingsService.Instance.SaveSettings();
    }

    private void OnScheduledCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (StartupReminderEntry item in e.NewItems)
            {
                item.PropertyChanged += OnScheduledItemPropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (StartupReminderEntry item in e.OldItems)
            {
                item.PropertyChanged -= OnScheduledItemPropertyChanged;
            }
        }
        SyncScheduledItems();
        SettingsService.Instance.SaveSettings();
    }

    private void OnStartupItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        SettingsService.Instance.SaveSettings();
    }

    private void OnScheduledItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        SettingsService.Instance.SaveSettings();
    }

    private void SyncStartupItems()
    {
        _settings.StartupReminderItems.Clear();
        foreach (var item in StartupReminderItems)
        {
            _settings.StartupReminderItems.Add(item);
        }
    }

    private void SyncScheduledItems()
    {
        _settings.ScheduledReminderItems.Clear();
        foreach (var item in ScheduledReminderItems)
        {
            _settings.ScheduledReminderItems.Add(item);
        }
    }

    private void AddStartupReminder(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        StartupReminderItems.Add(new StartupReminderEntry
        {
            Text = text.Trim(),
            IsEnabled = true,
            ScheduledTime = string.Empty
        });
    }

    private void AddScheduledReminder(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // 解析时间；无效时回退到 09:00 并同步回绑定的 _scheduledTime，让 UI 反映修正
        var scheduledTime = ScheduledTime;
        TimeOnly parsedTime;
        if (!StartupReminderService.TryParseScheduledReminderTime(scheduledTime, out parsedTime))
        {
            scheduledTime = "09:00";
            parsedTime = new System.TimeOnly(9, 0);
            _scheduledTime = scheduledTime;
            OnPropertyChanged(nameof(ScheduledTime));
        }
        else
        {
            scheduledTime = parsedTime.ToString("HH:mm");
        }

        AppendScheduledReminder(text.Trim(), scheduledTime);
    }

    private void AppendScheduledReminder(string text, string time)
    {
        ScheduledReminderItems.Add(new StartupReminderEntry
        {
            Text = text,
            IsEnabled = true,
            ScheduledTime = time
        });
    }

    private void RemoveStartupReminder(StartupReminderEntry? entry)
    {
        if (entry is null) return;
        StartupReminderItems.Remove(entry);
    }

    private void RemoveScheduledReminder(StartupReminderEntry? entry)
    {
        if (entry is null) return;
        ScheduledReminderItems.Remove(entry);
    }

    private void CommitScheduledItemTime(StartupReminderEntry? entry, string? timeText)
    {
        if (entry is null) return;
        var input = (timeText ?? string.Empty).Trim();
        if (!StartupReminderService.TryParseScheduledReminderTime(input, out var parsedTime))
        {
            // 还原为当前值或默认
            entry.ScheduledTime = string.IsNullOrWhiteSpace(entry.ScheduledTime) ? "09:00" : entry.ScheduledTime;
            return;
        }

        entry.ScheduledTime = parsedTime.ToString("HH:mm");
        SettingsService.Instance.SaveSettings();
    }

    private void CommitScheduledTime()
    {
        if (!StartupReminderService.TryParseScheduledReminderTime(_scheduledTime, out var parsedTime))
        {
            _scheduledTime = string.IsNullOrWhiteSpace(_settings.ScheduledReminderTime) ? "09:00" : _settings.ScheduledReminderTime;
            OnPropertyChanged(nameof(ScheduledTime));
            return;
        }

        _settings.ScheduledReminderTime = parsedTime.ToString("HH:mm");
        _scheduledTime = _settings.ScheduledReminderTime;
        SettingsService.Instance.SaveSettings();
    }
}
