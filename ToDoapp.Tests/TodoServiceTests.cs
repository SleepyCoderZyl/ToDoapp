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
    public async Task LoadTodosAsync_MissingFile_ReturnsSuccessWithEmptyCollection()
    {
        var result = await _todoService.LoadTodosAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsRecoveredFromBackup);
        Assert.Empty(result.Todos);
    }

    [Fact]
    public async Task LoadTodosAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _todoService.LoadTodosAsync(cancellation.Token));
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
    public async Task LoadTodosAsync_CorruptedPrimaryWithBackup_RestoresLatestBackup()
    {
        var todo = new TodoItem
        {
            Title = "异步恢复测试",
            CreatedDate = new DateTime(2026, 7, 12, 9, 0, 0)
        };
        Assert.True(_todoService.SaveTodos([todo]).IsSuccess);

        todo.IsCompleted = true;
        todo.CompletedDate = new DateTime(2026, 7, 12, 10, 0, 0);
        Assert.True(_todoService.SaveTodos([todo]).IsSuccess);
        File.WriteAllText(Path.Combine(_testDirectory, "todos.json"), "{ invalid json");

        var result = await _todoService.LoadTodosAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsRecoveredFromBackup);
        Assert.Single(result.Todos);
        Assert.False(result.Todos[0].IsCompleted);
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
    public async Task LoadTodosAsync_CorruptedPrimaryWithoutBackup_ReturnsFailure()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "todos.json"), "{ invalid json");

        var result = await _todoService.LoadTodosAsync(CancellationToken.None);

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
    public void MergeImportedTodos_SkipsDuplicateItemsByTitleAndCreatedDate()
    {
        var createdAt = new DateTime(2026, 4, 24, 9, 0, 0);
        var existing = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "重复任务",
                CreatedDate = createdAt
            }
        };
        var imported = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "重复任务",
                CreatedDate = createdAt
            },
            new()
            {
                Title = "新增任务",
                CreatedDate = new DateTime(2026, 4, 24, 10, 0, 0)
            }
        };

        var result = _todoService.MergeImportedTodos(existing, imported);

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(2, result.MergedTodos.Count);
        Assert.Contains(result.MergedTodos, item => item.Title == "新增任务");
    }

    [Fact]
    public void MergeImportedTodos_AllowsSameTitleWithDifferentCreatedDate()
    {
        var existing = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "同名任务",
                CreatedDate = new DateTime(2026, 4, 24, 9, 0, 0)
            }
        };
        var imported = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "同名任务",
                CreatedDate = new DateTime(2026, 4, 24, 9, 0, 1)
            }
        };

        var result = _todoService.MergeImportedTodos(existing, imported);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.AddedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.MergedTodos.Count);
    }

    [Fact]
    public void MergeImportedTodos_DeduplicatesDuplicateItemsWithinImportedList()
    {
        var createdAt = new DateTime(2026, 4, 24, 11, 0, 0);
        var imported = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "文件内重复任务",
                CreatedDate = createdAt
            },
            new()
            {
                Title = "文件内重复任务",
                CreatedDate = createdAt
            }
        };

        var result = _todoService.MergeImportedTodos(Array.Empty<TodoItem>(), imported);

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.MergedTodos);
    }

    [Fact]
    public void MergeImportedTodos_WhenAllItemsDuplicate_ReturnsZeroAdded()
    {
        var createdAt = new DateTime(2026, 4, 24, 9, 0, 0);
        var existing = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "已有任务",
                CreatedDate = createdAt
            }
        };
        var imported = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "已有任务",
                CreatedDate = createdAt
            }
        };

        var result = _todoService.MergeImportedTodos(existing, imported);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.MergedTodos);
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
    public void GetBackupInfos_PrefersFileNameTimestampOverLastWriteTime()
    {
        Directory.CreateDirectory(_backupDirectory);
        var olderByName = Path.Combine(_backupDirectory, "todos-20260424-080000000.json");
        var newerByName = Path.Combine(_backupDirectory, "todos-20260424-100000000.json");

        File.WriteAllText(olderByName, "older");
        File.WriteAllText(newerByName, "newer");
        File.SetLastWriteTime(olderByName, new DateTime(2026, 4, 24, 12, 0, 0));
        File.SetLastWriteTime(newerByName, new DateTime(2026, 4, 24, 7, 0, 0));

        var backupInfos = _todoService.GetBackupInfos();

        Assert.Equal(2, backupInfos.Count);
        Assert.Equal(newerByName, backupInfos[0].FilePath);
        Assert.Equal(new DateTime(2026, 4, 24, 10, 0, 0), backupInfos[0].BackupTime);
        Assert.Equal(olderByName, backupInfos[1].FilePath);
        Assert.Equal(new DateTime(2026, 4, 24, 8, 0, 0), backupInfos[1].BackupTime);
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
    public void SaveTodos_WhenBackupCountExceedsThirty_KeepsLatestThirtyBackups()
    {
        var todoService = new TodoService(_testDirectory, backupInterval: TimeSpan.Zero);

        for (var index = 0; index < 32; index++)
        {
            var items = new ObservableCollection<TodoItem>
            {
                new()
                {
                    Title = $"任务 {index}",
                    CreatedDate = new DateTime(2026, 4, 24, 9, 0, 0).AddMinutes(index)
                }
            };

            Assert.True(todoService.SaveTodos(items).IsSuccess);
            Thread.Sleep(5);
        }

        var backupFiles = Directory.GetFiles(_backupDirectory, "todos-*.json");
        Assert.Equal(30, backupFiles.Length);
    }

    [Fact]
    public void SaveTodos_WhenBackupIntervalHasNotElapsed_DoesNotCreateAdditionalBackup()
    {
        var todoService = new TodoService(_testDirectory, backupInterval: TimeSpan.FromMinutes(10));
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
                CreatedDate = new DateTime(2026, 4, 24, 9, 1, 0)
            }
        };
        var thirdItems = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "第三版",
                CreatedDate = new DateTime(2026, 4, 24, 9, 2, 0)
            }
        };

        Assert.True(todoService.SaveTodos(firstItems).IsSuccess);

        var secondSave = todoService.SaveTodos(secondItems);
        Assert.True(secondSave.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(secondSave.BackupPath));

        var thirdSave = todoService.SaveTodos(thirdItems);
        Assert.True(thirdSave.IsSuccess);
        Assert.Null(thirdSave.BackupPath);
        Assert.Single(Directory.GetFiles(_backupDirectory, "todos-*.json"));
    }

    [Fact]
    public void SaveTodos_WhenLatestBackupFileNameTimestampIsRecent_SkipsBackupEvenIfLastWriteTimeIsOld()
    {
        var todoService = new TodoService(_testDirectory, backupInterval: TimeSpan.FromHours(1));
        var firstItems = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "第一版",
                CreatedDate = DateTime.Now.AddMinutes(-5)
            }
        };
        var secondItems = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "第二版",
                CreatedDate = DateTime.Now
            }
        };

        Assert.True(todoService.SaveTodos(firstItems).IsSuccess);

        var recentTimestamp = DateTime.Now.AddMinutes(-10).ToString("yyyyMMdd-HHmmssfff");
        var backupPath = Path.Combine(_backupDirectory, $"todos-{recentTimestamp}.json");
        File.WriteAllText(backupPath, "seed");
        File.SetLastWriteTime(backupPath, DateTime.Now.AddDays(-2));

        var saveResult = todoService.SaveTodos(secondItems);

        Assert.True(saveResult.IsSuccess);
        Assert.Null(saveResult.BackupPath);
        Assert.Single(Directory.GetFiles(_backupDirectory, "todos-*.json"));
    }

    [Fact]
    public void SaveTodos_ThenLoad_RoundTripsReminderFields()
    {
        var items = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "带提醒的待办",
                CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
                DueDate = new DateTime(2026, 5, 13),
                DueTime = new TimeOnly(15, 0),
                ReminderOffsetMinutes = 15,
                HasReminder = true
            }
        };

        Assert.True(_todoService.SaveTodos(items).IsSuccess);

        var loaded = _todoService.LoadTodos();
        Assert.True(loaded.IsSuccess);
        var todo = Assert.Single(loaded.Todos);
        Assert.Equal(new TimeOnly(15, 0), todo.DueTime);
        Assert.Equal(15, todo.ReminderOffsetMinutes);
        Assert.True(todo.HasReminder);
    }

    [Fact]
    public void LoadTodosFromFile_OldSchemaWithoutReminderFields_LoadsWithNulls()
    {
        var filePath = Path.Combine(_testDirectory, "legacy.json");
        var document = JsonSerializer.Serialize(new[]
        {
            new
            {
                Title = "旧版待办",
                IsCompleted = false,
                CreatedDate = new DateTime(2026, 4, 1, 9, 0, 0),
                CompletedDate = (DateTime?)null,
                DueDate = (DateTime?)new DateTime(2026, 4, 2, 0, 0, 0),
                HasReminder = true,
                IsDeleted = false,
                DeletedDate = (DateTime?)null,
                IsArchived = false,
                ArchivedDate = (DateTime?)null
            }
        });
        File.WriteAllText(filePath, document);

        var items = _todoService.LoadTodosFromFile(filePath);

        var todo = Assert.Single(items);
        Assert.Equal("旧版待办", todo.Title);
        Assert.Null(todo.DueTime);
        Assert.Null(todo.ReminderOffsetMinutes);
        Assert.Null(todo.LastReminderShownAt);
        Assert.True(todo.HasReminder);
    }

    [Fact]
    public void SaveTodos_ExportsReminderFieldsInJson()
    {
        var items = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "导出测试",
                CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
                DueDate = new DateTime(2026, 5, 13),
                DueTime = new TimeOnly(18, 0),
                ReminderOffsetMinutes = 15,
                HasReminder = true
            }
        };
        var exportPath = Path.Combine(_testDirectory, "export.json");
        _todoService.ExportTodosToFile(items, exportPath);

        var json = File.ReadAllText(exportPath);
        Assert.Contains("\"dueTime\":", json);
        Assert.Contains("\"reminderOffsetMinutes\":", json);
        Assert.Contains("18:00", json);
        Assert.Contains("15", json);
    }

    [Fact]
    public void RestoreFromBackup_ContainsReminderFields_RestoresData()
    {
        var current = new ObservableCollection<TodoItem>
        {
            new()
            {
                Title = "当前",
                CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0)
            }
        };

        Assert.True(_todoService.SaveTodos(current).IsSuccess);
        Directory.CreateDirectory(_backupDirectory);

        var backupPayload = new[]
        {
            new
            {
                Title = "带提醒的备份",
                IsCompleted = false,
                CreatedDate = new DateTime(2026, 5, 12, 8, 0, 0),
                CompletedDate = (DateTime?)null,
                DueDate = (DateTime?)new DateTime(2026, 5, 15, 0, 0, 0),
                DueTime = (TimeOnly?)new TimeOnly(10, 30),
                ReminderOffsetMinutes = (int?)30,
                LastReminderShownAt = (DateTime?)null,
                HasReminder = true,
                IsDeleted = false,
                DeletedDate = (DateTime?)null,
                IsArchived = false,
                ArchivedDate = (DateTime?)null
            }
        };
        var backupPath = Path.Combine(_backupDirectory, "todos-20260512-080000000.json");
        File.WriteAllText(backupPath, JsonSerializer.Serialize(backupPayload));
        File.SetLastWriteTime(backupPath, new DateTime(2026, 5, 12, 8, 0, 0));

        var restoreResult = _todoService.RestoreFromBackup(backupPath);

        Assert.True(restoreResult.IsSuccess);
        var todo = Assert.Single(restoreResult.Todos);
        Assert.Equal("带提醒的备份", todo.Title);
        Assert.Equal(new TimeOnly(10, 30), todo.DueTime);
        Assert.Equal(30, todo.ReminderOffsetMinutes);
        Assert.True(todo.HasReminder);

        var reloaded = _todoService.LoadTodos();
        Assert.True(reloaded.IsSuccess);
        Assert.Equal(new TimeOnly(10, 30), reloaded.Todos[0].DueTime);
        Assert.Equal(30, reloaded.Todos[0].ReminderOffsetMinutes);
    }

    [Fact]
    public void LoadTodosFromFile_MissingFieldsLoadAsNullAndDoNotThrow()
    {
        var filePath = Path.Combine(_testDirectory, "legacy.json");
        var document = JsonSerializer.Serialize(new[]
        {
            new
            {
                Title = "旧版字段",
                IsCompleted = false,
                CreatedDate = new DateTime(2026, 4, 1, 9, 0, 0),
                CompletedDate = (DateTime?)null,
                DueDate = (DateTime?)new DateTime(2026, 4, 2, 0, 0, 0),
                HasReminder = true,
                IsDeleted = false,
                DeletedDate = (DateTime?)null,
                IsArchived = false,
                ArchivedDate = (DateTime?)null
            }
        });
        File.WriteAllText(filePath, document);

        var items = _todoService.LoadTodosFromFile(filePath);

        var todo = Assert.Single(items);
        Assert.Null(todo.DueTime);
        Assert.Null(todo.ReminderOffsetMinutes);
        Assert.Null(todo.LastReminderShownAt);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
