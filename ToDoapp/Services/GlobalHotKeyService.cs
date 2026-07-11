using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ToDoapp.Services;

public enum GlobalHotKeyAction
{
    QuickAdd,
    ShowHome,
    HideWidget,
    ToggleWidgetMode
}

public class GlobalHotKeyService : IDisposable
{
    private Window _window;
    private IntPtr _windowHandle;
    private HwndSource? _source;
    private bool _isDisposed;
    private const int WM_HOTKEY = 0x0312;
    private int _currentId = 0;
    private readonly Dictionary<GlobalHotKeyAction, int> _registeredHotKeyIds = [];
    private readonly Dictionary<int, GlobalHotKeyAction> _registeredActions = [];
    private readonly Dictionary<GlobalHotKeyAction, (uint Modifiers, uint Key)> _registeredHotKeyBindings = [];
    private readonly Dictionary<GlobalHotKeyAction, (uint Modifiers, uint Key)> _pendingRegistrations = [];

    public event Action<GlobalHotKeyAction>? HotKeyPressed;
    public bool IsRegistered => _registeredHotKeyIds.Count > 0;

    public GlobalHotKeyService(Window window)
    {
        _window = window;
        
        var interopHelper = new WindowInteropHelper(_window);
        if (interopHelper.Handle != IntPtr.Zero)
        {
            _windowHandle = interopHelper.Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(WndProc);
        }
        else
        {
            _window.SourceInitialized += OnSourceInitialized;
        }
        
        _window.Closed += OnWindowClosed;
    }

    public int RegisterHotKey(uint modifiers, uint key)
    {
        return RegisterHotKey(GlobalHotKeyAction.QuickAdd, modifiers, key);
    }

    public int RegisterHotKey(GlobalHotKeyAction action, uint modifiers, uint key)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            _pendingRegistrations[action] = (modifiers, key);
            return -1;
        }

        return RegisterHotKeyInternal(action, modifiers, key);
    }

    private int RegisterHotKeyInternal(GlobalHotKeyAction action, uint modifiers, uint key)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return -1;
        }
        
        if (_registeredHotKeyIds.TryGetValue(action, out var registeredId) &&
            _registeredHotKeyBindings.TryGetValue(action, out var registeredBinding) &&
            registeredBinding == (modifiers, key))
        {
            return registeredId;
        }

        try
        {
            int id = ++_currentId;
            bool success = RegisterHotKey(_windowHandle, id, modifiers, key);

            if (!success)
            {
                return -1;
            }

            if (_registeredHotKeyIds.TryGetValue(action, out registeredId) && !UnregisterHotKey(registeredId))
            {
                UnregisterHotKey(_windowHandle, id);
                return -1;
            }

            _registeredHotKeyIds[action] = id;
            _registeredActions[id] = action;
            _registeredHotKeyBindings[action] = (modifiers, key);
            return id;
        }
        catch
        {
            return -1;
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Dispose();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _windowHandle = new WindowInteropHelper(_window).Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);

        if (_pendingRegistrations.Count > 0)
        {
            foreach (var pendingRegistration in _pendingRegistrations.ToArray())
            {
                RegisterHotKeyInternal(
                    pendingRegistration.Key,
                    pendingRegistration.Value.Modifiers,
                    pendingRegistration.Value.Key);
            }

            _pendingRegistrations.Clear();
        }
    }

    public bool UnregisterHotKey(int id)
    {
        if (_windowHandle == IntPtr.Zero)
            return false;

        bool result = UnregisterHotKey(_windowHandle, id);
        if (result && _registeredActions.TryGetValue(id, out var action))
        {
            _registeredActions.Remove(id);
            _registeredHotKeyIds.Remove(action);
            _registeredHotKeyBindings.Remove(action);
        }
        return result;
    }

    public bool UnregisterHotKey(GlobalHotKeyAction action)
    {
        _pendingRegistrations.Remove(action);
        return _registeredHotKeyIds.TryGetValue(action, out var id) && UnregisterHotKey(id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_registeredActions.TryGetValue(id, out var action))
            {
                HotKeyPressed?.Invoke(action);
            }

            handled = true;
        }
        return IntPtr.Zero;
    }

    public void UnregisterAll()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            _pendingRegistrations.Clear();
            return;
        }

        foreach (var id in _registeredActions.Keys.ToArray())
        {
            UnregisterHotKey(id);
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            UnregisterAll();
            _source?.RemoveHook(WndProc);
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public const uint VK_Z = 0x5A;
    public const uint VK_SPACE = 0x20;
    public const uint VK_BACK = 0x08;
    public const uint VK_TAB = 0x09;
    public const uint VK_RETURN = 0x0D;
    public const uint VK_HOME = 0x24;
    public const uint VK_END = 0x23;
    public const uint VK_LEFT = 0x25;
    public const uint VK_UP = 0x26;
    public const uint VK_RIGHT = 0x27;
    public const uint VK_DOWN = 0x28;
    public const uint VK_INSERT = 0x2D;
    public const uint VK_DELETE = 0x2E;
    public const uint VK_PRIOR = 0x21;
    public const uint VK_NEXT = 0x22;

    public static string GetHotKeyDisplayText(uint modifiers, uint key)
    {
        var parts = new System.Collections.Generic.List<string>();
        
        if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & MOD_WIN) != 0) parts.Add("Win");
        
        if (key != 0)
        {
            string keyName = key switch
            {
                >= 0x41 and <= 0x5A => ((char)key).ToString(),
                >= 0x30 and <= 0x39 => ((char)key).ToString(),
                >= 0x60 and <= 0x69 => $"Num{key - 0x60}",
                >= 0x70 and <= 0x87 => $"F{key - 0x6F}",
                VK_SPACE => "Space",
                VK_BACK => "Backspace",
                VK_TAB => "Tab",
                VK_RETURN => "Enter",
                VK_HOME => "Home",
                VK_END => "End",
                VK_LEFT => "Left",
                VK_UP => "Up",
                VK_RIGHT => "Right",
                VK_DOWN => "Down",
                VK_INSERT => "Insert",
                VK_DELETE => "Delete",
                VK_PRIOR => "PageUp",
                VK_NEXT => "PageDown",
                _ => $"0x{key:X}"
            };
            parts.Add(keyName);
        }
        
        return string.Join(" + ", parts);
    }

    public string GetHotKeyDisplayText()
    {
        var settings = SettingsService.Instance.Settings;
        return GetHotKeyDisplayText(settings.HotKeyModifiers, settings.HotKeyKey);
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
