using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using ToDoapp.Models;
using ToDoapp.Services;
using ToDoapp.Views;
using ToDoapp.Widgets;

namespace ToDoapp.Views;

public partial class MainWindow : Window
{
    private readonly TodoService _todoService;
    private readonly WidgetOpacityManager _opacityManager;
    private ObservableCollection<TodoItem> _todoItems = new();
    private ObservableCollection<TodoItem> _pendingTasks = new();
    private ObservableCollection<TodoItem> _completedTasks = new();
    private ObservableCollection<TodoItem> _deletedTasks = new();
    private ObservableCollection<TodoItem> _archivedTasks = new();
    private ObservableCollection<ArchivedGroup> _archivedGroups = new();
    private Dictionary<string, bool> _archivedExpansionStates = new();
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
    private bool _canPersistData = true;
    private string? _startupPersistenceMessage;
    private string? _startupPersistenceDetail;

    public MainWindow()
    {
        InitializeComponent();
        _opacityManager = WidgetOpacityManager.Instance;
        _todoService = new TodoService();

        AdjustFontSizeForDpi();
        InitializeData();
        InitializeTimer();
        InitializeSystemTray();
        ShowStartupPersistenceNotificationIfNeeded();

        _opacityManager.OpacityChanged += OnOpacityChanged;
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
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
            HolidayCalendarService.Instance.WarmupStatusChanged -= OnHolidayWarmupStatusChanged;
        }

        base.OnClosed(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateWindowFrameState();
    }
}
