using System.Windows;
using System.Windows.Input;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class StartupReminderWindow : Window
{
    private readonly MainWindow _mainWindow;

    public StartupReminderWindow(MainWindow mainWindow, ReminderSnapshot snapshot)
    {
        InitializeComponent();
        NativeWindowAppearance.ConfigureDialog(this, "DialogBaseSurfaceBrush");
        _mainWindow = mainWindow;
        DataContext = snapshot;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenMainWindowButton_Click(object sender, RoutedEventArgs e)
    {
        _mainWindow.RestoreMainWindow();
        Close();
    }
}
