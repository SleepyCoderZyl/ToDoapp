using System.Collections.ObjectModel;
using ToDoapp.Models;

namespace ToDoapp.Services;

public interface ITodoService
{
    TodoLoadResult LoadTodos();

    /// <summary>
    /// 在后台加载待办数据，避免阻塞 UI 线程。
    /// </summary>
    Task<TodoLoadResult> LoadTodosAsync(CancellationToken cancellationToken);

    TodoSaveResult SaveTodos(IEnumerable<TodoItem> todos);

    ObservableCollection<TodoItem> LoadTodosFromFile(string filePath);

    TodoImportMergeResult MergeImportedTodos(
        IEnumerable<TodoItem> existingTodos,
        IEnumerable<TodoItem> importedTodos);

    IReadOnlyList<TodoBackupInfo> GetBackupInfos();

    TodoRestoreResult RestoreFromBackup(string backupPath);

    void ExportTodosToFile(IEnumerable<TodoItem> todos, string filePath);
}
