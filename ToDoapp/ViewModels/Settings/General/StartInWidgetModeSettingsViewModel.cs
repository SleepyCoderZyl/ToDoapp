using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.ViewModels.Settings.General;

/// <summary>
/// "启动模式" 设置面板 ViewModel。
/// </summary>
public class StartInWidgetModeSettingsViewModel : SettingsPageViewModel
{
    public override string Name => "启动模式";
    public override string Description => "启动时自动进入小组件模式";
    public override SettingCategory Category => SettingCategory.General;

    private bool _startInWidgetMode;
    public bool StartInWidgetMode
    {
        get => _startInWidgetMode;
        set
        {
            if (SetField(ref _startInWidgetMode, value))
            {
                SettingsService.Instance.Settings.StartInWidgetMode = value;
                SettingsService.Instance.SaveSettings();
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => _startInWidgetMode ? "小组件模式" : "主页面模式";

    public StartInWidgetModeSettingsViewModel()
    {
        _startInWidgetMode = SettingsService.Instance.Settings.StartInWidgetMode;
    }
}
