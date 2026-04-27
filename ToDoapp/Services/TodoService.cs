using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json;
using ToDoapp.Models;

namespace ToDoapp.Services;

/// <summary>
/// 待办事项服务类，负责待办事项的持久化存储和加载
/// </summary>
public class TodoService
{
    private const int DefaultBackupLimit = 30;
    private const string BackupSearchPattern = "todos-*.json";
    private const string BackupFilePrefix = "todos-";
    private const string BackupFileTimestampFormat = "yyyyMMdd-HHmmssfff";
    private static readonly TimeSpan DefaultBackupInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 数据文件路径
    /// </summary>
    private readonly string _dataFilePath;
    private readonly string _backupDirectoryPath;
    private readonly int _maxBackupFiles;
    private readonly TimeSpan _backupInterval;
    
    /// <summary>
    /// JSON序列化选项
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 初始化TodoService实例
    /// </summary>
    public TodoService(
        string? dataDirectoryOverride = null,
        int maxBackupFiles = DefaultBackupLimit,
        TimeSpan? backupInterval = null)
    {
        if (maxBackupFiles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBackupFiles), "备份文件数量上限必须大于 0。");
        }

        var resolvedBackupInterval = backupInterval ?? DefaultBackupInterval;
        if (resolvedBackupInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(backupInterval), "备份时间间隔不能小于 0。");
        }

        string appFolder;
        if (string.IsNullOrWhiteSpace(dataDirectoryOverride))
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            appFolder = Path.Combine(appDataPath, "ToDoApp");

            var fullPath = Path.GetFullPath(appFolder);
            var fullAppDataPath = Path.GetFullPath(appDataPath);
            if (!fullPath.StartsWith(fullAppDataPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("检测到非法路径访问");
            }
        }
        else
        {
            appFolder = Path.GetFullPath(dataDirectoryOverride);
        }

        Directory.CreateDirectory(appFolder);
        _dataFilePath = Path.Combine(appFolder, "todos.json");
        _backupDirectoryPath = Path.Combine(appFolder, "backups");
        Directory.CreateDirectory(_backupDirectoryPath);
        _maxBackupFiles = maxBackupFiles;
        _backupInterval = resolvedBackupInterval;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// 加载待办事项列表
    /// </summary>
    /// <returns>待办事项集合</returns>
    public TodoLoadResult LoadTodos()
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                return TodoLoadResult.Success(new ObservableCollection<TodoItem>());
            }

            var readResult = TryReadTodosFromFile(_dataFilePath);
            if (readResult.IsSuccess)
            {
                return TodoLoadResult.Success(readResult.Todos);
            }

            var recoveryResult = TryRecoverFromBackups(readResult.ErrorMessage);
            if (recoveryResult != null)
            {
                return recoveryResult;
            }

            return TodoLoadResult.Failure(readResult.ErrorMessage ?? "主待办数据文件损坏，且没有可用备份。");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载待办事项失败: {ex.Message}");
            return TodoLoadResult.Failure($"加载待办事项失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 保存待办事项列表
    /// </summary>
    /// <param name="todos">待办事项集合</param>
    public TodoSaveResult SaveTodos(IEnumerable<TodoItem> todos)
    {
        var tempFilePath = $"{_dataFilePath}.tmp";

        try
        {
            var storageItems = (todos ?? Enumerable.Empty<TodoItem>())
                .Select(TodoStorageItem.FromTodoItem)
                .ToList();
            var json = JsonSerializer.Serialize(storageItems, _jsonOptions);

            if (File.Exists(_dataFilePath))
            {
                var currentJson = File.ReadAllText(_dataFilePath);
                if (string.Equals(currentJson, json, StringComparison.Ordinal))
                {
                    return TodoSaveResult.Success(null);
                }
            }

            File.WriteAllText(tempFilePath, json);

            var validationResult = TryReadTodosFromFile(tempFilePath);
            if (!validationResult.IsSuccess)
            {
                throw new InvalidDataException(validationResult.ErrorMessage ?? "保存校验失败。");
            }

            string? backupPath = null;
            if (File.Exists(_dataFilePath))
            {
                backupPath = CreateBackupIfNeeded();
                File.Replace(tempFilePath, _dataFilePath, null, true);
            }
            else
            {
                File.Move(tempFilePath, _dataFilePath);
            }

            CleanupOldBackups();
            return TodoSaveResult.Success(backupPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存待办事项失败: {ex.Message}");
            return TodoSaveResult.Failure($"保存待办事项失败：{ex.Message}");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                }
            }
        }
    }

    /// <summary>
    /// 从指定文件加载待办事项列表
    /// </summary>
    public ObservableCollection<TodoItem> LoadTodosFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("导入文件路径不能为空。", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("找不到要导入的文件。", filePath);
        }

        var result = TryReadTodosFromFile(filePath);
        if (!result.IsSuccess)
        {
            throw new InvalidDataException(result.ErrorMessage ?? "导入文件中没有可用的待办数据。");
        }

        return result.Todos;
    }

    public IReadOnlyList<TodoBackupInfo> GetBackupInfos()
    {
        var backupFiles = GetBackupFilesDescending();
        var backupInfos = new List<TodoBackupInfo>();
        var isLatest = true;

        foreach (var backupPath in backupFiles)
        {
            var fileInfo = new FileInfo(backupPath);
            if (!fileInfo.Exists)
            {
                continue;
            }

            backupInfos.Add(new TodoBackupInfo(
                fileInfo.FullName,
                GetBackupTimestamp(backupPath) ?? fileInfo.LastWriteTime,
                fileInfo.Length,
                isLatest));
            isLatest = false;
        }

        return backupInfos;
    }

    public TodoRestoreResult RestoreFromBackup(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            return TodoRestoreResult.Failure("请选择要恢复的备份。");
        }

        try
        {
            var fullBackupPath = Path.GetFullPath(backupPath);
            var fullBackupDirectoryPath = Path.GetFullPath(_backupDirectoryPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!fullBackupPath.StartsWith(fullBackupDirectoryPath, StringComparison.OrdinalIgnoreCase))
            {
                return TodoRestoreResult.Failure("只能恢复应用备份目录中的文件。");
            }

            if (!File.Exists(fullBackupPath))
            {
                return TodoRestoreResult.Failure("选中的备份文件不存在。");
            }

            var readResult = TryReadTodosFromFile(fullBackupPath);
            if (!readResult.IsSuccess)
            {
                return TodoRestoreResult.Failure(readResult.ErrorMessage ?? "备份文件无法读取。");
            }

            RestoreBackupToPrimary(fullBackupPath);

            var backupInfo = new FileInfo(fullBackupPath);
            return TodoRestoreResult.Success(
                readResult.Todos,
                new TodoBackupInfo(fullBackupPath, GetBackupTimestamp(fullBackupPath) ?? backupInfo.LastWriteTime, backupInfo.Length, false));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"手动恢复备份失败: {ex.Message}");
            return TodoRestoreResult.Failure($"恢复备份失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 导出待办事项列表到指定文件
    /// </summary>
    public void ExportTodosToFile(IEnumerable<TodoItem> todos, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("导出文件路径不能为空。", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var exportItems = (todos ?? Enumerable.Empty<TodoItem>())
            .Select(TodoStorageItem.FromTodoItem)
            .ToList();
        var json = JsonSerializer.Serialize(exportItems, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    private TodoFileReadResult TryReadTodosFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return TodoFileReadResult.Failure("待办数据文件为空。");
            }

            var storageItems = JsonSerializer.Deserialize<List<TodoStorageItem>>(json, _jsonOptions);
            if (storageItems == null)
            {
                return TodoFileReadResult.Failure("待办数据文件中没有可用的待办数据。");
            }

            var todos = new ObservableCollection<TodoItem>(storageItems.Select(TodoItem.FromStorage));
            return TodoFileReadResult.Success(todos);
        }
        catch (Exception ex)
        {
            return TodoFileReadResult.Failure($"待办数据解析失败：{ex.Message}");
        }
    }

    private TodoLoadResult? TryRecoverFromBackups(string? primaryReadError)
    {
        foreach (var backupPath in GetBackupFilesDescending())
        {
            var backupReadResult = TryReadTodosFromFile(backupPath);
            if (!backupReadResult.IsSuccess)
            {
                continue;
            }

            try
            {
                RestoreBackupToPrimary(backupPath);
                return TodoLoadResult.Recovered(
                    backupReadResult.Todos,
                    $"主待办数据文件损坏，已从最近备份恢复。原始错误：{primaryReadError}",
                    backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复备份失败: {ex.Message}");
            }
        }

        return null;
    }

    private void RestoreBackupToPrimary(string backupPath)
    {
        var tempFilePath = $"{_dataFilePath}.restore";
        File.Copy(backupPath, tempFilePath, true);

        try
        {
            var validationResult = TryReadTodosFromFile(tempFilePath);
            if (!validationResult.IsSuccess)
            {
                throw new InvalidDataException(validationResult.ErrorMessage ?? "恢复备份校验失败。");
            }

            if (File.Exists(_dataFilePath))
            {
                File.Replace(tempFilePath, _dataFilePath, null, true);
            }
            else
            {
                File.Move(tempFilePath, _dataFilePath);
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                }
            }
        }
    }

    private string? CreateBackupIfNeeded()
    {
        var latestBackupPath = GetBackupFilesDescending().FirstOrDefault();
        if (latestBackupPath != null)
        {
            var latestBackupTime = GetBackupTimestamp(latestBackupPath);
            if (latestBackupTime.HasValue)
            {
                var elapsed = DateTime.Now - latestBackupTime.Value;
                if (elapsed < _backupInterval)
                {
                    return null;
                }
            }
        }

        var backupPath = Path.Combine(_backupDirectoryPath, $"{BackupFilePrefix}{DateTime.Now.ToString(BackupFileTimestampFormat, CultureInfo.InvariantCulture)}.json");
        File.Copy(_dataFilePath, backupPath, true);
        return backupPath;
    }

    private IEnumerable<string> GetBackupFilesDescending()
    {
        if (!Directory.Exists(_backupDirectoryPath))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(_backupDirectoryPath, BackupSearchPattern)
            .Select(path => new BackupFileEntry(path, GetBackupTimestamp(path), File.GetLastWriteTime(path)))
            .OrderByDescending(entry => entry.TimestampFromFileName ?? entry.LastWriteTime)
            .ThenByDescending(entry => Path.GetFileName(entry.Path))
            .Select(entry => entry.Path);
    }

    private static DateTime? GetBackupTimestamp(string backupPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(backupPath);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.StartsWith(BackupFilePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var timestampText = fileName[BackupFilePrefix.Length..];
        return DateTime.TryParseExact(
            timestampText,
            BackupFileTimestampFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var backupTime)
            ? backupTime
            : null;
    }

    private void CleanupOldBackups()
    {
        var backupFiles = GetBackupFilesDescending().ToList();
        if (backupFiles.Count <= _maxBackupFiles)
        {
            return;
        }

        foreach (var backupPath in backupFiles.Skip(_maxBackupFiles))
        {
            try
            {
                File.Delete(backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除旧备份失败: {ex.Message}");
            }
        }
    }

    private sealed record BackupFileEntry(string Path, DateTime? TimestampFromFileName, DateTime LastWriteTime);
}

public sealed class TodoLoadResult
{
    public bool IsSuccess { get; }
    public bool IsRecoveredFromBackup { get; }
    public ObservableCollection<TodoItem> Todos { get; }
    public string? ErrorMessage { get; }
    public string? RecoverySourcePath { get; }

    private TodoLoadResult(bool isSuccess, bool isRecoveredFromBackup, ObservableCollection<TodoItem> todos, string? errorMessage, string? recoverySourcePath)
    {
        IsSuccess = isSuccess;
        IsRecoveredFromBackup = isRecoveredFromBackup;
        Todos = todos;
        ErrorMessage = errorMessage;
        RecoverySourcePath = recoverySourcePath;
    }

    public static TodoLoadResult Success(ObservableCollection<TodoItem> todos)
    {
        return new TodoLoadResult(true, false, todos, null, null);
    }

    public static TodoLoadResult Recovered(ObservableCollection<TodoItem> todos, string? message, string? recoverySourcePath)
    {
        return new TodoLoadResult(true, true, todos, message, recoverySourcePath);
    }

    public static TodoLoadResult Failure(string? errorMessage)
    {
        return new TodoLoadResult(false, false, new ObservableCollection<TodoItem>(), errorMessage, null);
    }
}

public sealed class TodoSaveResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? BackupPath { get; }

    private TodoSaveResult(bool isSuccess, string? errorMessage, string? backupPath)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        BackupPath = backupPath;
    }

    public static TodoSaveResult Success(string? backupPath)
    {
        return new TodoSaveResult(true, null, backupPath);
    }

    public static TodoSaveResult Failure(string? errorMessage)
    {
        return new TodoSaveResult(false, errorMessage, null);
    }
}

public sealed class TodoRestoreResult
{
    public bool IsSuccess { get; }
    public ObservableCollection<TodoItem> Todos { get; }
    public string? ErrorMessage { get; }
    public TodoBackupInfo? BackupInfo { get; }

    private TodoRestoreResult(bool isSuccess, ObservableCollection<TodoItem> todos, string? errorMessage, TodoBackupInfo? backupInfo)
    {
        IsSuccess = isSuccess;
        Todos = todos;
        ErrorMessage = errorMessage;
        BackupInfo = backupInfo;
    }

    public static TodoRestoreResult Success(ObservableCollection<TodoItem> todos, TodoBackupInfo backupInfo)
    {
        return new TodoRestoreResult(true, todos, null, backupInfo);
    }

    public static TodoRestoreResult Failure(string? errorMessage)
    {
        return new TodoRestoreResult(false, new ObservableCollection<TodoItem>(), errorMessage, null);
    }
}

public sealed class TodoBackupInfo
{
    public string FilePath { get; }
    public DateTime BackupTime { get; }
    public long FileSizeBytes { get; }
    public bool IsLatest { get; }
    public string FileName => Path.GetFileName(FilePath);
    public string BackupTimeDisplay => BackupTime.ToString("yyyy-MM-dd HH:mm:ss");
    public string FileSizeDisplay => FormatFileSize(FileSizeBytes);

    public TodoBackupInfo(string filePath, DateTime backupTime, long fileSizeBytes, bool isLatest)
    {
        FilePath = filePath;
        BackupTime = backupTime;
        FileSizeBytes = fileSizeBytes;
        IsLatest = isLatest;
    }

    private static string FormatFileSize(long fileSizeBytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;

        if (fileSizeBytes >= mb)
        {
            return $"{fileSizeBytes / (double)mb:0.##} MB";
        }

        if (fileSizeBytes >= kb)
        {
            return $"{fileSizeBytes / (double)kb:0.##} KB";
        }

        return $"{fileSizeBytes} B";
    }
}

internal sealed class TodoFileReadResult
{
    public bool IsSuccess { get; }
    public ObservableCollection<TodoItem> Todos { get; }
    public string? ErrorMessage { get; }

    private TodoFileReadResult(bool isSuccess, ObservableCollection<TodoItem> todos, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Todos = todos;
        ErrorMessage = errorMessage;
    }

    public static TodoFileReadResult Success(ObservableCollection<TodoItem> todos)
    {
        return new TodoFileReadResult(true, todos, null);
    }

    public static TodoFileReadResult Failure(string? errorMessage)
    {
        return new TodoFileReadResult(false, new ObservableCollection<TodoItem>(), errorMessage);
    }
}
