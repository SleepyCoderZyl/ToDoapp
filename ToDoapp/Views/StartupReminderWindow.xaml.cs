using System.Windows;
using System.Windows.Input;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class StartupReminderWindow : Window
{
    private readonly MainWindow _mainWindow;

    public StartupReminderWindow(MainWindow mainWindow, StartupReminderSnapshot snapshot)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        DataContext = snapshot;
    }

    public string GreetingText
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var greeting = hour switch
            {
                >= 5 and < 12 => "早安",
                >= 12 and < 18 => "午安",
                _ => "晚上好"
            };

            return $"{greeting}，今天先看这几件事";
        }
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
