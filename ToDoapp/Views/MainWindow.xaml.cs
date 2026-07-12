using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using ToDoapp.Models;
using ToDoapp.Services;
using ToDoapp.ViewModels;
using ToDoapp.Views;

namespace ToDoapp.Views;

public partial class MainWindow : Window, ITrayActionHandler
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ITodoService _todoService;
    private readonly WidgetOpacityManager _opacityManager;
    private ObservableCollection<TodoItem> _todoItems => _viewModel.TodoItems;
    private ObservableCollection<TodoItem> _pendingTasks => _viewModel.PendingTasks;
    private ObservableCollection<TodoItem> _completedTasks => _viewModel.CompletedTasks;
    private ObservableCollection<TodoItem> _deletedTasks => _viewModel.DeletedTasks;
    private ObservableCollection<TodoItem> _archivedTasks => _viewModel.ArchivedTasks;
    private ObservableCollection<ArchivedGroup> _archivedGroups => _viewModel.ArchivedGroups;
    private DispatcherTimer _mainTimer = new();
    private readonly DispatcherTimer _statusResetTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private DateTime _lastAutoSaveTime = DateTime.Now;
    private DateTime _lastOverdueCheckTime = DateTime.Now;
    private DateTime _lastTrashCleanupTime = DateTime.Now;
    private DateTime _lastAutoArchiveCheckTime = DateTime.Now;
    private DateTime _lastTimeSensitiveRefreshDate = DateTime.Now.Date;
    private SystemTrayService? _systemTrayService;
    private GlobalHotKeyService? _globalHotKeyService;
    private bool _isWidgetMode;
    private double _widgetWindowWidth = 280;
    private double _widgetWindowHeight = 360;
    private double _widgetWindowLeft;
    private double _widgetWindowTop;
    private bool _isLoaded;
    private WidgetWindow? _widgetWindow;
    private QuickAddWindow? _quickAddWindow;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private HolidayCalendarService? _holidayCalendarService;
    private bool _canPersistData
    {
        get => _viewModel.CanPersistData;
        set => _viewModel.CanPersistData = value;
    }

    private string? _startupPersistenceMessage
    {
        get => _viewModel.StartupPersistenceMessage;
        set => _viewModel.StartupPersistenceMessage = value;
    }

    private string? _startupPersistenceDetail
    {
        get => _viewModel.StartupPersistenceDetail;
        set => _viewModel.StartupPersistenceDetail = value;
    }

    public MainWindow()
    {
        InitializeComponent();
        _statusResetTimer.Tick += StatusResetTimer_Tick;
        UpdateThemeToggleButton();
        _opacityManager = WidgetOpacityManager.Instance;
        _viewModel = new MainWindowViewModel(new TodoService());
        _todoService = _viewModel.TodoService;
        _canPersistData = false;
        DataContext = _viewModel;

        AdjustFontSizeForDpi();
        InitializeEmptyState();

        _opacityManager.OpacityChanged += OnOpacityChanged;
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
        SettingsService.Instance.SettingsSaveFailed += OnSettingsSaveFailed;
        ThemeService.Instance.ThemeChanged += OnThemeChanged;
        SourceInitialized += OnMainWindowSourceInitialized;
        ContentRendered += OnMainWindowContentRendered;

        _isLoaded = true;

    }

    private void OnMainWindowSourceInitialized(object? sender, EventArgs e)
    {
        ApplyNativeWindowAppearance();
        StartupDiagnostics.Mark("SourceInitialized");
    }

    private async void OnMainWindowContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnMainWindowContentRendered;
        StartupDiagnostics.Mark("ContentRendered (interactive first frame)");

        try
        {
            InitializeSystemTray();
            InitializeGlobalHotKey();
            await InitializeDataAsync(_lifetimeCancellation.Token);
            StartupDiagnostics.Mark("Todo data available");

            _ = Dispatcher.BeginInvoke(
                CheckAndAutoArchiveCompletedTasks,
                DispatcherPriority.Background);

            _holidayCalendarService = await Task.Run(
                () => HolidayCalendarService.Instance,
                _lifetimeCancellation.Token);
            _holidayCalendarService.WarmupStatusChanged += OnHolidayWarmupStatusChanged;
            if (_holidayCalendarService.LastWarmupStatus is { } warmupStatus)
            {
                UpdateStatus(warmupStatus.ShortMessage, warmupStatus.DetailMessage);
            }

            InitializeTimer();
            ShowStartupPersistenceNotificationIfNeeded();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"完成启动初始化失败: {ex.Message}");
            UpdateStatus("启动初始化失败", ex.Message);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_isLoaded)
        {
            _lifetimeCancellation.Cancel();
            SaveData();
            _mainTimer.Stop();
            _statusResetTimer.Stop();
            _systemTrayService?.Dispose();
            _globalHotKeyService?.Dispose();
            ThemeService.Instance.ThemeChanged -= OnThemeChanged;
            if (_holidayCalendarService != null)
            {
                _holidayCalendarService.WarmupStatusChanged -= OnHolidayWarmupStatusChanged;
            }
            SettingsService.Instance.SettingsSaveFailed -= OnSettingsSaveFailed;
        }

        _lifetimeCancellation.Dispose();

        base.OnClosed(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateWindowFrameState();
    }

    public Window TrayHostWindow => this;
}
