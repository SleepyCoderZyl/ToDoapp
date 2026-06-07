using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.ViewModels.Settings.General;

/// <summary>
/// "开机自启动" 设置面板 ViewModel。
/// </summary>
public class StartupSettingsViewModel : SettingsPageViewModel
{
    public override string Name => "开机自启动";
    public override string Description => "开机时自动启动应用程序";
    public override SettingCategory Category => SettingCategory.General;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
            {
                StartupService.Instance.SetAutoStart(value);
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => _isEnabled ? "已启用" : "已禁用";

    public StartupSettingsViewModel()
    {
        _isEnabled = StartupService.Instance.IsAutoStartEnabled;
    }
}
