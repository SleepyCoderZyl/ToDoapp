using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.ViewModels.Settings.Appearance;

/// <summary>
/// "小组件置顶" 面板 ViewModel。
/// </summary>
public class AlwaysOnTopSettingsViewModel : SettingsPageViewModel
{
    public override string Name => "小组件置顶";
    public override string Description => "使小组件始终显示在最上层";
    public override SettingCategory Category => SettingCategory.Appearance;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
            {
                SettingsService.Instance.UpdateWidgetAlwaysOnTop(value);
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => _isEnabled ? "已启用" : "已禁用";

    public AlwaysOnTopSettingsViewModel()
    {
        _isEnabled = SettingsService.Instance.Settings.WidgetAlwaysOnTop;
    }
}
