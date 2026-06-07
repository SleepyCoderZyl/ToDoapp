using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ToDoapp.Models;
using ToDoapp.ViewModels.Settings;
using ToDoapp.ViewModels.Settings.Appearance;
using ToDoapp.ViewModels.Settings.General;

namespace ToDoapp.ViewModels;

/// <summary>
/// 设置窗口主 ViewModel。
/// <para>持有 6 个 <see cref="SettingsPageViewModel"/> 子 VM，按 Category 分组显示在左侧 ListBox。</para>
/// <para>右侧 ContentControl 绑定 <see cref="CurrentPage"/>，由 App.xaml 中注册的 Implicit DataTemplate
/// 自动选择对应的 UserControl 渲染。</para>
/// </summary>
public class SettingsViewModel : ObservableObjectBase
{
    public ObservableCollection<SettingsPageViewModel> Pages { get; } = new();

    public IEnumerable<SettingsPageViewModel> GeneralPages =>
        Pages.Where(p => p.Category == SettingCategory.General);

    public IEnumerable<SettingsPageViewModel> AppearancePages =>
        Pages.Where(p => p.Category == SettingCategory.Appearance);

    private SettingsPageViewModel? _currentPage;
    public SettingsPageViewModel? CurrentPage
    {
        get => _currentPage;
        set => SetField(ref _currentPage, value);
    }

    public SettingsViewModel()
    {
        Pages.Add(new StartupSettingsViewModel());
        Pages.Add(new StartupReminderSettingsViewModel());
        Pages.Add(new StartInWidgetModeSettingsViewModel());
        Pages.Add(new HotKeySettingsViewModel());
        Pages.Add(new OpacitySettingsViewModel());
        Pages.Add(new AlwaysOnTopSettingsViewModel());

        // 默认选中第一项
        CurrentPage = Pages.FirstOrDefault();
    }
}
