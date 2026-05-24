using System.Windows;

namespace ToDoapp.Services;

public interface ITrayActionHandler
{
    Window TrayHostWindow { get; }

    bool IsWidgetWindowVisible { get; }

    bool IsWidgetMode();

    bool IsMousePassThroughEnabled();

    bool ToggleWidgetWindowVisibility();

    void ToggleWidgetMode();

    void ToggleMousePassThrough();

    void RestoreMainWindow();

    void ShowSettingsWindow();

    void ImportTodosFromJsonFile();

    void ExportTodosToJsonFile();

    void ShowBackupRecoveryDialog();

    void MinimizeHostWindow();

    void ExitApplication();
}
