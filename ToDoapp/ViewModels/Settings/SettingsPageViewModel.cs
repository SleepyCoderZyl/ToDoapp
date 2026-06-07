using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToDoapp.Models;

namespace ToDoapp.ViewModels.Settings;

/// <summary>
/// 设置页 ViewModel 抽象基类。所有 6 个设置面板的 ViewModel 都继承自此类。
/// 取代原先 <c>SettingItem</c> 模型 + <c>SettingItem.ContentControl: FrameworkElement</c> 反模式，
/// 通过 <see cref="ViewModels.SettingsPageViewModel"/> 派生类型 + Implicit DataTemplate
/// 让 WPF 自动选择对应的 UserControl 渲染。
/// </summary>
public abstract class SettingsPageViewModel : INotifyPropertyChanged
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract SettingCategory Category { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
