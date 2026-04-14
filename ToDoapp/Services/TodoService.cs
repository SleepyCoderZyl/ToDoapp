using System.Collections.ObjectModel;
using System.Collections.Generic;
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
    /// <summary>
    /// 数据文件路径
    /// </summary>
    private readonly string _dataFilePath;
    
    /// <summary>
    /// JSON序列化选项
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 初始化TodoService实例
    /// </summary>
    public TodoService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "ToDoApp");

        // 验证路径安全性，防止路径遍历攻击
        var fullPath = Path.GetFullPath(appFolder);
        var fullAppDataPath = Path.GetFullPath(appDataPath);
        if (!fullPath.StartsWith(fullAppDataPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException("检测到非法路径访问");
        }

        Directory.CreateDirectory(appFolder);
        _dataFilePath = Path.Combine(appFolder, "todos.json");

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
    public ObservableCollection<TodoItem> LoadTodos()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                var todos = JsonSerializer.Deserialize<ObservableCollection<TodoItem>>(json, _jsonOptions);
                return todos ?? new ObservableCollection<TodoItem>();
            }
        }
        catch (Exception ex)
        {
            // 记录错误日志
            System.Diagnostics.Debug.WriteLine($"加载待办事项失败: {ex.Message}");
        }
        
        return new ObservableCollection<TodoItem>();
    }

    /// <summary>
    /// 保存待办事项列表
    /// </summary>
    /// <param name="todos">待办事项集合</param>
    public void SaveTodos(ObservableCollection<TodoItem> todos)
    {
        try
        {
            var json = JsonSerializer.Serialize(todos, _jsonOptions);
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            // 记录错误日志
            System.Diagnostics.Debug.WriteLine($"保存待办事项失败: {ex.Message}");
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

        var json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("导入文件为空。");
        }

        try
        {
            var todos = JsonSerializer.Deserialize<ObservableCollection<TodoItem>>(json, _jsonOptions);
            if (todos == null)
            {
                throw new InvalidDataException("导入文件中没有可用的待办数据。");
            }

            return todos;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("导入文件不是有效的待办 JSON 格式。", ex);
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

        var exportItems = todos?.ToList() ?? new List<TodoItem>();
        var json = JsonSerializer.Serialize(exportItems, _jsonOptions);
        File.WriteAllText(filePath, json);
    }
}
