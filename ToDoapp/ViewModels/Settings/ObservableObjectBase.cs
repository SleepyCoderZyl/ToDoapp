using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ToDoapp.ViewModels.Settings;

/// <summary>
/// 非"设置页"用途的 ViewModel 基类。仅提供 <see cref="INotifyPropertyChanged"/> 与 <see cref="SetField{T}"/> 工具。
/// 真正的设置页请继承 <see cref="SettingsPageViewModel"/>。
/// </summary>
public abstract class ObservableObjectBase : INotifyPropertyChanged
{
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
