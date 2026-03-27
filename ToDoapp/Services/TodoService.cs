using System.Collections.ObjectModel;
using System.IO;
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
}