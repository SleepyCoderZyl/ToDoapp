using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ToDoapp.Services;
using ToDoapp.ViewModels.Settings.General;

namespace ToDoapp.Views.Settings.General;

public partial class HotKeySettingsView : UserControl
{
    private const string RecordingPlaceholder = "按下快捷键组合...";

    private readonly Dictionary<HotKeyEntryViewModel, TextBox> _inputBoxes = new();
    private readonly HashSet<HotKeyEntryViewModel> _recording = new();

    public HotKeySettingsView()
    {
        InitializeComponent();
    }

    private void HotKeyRow_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not HotKeyEntryViewModel entry)
        {
            return;
        }

        if (fe.FindName("HotKeyInput") as TextBox is not { } textBox)
        {
            return;
        }

        _inputBoxes[entry] = textBox;
        SyncTextBoxState(textBox, entry);
        entry.PropertyChanged += Entry_PropertyChanged;

        textBox.PreviewMouseLeftButtonDown += (s, _) => OnInputMouseDown(textBox, entry);
        textBox.PreviewKeyDown += (s, args) => OnInputKeyDown(textBox, entry, args);
        textBox.LostFocus += (s, _) => OnInputLostFocus(textBox, entry);
    }

    private void HotKeyRow_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not HotKeyEntryViewModel entry)
        {
            return;
        }

        entry.PropertyChanged -= Entry_PropertyChanged;
        _inputBoxes.Remove(entry);
        _recording.Remove(entry);
    }

    private void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not HotKeyEntryViewModel entry) return;
        if (e.PropertyName is not (nameof(HotKeyEntryViewModel.HotKeyDisplay)
                                   or nameof(HotKeyEntryViewModel.IsControlsEnabled)))
        {
            return;
        }
        if (_recording.Contains(entry)) return;
        if (!_inputBoxes.TryGetValue(entry, out var textBox)) return;
        SyncTextBoxState(textBox, entry);
    }

    private void OnInputMouseDown(TextBox textBox, HotKeyEntryViewModel entry)
    {
        if (!entry.IsControlsEnabled || _recording.Contains(entry)) return;
        BeginRecord(textBox, entry);
    }

    private void OnInputKeyDown(TextBox textBox, HotKeyEntryViewModel entry, KeyEventArgs e)
    {
        if (!_recording.Contains(entry)) return;

        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            CancelRecord(textBox, entry);
            return;
        }

        var modifiers = GetCurrentHotKeyModifiers();
        if (modifiers != 0 && TryGetVirtualKey(e.Key, out var key))
        {
            _recording.Remove(entry);
            entry.ApplyHotKeyCommand.Execute(new uint[] { modifiers, key });
            SyncTextBoxState(textBox, entry);
            return;
        }

        if (modifiers != 0)
        {
            textBox.Text = GlobalHotKeyService.GetHotKeyDisplayText(modifiers, 0) + "+...";
        }
    }

    private void OnInputLostFocus(TextBox textBox, HotKeyEntryViewModel entry)
    {
        CancelRecord(textBox, entry);
    }

    private void BeginRecord(TextBox textBox, HotKeyEntryViewModel entry)
    {
        _recording.Add(entry);
        textBox.Text = RecordingPlaceholder;
        textBox.Foreground = (Brush)Application.Current.Resources["PrimaryBrush"];
        textBox.Focus();
    }

    private void CancelRecord(TextBox textBox, HotKeyEntryViewModel entry)
    {
        if (!_recording.Remove(entry)) return;
        SyncTextBoxState(textBox, entry);
    }

    private void SyncTextBoxState(TextBox textBox, HotKeyEntryViewModel entry)
    {
        textBox.Text = entry.HotKeyDisplay;
        textBox.Foreground = (Brush)Application.Current.Resources["PrimaryBrush"];
    }

    private static uint GetCurrentHotKeyModifiers()
    {
        uint modifiers = 0;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= GlobalHotKeyService.MOD_CONTROL;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= GlobalHotKeyService.MOD_SHIFT;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= GlobalHotKeyService.MOD_ALT;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers |= GlobalHotKeyService.MOD_WIN;
        return modifiers;
    }

    private static bool TryGetVirtualKey(Key keyInput, out uint key)
    {
        key = keyInput switch
        {
            >= Key.A and <= Key.Z => (uint)(0x41 + (keyInput - Key.A)),
            >= Key.D0 and <= Key.D9 => (uint)(0x30 + (keyInput - Key.D0)),
            >= Key.NumPad0 and <= Key.NumPad9 => (uint)(0x60 + (keyInput - Key.NumPad0)),
            >= Key.F1 and <= Key.F24 => (uint)(0x70 + (keyInput - Key.F1)),
            Key.Space => GlobalHotKeyService.VK_SPACE,
            Key.Back => GlobalHotKeyService.VK_BACK,
            Key.Tab => GlobalHotKeyService.VK_TAB,
            Key.Return => GlobalHotKeyService.VK_RETURN,
            Key.Home => GlobalHotKeyService.VK_HOME,
            Key.End => GlobalHotKeyService.VK_END,
            Key.Left => GlobalHotKeyService.VK_LEFT,
            Key.Up => GlobalHotKeyService.VK_UP,
            Key.Right => GlobalHotKeyService.VK_RIGHT,
            Key.Down => GlobalHotKeyService.VK_DOWN,
            Key.Insert => GlobalHotKeyService.VK_INSERT,
            Key.Delete => GlobalHotKeyService.VK_DELETE,
            Key.PageUp => GlobalHotKeyService.VK_PRIOR,
            Key.PageDown => GlobalHotKeyService.VK_NEXT,
            _ => 0
        };

        return key != 0;
    }
}
