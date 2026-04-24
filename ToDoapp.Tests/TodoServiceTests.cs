using System.Collections.ObjectModel;
using System.Text.Json;
using ToDoapp.Models;
using ToDoapp.Services;
using Xunit;

namespace ToDoapp.Tests;

public class TodoServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly TodoService _todoService;
    private readonly string _backupDirectory;

    public TodoServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ToDoappTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _todoService = new TodoService(_testDirectory);
        _backupDirectory = Path.Combine(_testDirectory, "backups");
    }

    [Fact]
    public void LoadTodos_MissingFile_ReturnsSuccessWithEmptyCollection()
    {
        var result = _todoService.LoadTodos();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsRecoveredFromBackup);
        Assert.Empty(result.Todos);
    }

    [Fact]
    public void LoadTodos_CorruptedPrimaryWithBackup_RestoresLatestBackup()
    {
        var validTodo = new TodoItem
        {
            Title = "恢复测试",
            CreatedDate = new DateTime(2026, 4, 24, 9, 0, 0),
            IsCompleted = false
        };

        var firstSave = _todoService.SaveTodos(new ObservableCollection<TodoItem> { validTodo });
        Assert.True(firstSave.IsSuccess);

        validTodo.IsCompleted = true;
        validTodo.CompletedDate = new DateTime(2026, 4, 24, 10, 0, 0);
        var secondSave = _todoService.SaveTodos(new ObservableCollection<TodoItem> { validTodo });
        Assert.True(secondSave.IsSuccess);

        var primaryFilePath = Path.Combine(_testDirectory, "todos.json");
        File.WriteAllText(primaryFilePath, "{ invalid json");

        var result = _todoService.LoadTodos();

        Assert.True(result.IsSuccess);
        Assert.True(result.IsRecoveredFromBackup);
        Assert.Single(result.Todos);
        Assert.False(result.Todos[0].IsCompleted);

        var restored = _todoService.LoadTodos();
        Assert.True(restored.IsSuccess);
        Assert.False(restored.IsRecoveredFromBackup);
        Assert.False(restored.Todos[0].IsCompleted);
    }

    [Fact]
    public void LoadTodos_CorruptedPrimaryWithoutBackup_ReturnsFailure()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "todos.json"), "{ invalid json");

        var result = _todoService.LoadTodos();

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Todos);
        Assert.Contains("待办数据解析失败", result.ErrorMessage);
    }

    [Fact]
    public void SaveTodos_WhenPrimaryExists_CreatesBackupAndWritesReadablePrimary()
    {
        var firstItems = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "第一版",
                CreatedDate = new DateTime(2026, 4, 24, 9, 0, 0)
            }
        };

        var secondItems = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "第二版",
                CreatedDate = new DateTime(2026, 4, 24, 10, 0, 0)
            }
        };

        Assert.True(_todoService.SaveTodos(firstItems).IsSuccess);
        var saveResult = _todoService.SaveTodos(secondItems);

        Assert.True(saveResult.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(saveResult.BackupPath));
        Assert.True(File.Exists(saveResult.BackupPath!));

        var loaded = _todoService.LoadTodos();
        Assert.True(loaded.IsSuccess);
        Assert.Single(loaded.Todos);
        Assert.Equal("第二版", loaded.Todos[0].Title);
    }

    [Fact]
    public void SaveTodos_WhenContentUnchanged_DoesNotCreateBackup()
    {
        var items = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "稳定内容",
                CreatedDate = new DateTime(2026, 4, 24, 9, 0, 0)
            }
        };

        var firstSave = _todoService.SaveTodos(items);
        Assert.True(firstSave.IsSuccess);

        var primaryPath = Path.Combine(_testDirectory, "todos.json");
        var originalContent = File.ReadAllText(primaryPath);

        var secondSave = _todoService.SaveTodos(items);

        Assert.True(secondSave.IsSuccess);
        Assert.Null(secondSave.BackupPath);
        Assert.Equal(originalContent, File.ReadAllText(primaryPath));
        Assert.Empty(Directory.GetFiles(_backupDirectory, "todos-*.json"));
    }

    [Fact]
    public void LoadTodosFromFile_AllowsHistoricalExpiredDueDate()
    {
        var filePath = Path.Combine(_testDirectory, "legacy.json");
        var document = JsonSerializer.Serialize(new[]
        {
            new
            {
                Title = "旧任务",
                IsCompleted = false,
                CreatedDate = new DateTime(2021, 1, 1, 8, 0, 0),
                CompletedDate = (DateTime?)null,
                DueDate = new DateTime(2022, 1, 1, 0, 0, 0),
                HasReminder = true,
                IsDeleted = false,
                DeletedDate = (DateTime?)null,
                IsArchived = false,
                ArchivedDate = (DateTime?)null,
                IsOverdue = true,
                DueDateDisplay = "很久以前到期"
            }
        });
        File.WriteAllText(filePath, document);

        var items = _todoService.LoadTodosFromFile(filePath);

        Assert.Single(items);
        Assert.Equal(new DateTime(2022, 1, 1, 0, 0, 0), items[0].DueDate);
        Assert.True(items[0].HasReminder);
    }

    [Fact]
    public void GetBackupInfos_ReturnsDescendingItemsWithTimeAndSize()
    {
        Directory.CreateDirectory(_backupDirectory);
        var oldestPath = Path.Combine(_backupDirectory, "todos-20260424-080000000.json");
        var latestPath = Path.Combine(_backupDirectory, "todos-20260424-100000000.json");

        File.WriteAllText(oldestPath, "1234");
        File.WriteAllText(latestPath, "123456789");
        File.SetLastWriteTime(oldestPath, new DateTime(2026, 4, 24, 8, 0, 0));
        File.SetLastWriteTime(latestPath, new DateTime(2026, 4, 24, 10, 0, 0));

        var backupInfos = _todoService.GetBackupInfos();

        Assert.Equal(2, backupInfos.Count);
        Assert.Equal(latestPath, backupInfos[0].FilePath);
        Assert.True(backupInfos[0].IsLatest);
        Assert.Equal("9 B", backupInfos[0].FileSizeDisplay);
        Assert.Equal(oldestPath, backupInfos[1].FilePath);
        Assert.False(backupInfos[1].IsLatest);
    }

    [Fact]
    public void RestoreFromBackup_ValidBackup_ReplacesPrimaryFile()
    {
        var original = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "当前数据",
                CreatedDate = new DateTime(2026, 4, 24, 9, 0, 0)
            }
        };

        var backupData = new[]
        {
            new
            {
                Title = "备份数据",
                IsCompleted = false,
                CreatedDate = new DateTime(2026, 4, 20, 8, 0, 0),
                CompletedDate = (DateTime?)null,
                DueDate = (DateTime?)null,
                HasReminder = false,
                IsDeleted = false,
                DeletedDate = (DateTime?)null,
                IsArchived = false,
                ArchivedDate = (DateTime?)null
            }
        };

        Assert.True(_todoService.SaveTodos(original).IsSuccess);
        Directory.CreateDirectory(_backupDirectory);

        var backupPath = Path.Combine(_backupDirectory, "todos-20260424-120000000.json");
        File.WriteAllText(backupPath, JsonSerializer.Serialize(backupData));
        File.SetLastWriteTime(backupPath, new DateTime(2026, 4, 24, 12, 0, 0));

        var restoreResult = _todoService.RestoreFromBackup(backupPath);

        Assert.True(restoreResult.IsSuccess);
        Assert.Single(restoreResult.Todos);
        Assert.Equal("备份数据", restoreResult.Todos[0].Title);

        var loaded = _todoService.LoadTodos();
        Assert.True(loaded.IsSuccess);
        Assert.Single(loaded.Todos);
        Assert.Equal("备份数据", loaded.Todos[0].Title);
    }

    [Fact]
    public void RestoreFromBackup_InvalidBackup_DoesNotReplacePrimaryFile()
    {
        var current = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "当前数据",
                CreatedDate = new DateTime(2026, 4, 24, 9, 0, 0)
            }
        };

        Assert.True(_todoService.SaveTodos(current).IsSuccess);
        Directory.CreateDirectory(_backupDirectory);

        var invalidBackupPath = Path.Combine(_backupDirectory, "todos-20260424-130000000.json");
        File.WriteAllText(invalidBackupPath, "{ invalid json");

        var restoreResult = _todoService.RestoreFromBackup(invalidBackupPath);

        Assert.False(restoreResult.IsSuccess);

        var loaded = _todoService.LoadTodos();
        Assert.True(loaded.IsSuccess);
        Assert.Single(loaded.Todos);
        Assert.Equal("当前数据", loaded.Todos[0].Title);
    }

    [Fact]
    public void SaveTodos_WhenBackupCountExceedsTen_KeepsLatestTenBackups()
    {
        for (var index = 0; index < 12; index++)
        {
            var items = new ObservableCollection<TodoItem>
            {
                new()
                {
                    Title = $"任务 {index}",
                    CreatedDate = new DateTime(2026, 4, 24, 9, 0, 0).AddMinutes(index)
                }
            };

            Assert.True(_todoService.SaveTodos(items).IsSuccess);
            Thread.Sleep(5);
        }

        var backupFiles = Directory.GetFiles(_backupDirectory, "todos-*.json");
        Assert.Equal(10, backupFiles.Length);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
