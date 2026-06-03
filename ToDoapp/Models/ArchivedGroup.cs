using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ToDoapp.Models;

public enum ArchivedGroupLevel
{
    Year,
    Month,
    Week,
    Task
}

public class ArchivedGroup : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isExpanded;
    private ObservableCollection<ArchivedGroup> _children = new();
    private ObservableCollection<TodoItem> _tasks = new();
    private ArchivedGroupLevel _level;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private bool _isYearLevel;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    public ArchivedGroupLevel Level
    {
        get => _level;
        set { _level = value; OnPropertyChanged(); }
    }

    public bool IsYearLevel
    {
        get => _isYearLevel;
        set { _isYearLevel = value; OnPropertyChanged(); }
    }

    public DateTime? StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(); }
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set { _endDate = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ArchivedGroup> Children
    {
        get => _children;
        set { _children = value; OnPropertyChanged(); }
    }

    public ObservableCollection<TodoItem> Tasks
    {
        get => _tasks;
        set { _tasks = value; OnPropertyChanged(); }
    }

    public int TaskCount => Tasks.Count + Children.Sum(c => c.TaskCount);

    public void ExpandAll()
    {
        IsExpanded = true;
        foreach (var child in Children)
        {
            child.ExpandAll();
        }
    }

    public void CollapseAll()
    {
        IsExpanded = false;
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }

    public string DisplayName
    {
        get
        {
            return Level switch
            {
                ArchivedGroupLevel.Year => $"{Name}年",
                ArchivedGroupLevel.Month => $"{Name}月",
                ArchivedGroupLevel.Week => $"{Name}（{TaskCount}）",
                _ => Name
            };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static ObservableCollection<ArchivedGroup> BuildGroupTree(ObservableCollection<TodoItem> archivedTasks)
    {
        var groups = new ObservableCollection<ArchivedGroup>();

        var groupedByYear = archivedTasks
            .Where(t => t.CompletedDate.HasValue)
            .GroupBy(t => t.CompletedDate.Value.Year)
            .OrderByDescending(g => g.Key);

        foreach (var yearGroup in groupedByYear)
        {
            var yearNode = new ArchivedGroup
            {
                Name = yearGroup.Key.ToString(),
                Level = ArchivedGroupLevel.Year,
                IsExpanded = false,
                IsYearLevel = true
            };

            var groupedByMonth = yearGroup
                .GroupBy(t => t.CompletedDate.Value.Month)
                .OrderByDescending(g => g.Key);

            foreach (var monthGroup in groupedByMonth)
            {
                var monthNode = new ArchivedGroup
                {
                    Name = monthGroup.Key.ToString(),
                    Level = ArchivedGroupLevel.Month,
                    IsExpanded = false
                };

                var groupedByWeek = monthGroup
                    .GroupBy(t => GetMonthWeekNumber(t.CompletedDate.Value))
                    .OrderByDescending(g => g.Key);

                foreach (var weekGroup in groupedByWeek)
                {
                    var weekInfo = GetWeekDateRange(yearGroup.Key, weekGroup.Key, monthGroup.Key);
                    var weekNode = new ArchivedGroup
                    {
                        Name = $"第{weekGroup.Key}周",
                        Level = ArchivedGroupLevel.Week,
                        StartDate = weekInfo.start,
                        EndDate = weekInfo.end,
                        IsExpanded = false,
                        Tasks = new ObservableCollection<TodoItem>(weekGroup.OrderByDescending(t => t.CompletedDate))
                    };

                    monthNode.Children.Add(weekNode);
                }

                yearNode.Children.Add(monthNode);
            }

            groups.Add(yearNode);
        }

        return groups;
    }

    private static int GetMonthWeekNumber(DateTime date)
    {
        return (date.Day - 1) / 7 + 1;
    }

    private static (DateTime start, DateTime end) GetWeekDateRange(int year, int week, int month)
    {
        var firstDayOfMonth = new DateTime(year, month, 1);
        var weekStart = firstDayOfMonth.AddDays((week - 1) * 7);
        var weekEnd = weekStart.AddDays(6);

        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        if (weekEnd > lastDayOfMonth)
        {
            weekEnd = lastDayOfMonth;
        }

        return (weekStart, weekEnd);
    }
}