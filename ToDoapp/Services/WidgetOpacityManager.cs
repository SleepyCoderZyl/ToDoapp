using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ToDoapp.Services;

public class WidgetOpacityManager : INotifyPropertyChanged
{
    private static WidgetOpacityManager? _instance;
    public static WidgetOpacityManager Instance => _instance ??= new WidgetOpacityManager();

    private readonly SettingsService _settingsService;
    private double _widgetOpacity = 0.8;
    private double _widgetContentOpacity = 1.0;
    private bool _isWidgetMode = false;
    private bool _isMousePassThroughEnabled = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double WidgetOpacity
    {
        get => _widgetOpacity;
        set
        {
            if (Math.Abs(_widgetOpacity - value) > 0.001)
            {
                _widgetOpacity = Math.Clamp(value, 0.2, 1.0);
                OnPropertyChanged();
                OnOpacityChanged();
                _settingsService.UpdateWidgetOpacity(_widgetOpacity);
            }
        }
    }

    public double WidgetContentOpacity
    {
        get => _widgetContentOpacity;
        set
        {
            if (Math.Abs(_widgetContentOpacity - value) > 0.001)
            {
                _widgetContentOpacity = Math.Clamp(value, 0.2, 1.0);
                OnPropertyChanged();
                OnContentOpacityChanged();
                _settingsService.UpdateWidgetContentOpacity(_widgetContentOpacity);
            }
        }
    }

    public bool IsWidgetMode
    {
        get => _isWidgetMode;
        set
        {
            if (_isWidgetMode != value)
            {
                _isWidgetMode = value;
                OnPropertyChanged();
                OnOpacityChanged();
            }
        }
    }

    public bool IsMousePassThroughEnabled
    {
        get => _isMousePassThroughEnabled;
        set
        {
            if (_isMousePassThroughEnabled != value)
            {
                _isMousePassThroughEnabled = value;
                OnPropertyChanged();
                OnOpacityChanged();
            }
        }
    }

    public double EffectiveOpacity
    {
        get
        {
            if (!IsWidgetMode)
                return 1.0;
            
            return IsMousePassThroughEnabled ? _widgetOpacity : 1.0;
        }
    }

    public double EffectiveContentOpacity
    {
        get
        {
            if (!IsWidgetMode)
                return 1.0;
            
            return _widgetContentOpacity;
        }
    }

    public event EventHandler<double>? OpacityChanged;
    public event EventHandler<double>? ContentOpacityChanged;

    private WidgetOpacityManager()
    {
        _settingsService = SettingsService.Instance;
        _widgetOpacity = _settingsService.Settings.WidgetOpacity;
        _widgetContentOpacity = _settingsService.Settings.WidgetContentOpacity;
    }

    private void OnOpacityChanged()
    {
        OnPropertyChanged(nameof(EffectiveOpacity));
        OpacityChanged?.Invoke(this, EffectiveOpacity);
    }

    private void OnContentOpacityChanged()
    {
        OnPropertyChanged(nameof(EffectiveContentOpacity));
        ContentOpacityChanged?.Invoke(this, EffectiveContentOpacity);
    }

    public void SetOpacity(double value)
    {
        WidgetOpacity = value;
    }

    public void SetContentOpacity(double value)
    {
        WidgetContentOpacity = value;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
