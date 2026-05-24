using System.Collections.ObjectModel;
using ToDoapp.Models;
using ToDoapp.Services;
using ToDoapp.ViewModels;
using Xunit;

namespace ToDoapp.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void AddSmartTask_WithPlainTitle_AddsPendingTaskAndSaves()
    {
        var service = new FakeTodoService();
        var viewModel = new MainWindowViewModel(service);

        var todoItem = viewModel.AddSmartTask("整理 WPF 架构", null);

        Assert.NotNull(todoItem);
        Assert.Single(viewModel.TodoItems);
        Assert.Single(viewModel.PendingTasks);
        Assert.Equal("整理 WPF 架构", viewModel.PendingTasks[0].Title);
        Assert.Equal(1, service.SaveCount);
        Assert.Contains("已添加", viewModel.StatusMessage);
    }

    [Fact]
    public void MoveRestoreAndPermanentDelete_UpdateCollections()
    {
        var service = new FakeTodoService();
        var viewModel = new MainWindowViewModel(service);
        var item = new TodoItem { Title = "可删除任务", CreatedDate = DateTime.Now };
        viewModel.ReplaceTodoItems([item]);

        viewModel.MoveTaskToTrash(item);

        Assert.Empty(viewModel.PendingTasks);
        Assert.Single(viewModel.DeletedTasks);

        viewModel.RestoreDeletedTask(item);

        Assert.Single(viewModel.PendingTasks);
        Assert.Empty(viewModel.DeletedTasks);

        viewModel.PermanentlyDeleteTask(item);

        Assert.Empty(viewModel.TodoItems);
        Assert.Empty(viewModel.PendingTasks);
    }

    [Fact]
    public void ArchiveAndUnarchive_MoveTaskBetweenCollections()
    {
        var service = new FakeTodoService();
        var viewModel = new MainWindowViewModel(service);
        var item = new TodoItem
        {
            Title = "已完成任务",
            CreatedDate = DateTime.Now,
            IsCompleted = true,
            CompletedDate = DateTime.Now
        };
        viewModel.ReplaceTodoItems([item]);

        viewModel.ArchiveTask(item);

        Assert.Empty(viewModel.CompletedTasks);
        Assert.Single(viewModel.ArchivedTasks);

        viewModel.UnarchiveTask(item);

        Assert.Single(viewModel.CompletedTasks);
        Assert.Empty(viewModel.ArchivedTasks);
    }

    [Fact]
    public void CleanupExpiredTrashItems_RemovesOnlyExpiredDeletedTasks()
    {
        var service = new FakeTodoService();
        var viewModel = new MainWindowViewModel(service);
        var expired = new TodoItem
        {
            Title = "过期垃圾",
            CreatedDate = DateTime.Now.AddDays(-10),
            IsDeleted = true,
            DeletedDate = new DateTime(2026, 5, 1)
        };
        var recent = new TodoItem
        {
            Title = "近期垃圾",
            CreatedDate = DateTime.Now,
            IsDeleted = true,
            DeletedDate = new DateTime(2026, 5, 8)
        };
        viewModel.ReplaceTodoItems([expired, recent]);

        var removedCount = viewModel.CleanupExpiredTrashItems(new DateTime(2026, 5, 9));

        Assert.Equal(1, removedCount);
        Assert.DoesNotContain(expired, viewModel.TodoItems);
        Assert.Contains(recent, viewModel.TodoItems);
    }

    [Fact]
    public void BuildTaskClipboardText_IncludesDueDateWhenPresent()
    {
        var item = new TodoItem
        {
            Title = "提交重构",
            CreatedDate = DateTime.Now,
            DueDate = new DateTime(2026, 5, 25)
        };

        var text = MainWindowViewModel.BuildTaskClipboardText(item);

        Assert.Equal("提交重构 2026-05-25", text);
    }

    private sealed class FakeTodoService : ITodoService
    {
        public int SaveCount { get; private set; }

        public TodoLoadResult LoadTodos()
        {
            return TodoLoadResult.Success([]);
        }

        public TodoSaveResult SaveTodos(IEnumerable<TodoItem> todos)
        {
            SaveCount++;
            return TodoSaveResult.Success(null);
        }

        public ObservableCollection<TodoItem> LoadTodosFromFile(string filePath)
        {
            return [];
        }

        public TodoImportMergeResult MergeImportedTodos(
            IEnumerable<TodoItem> existingTodos,
            IEnumerable<TodoItem> importedTodos)
        {
            var mergedTodos = new ObservableCollection<TodoItem>(existingTodos.Concat(importedTodos));
            var importedCount = importedTodos.Count();
            return new TodoImportMergeResult(mergedTodos, importedCount, importedCount, 0);
        }

        public IReadOnlyList<TodoBackupInfo> GetBackupInfos()
        {
            return [];
        }

        public TodoRestoreResult RestoreFromBackup(string backupPath)
        {
            return TodoRestoreResult.Failure("not implemented in test fake");
        }

        public void ExportTodosToFile(IEnumerable<TodoItem> todos, string filePath)
        {
        }
    }
}
