using System.Windows.Controls;
using System.Windows.Input;
using ToDoapp.ViewModels.Settings.General;

namespace ToDoapp.Views.Settings.General;

public partial class StartupReminderSettingsView : UserControl
{
    public StartupReminderSettingsView()
    {
        InitializeComponent();
    }

    private void StartupInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox textBox) return;
        if (DataContext is not StartupReminderSettingsViewModel vm) return;
        vm.AddStartupReminderCommand.Execute(textBox.Text);
        textBox.Clear();
        e.Handled = true;
    }

    private void ScheduledInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox textBox) return;
        if (DataContext is not StartupReminderSettingsViewModel vm) return;
        vm.AddScheduledReminderCommand.Execute(textBox.Text);
        textBox.Clear();
        e.Handled = true;
    }
}
