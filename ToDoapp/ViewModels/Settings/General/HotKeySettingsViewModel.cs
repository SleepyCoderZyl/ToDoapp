using System.Collections.ObjectModel;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.ViewModels.Settings.General;

/// <summary>
/// 单条快捷键编辑项。View 端负责按键捕获 → 转 (modifiers, key) → 调 <see cref="ApplyHotKeyCommand"/>。
/// </summary>
public class HotKeyEntryViewModel : ObservableObjectBase
{
    public string Title { get; }

    private readonly uint _defaultModifiers;
    private readonly uint _defaultKey;
    private readonly Action<uint, uint> _saveCallback;
    private readonly Action<bool>? _enabledChangedCallback;

    private bool? _isEnabled;
    public bool? IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
            {
                _enabledChangedCallback?.Invoke(value ?? false);
                OnPropertyChanged(nameof(IsControlsEnabled));
            }
        }
    }

    public bool IsControlsEnabled => _isEnabled != false;

    private uint _modifiers;
    private uint _key;
    public string HotKeyDisplay => GlobalHotKeyService.GetHotKeyDisplayText(_modifiers, _key);

    public RelayCommand<uint[]> ApplyHotKeyCommand { get; }
    public RelayCommand ResetHotKeyCommand { get; }

    public HotKeyEntryViewModel(
        string title,
        bool? initialEnabled,
        uint initialModifiers,
        uint initialKey,
        uint defaultModifiers,
        uint defaultKey,
        Action<uint, uint> saveCallback,
        Action<bool>? enabledChangedCallback = null)
    {
        Title = title;
        _isEnabled = initialEnabled;
        _modifiers = initialModifiers;
        _key = initialKey;
        _defaultModifiers = defaultModifiers;
        _defaultKey = defaultKey;
        _saveCallback = saveCallback;
        _enabledChangedCallback = enabledChangedCallback;

        ApplyHotKeyCommand = new RelayCommand<uint[]>(parts =>
        {
            if (parts is null || parts.Length < 2) return;
            var modifiers = parts[0];
            var key = parts[1];
            _modifiers = modifiers;
            _key = key;
            _saveCallback(modifiers, key);
            OnPropertyChanged(nameof(HotKeyDisplay));
        });

        ResetHotKeyCommand = new RelayCommand(() =>
        {
            _modifiers = _defaultModifiers;
            _key = _defaultKey;
            _saveCallback(_defaultModifiers, _defaultKey);
            OnPropertyChanged(nameof(HotKeyDisplay));
        });
    }
}

/// <summary>
/// "全局快捷键" 设置面板 ViewModel。包含 3 条快捷键：快速添加、显示主页、隐藏小组件。
/// </summary>
public class HotKeySettingsViewModel : SettingsPageViewModel
{
    public override string Name => "全局快捷键";
    public override string Description => "设置快速添加和显示主页";
    public override SettingCategory Category => SettingCategory.General;

    public ObservableCollection<HotKeyEntryViewModel> Entries { get; } = new();

    public HotKeySettingsViewModel()
    {
        var settings = SettingsService.Instance.Settings;

        Entries.Add(new HotKeyEntryViewModel(
            title: "快速添加",
            initialEnabled: settings.QuickAddHotKeyEnabled,
            initialModifiers: settings.HotKeyModifiers,
            initialKey: settings.HotKeyKey,
            defaultModifiers: GlobalHotKeyService.MOD_CONTROL | GlobalHotKeyService.MOD_SHIFT | GlobalHotKeyService.MOD_ALT,
            defaultKey: GlobalHotKeyService.VK_Z,
            saveCallback: (modifiers, key) =>
            {
                settings.HotKeyModifiers = modifiers;
                settings.HotKeyKey = key;
                SettingsService.Instance.SaveSettings();
            },
            enabledChangedCallback: enabled =>
            {
                settings.QuickAddHotKeyEnabled = enabled;
                SettingsService.Instance.SaveSettings();
            }));

        Entries.Add(new HotKeyEntryViewModel(
            title: "显示主页",
            initialEnabled: settings.ShowHomeHotKeyEnabled,
            initialModifiers: settings.ShowHomeHotKeyModifiers,
            initialKey: settings.ShowHomeHotKeyKey,
            defaultModifiers: GlobalHotKeyService.MOD_CONTROL | GlobalHotKeyService.MOD_SHIFT | GlobalHotKeyService.MOD_ALT,
            defaultKey: 0x48,
            saveCallback: (modifiers, key) =>
            {
                settings.ShowHomeHotKeyModifiers = modifiers;
                settings.ShowHomeHotKeyKey = key;
                SettingsService.Instance.SaveSettings();
            },
            enabledChangedCallback: enabled =>
            {
                settings.ShowHomeHotKeyEnabled = enabled;
                SettingsService.Instance.SaveSettings();
            }));

        Entries.Add(new HotKeyEntryViewModel(
            title: "隐藏小组件",
            initialEnabled: settings.HideWidgetHotKeyEnabled,
            initialModifiers: settings.HideWidgetHotKeyModifiers,
            initialKey: settings.HideWidgetHotKeyKey,
            defaultModifiers: GlobalHotKeyService.MOD_CONTROL | GlobalHotKeyService.MOD_SHIFT | GlobalHotKeyService.MOD_ALT,
            defaultKey: 0x4D,
            saveCallback: (modifiers, key) =>
            {
                settings.HideWidgetHotKeyModifiers = modifiers;
                settings.HideWidgetHotKeyKey = key;
                SettingsService.Instance.SaveSettings();
            },
            enabledChangedCallback: enabled =>
            {
                settings.HideWidgetHotKeyEnabled = enabled;
                SettingsService.Instance.SaveSettings();
            }));

        Entries.Add(new HotKeyEntryViewModel(
            title: "切换小组件",
            initialEnabled: settings.ToggleWidgetModeHotKeyEnabled,
            initialModifiers: settings.ToggleWidgetModeHotKeyModifiers,
            initialKey: settings.ToggleWidgetModeHotKeyKey,
            defaultModifiers: GlobalHotKeyService.MOD_CONTROL | GlobalHotKeyService.MOD_SHIFT | GlobalHotKeyService.MOD_ALT,
            defaultKey: 0x57,
            saveCallback: (modifiers, key) =>
            {
                settings.ToggleWidgetModeHotKeyModifiers = modifiers;
                settings.ToggleWidgetModeHotKeyKey = key;
                SettingsService.Instance.SaveSettings();
            },
            enabledChangedCallback: enabled =>
            {
                settings.ToggleWidgetModeHotKeyEnabled = enabled;
                SettingsService.Instance.SaveSettings();
            }));
    }
}
