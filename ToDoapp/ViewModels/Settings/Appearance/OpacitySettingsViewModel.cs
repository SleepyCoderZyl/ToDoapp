using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.ViewModels.Settings.Appearance;

/// <summary>
/// "透明度设置" 面板 ViewModel。
/// </summary>
public class OpacitySettingsViewModel : SettingsPageViewModel
{
    public override string Name => "透明度设置";
    public override string Description => "调整小组件的透明度";
    public override SettingCategory Category => SettingCategory.Appearance;

    private double _backgroundOpacity;
    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set
        {
            if (SetField(ref _backgroundOpacity, value))
            {
                WidgetOpacityManager.Instance.SetOpacity(value);
            }
        }
    }

    private double _contentOpacity;
    public double ContentOpacity
    {
        get => _contentOpacity;
        set
        {
            if (SetField(ref _contentOpacity, value))
            {
                WidgetOpacityManager.Instance.SetContentOpacity(value);
            }
        }
    }

    public OpacitySettingsViewModel()
    {
        _backgroundOpacity = WidgetOpacityManager.Instance.WidgetOpacity;
        _contentOpacity = WidgetOpacityManager.Instance.WidgetContentOpacity;
    }
}
