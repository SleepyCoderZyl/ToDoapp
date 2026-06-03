using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using ToDoapp.Models;
using ToDoapp.Services;
using ToDoapp.ViewModels;
using ToDoapp.Views;
using ToDoapp.Widgets;

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
        UpdateThemeToggleButton();
        _opacityManager = WidgetOpacityManager.Instance;
        _viewModel = new MainWindowViewModel(new TodoService());
        _todoService = _viewModel.TodoService;
        DataContext = _viewModel;

        AdjustFontSizeForDpi();
        InitializeData();
        InitializeTimer();
        InitializeSystemTray();
        ShowStartupPersistenceNotificationIfNeeded();

        _opacityManager.OpacityChanged += OnOpacityChanged;
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
        ThemeService.Instance.ThemeChanged += OnThemeChanged;
        HolidayCalendarService.Instance.WarmupStatusChanged += OnHolidayWarmupStatusChanged;
        SourceInitialized += OnMainWindowSourceInitialized;

        _isLoaded = true;

        if (HolidayCalendarService.Instance.LastWarmupStatus is { } warmupStatus)
        {
            UpdateStatus(warmupStatus.ShortMessage, warmupStatus.DetailMessage);
        }
    }

    private void OnMainWindowSourceInitialized(object? sender, EventArgs e)
    {
        ApplyNativeWindowAppearance();
        InitializeGlobalHotKey();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_isLoaded)
        {
            SaveData();
            _mainTimer.Stop();
            _systemTrayService?.Dispose();
            _globalHotKeyService?.Dispose();
            ThemeService.Instance.ThemeChanged -= OnThemeChanged;
            HolidayCalendarService.Instance.WarmupStatusChanged -= OnHolidayWarmupStatusChanged;
        }

        base.OnClosed(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateWindowFrameState();
    }

    public Window TrayHostWindow => this;
}
