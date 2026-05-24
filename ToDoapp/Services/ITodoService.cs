using System.Collections.ObjectModel;
using ToDoapp.Models;

namespace ToDoapp.Services;

public interface ITodoService
{
    TodoLoadResult LoadTodos();

    TodoSaveResult SaveTodos(IEnumerable<TodoItem> todos);

    ObservableCollection<TodoItem> LoadTodosFromFile(string filePath);

    TodoImportMergeResult MergeImportedTodos(
        IEnumerable<TodoItem> existingTodos,
        IEnumerable<TodoItem> importedTodos);

    IReadOnlyList<TodoBackupInfo> GetBackupInfos();

    TodoRestoreResult RestoreFromBackup(string backupPath);

    void ExportTodosToFile(IEnumerable<TodoItem> todos, string filePath);
}
