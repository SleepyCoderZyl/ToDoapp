using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using ToDoapp.Constants;

namespace ToDoapp.Models;

public class TodoItem : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private bool _isCompleted;
    private DateTime _createdDate;
    private DateTime? _completedDate;
    private DateTime? _dueDate;
    private bool _hasReminder;
    private bool _isDeleted;
    private DateTime? _deletedDate;
    private bool _isArchived;
    private DateTime? _archivedDate;

    public string Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("标题不能为空", nameof(value));
            if (value.Length > AppConstants.MaxTitleLength)
                throw new ArgumentException($"标题长度不能超过{AppConstants.MaxTitleLength}个字符", nameof(value));

            _title = value.Trim();
            OnPropertyChanged();
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            _isCompleted = value;
            if (_isCompleted)
            {
                CompletedDate = DateTime.Now;
            }
            else
            {
                CompletedDate = null;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    public DateTime CreatedDate
    {
        get => _createdDate;
        set
        {
            _createdDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime? CompletedDate
    {
        get => _completedDate;
        set
        {
            _completedDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set
        {
            if (value.HasValue)
            {
                if (value.Value < DateTime.Now.AddYears(-1))
                    throw new ArgumentException("截止日期不能早于一年前", nameof(value));
                if (value.Value > DateTime.Now.AddYears(10))
                    throw new ArgumentException("截止日期不能晚于十年后", nameof(value));
            }

            _dueDate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DueDateDisplay));
            OnPropertyChanged(nameof(IsOverdue));
            OnPropertyChanged(nameof(DaysUntilDue));
        }
    }

    public bool HasReminder
    {
        get => _hasReminder;
        set
        {
            _hasReminder = value;
            OnPropertyChanged();
        }
    }

    // 只读属性用于UI显示
    public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.Now && !IsCompleted;
    
    public string DueDateDisplay
    {
        get
        {
            if (!DueDate.HasValue) return "";
            
            var dueDate = DueDate.Value.Date;
            var today = DateTime.Now.Date;
            var daysUntil = (dueDate - today).Days;
            
            if (daysUntil < 0) return $"已过期 {Math.Abs(daysUntil)} 天";
            if (daysUntil == 0) return "今天到期";
            if (daysUntil == 1) return "明天到期";
            if (daysUntil <= 7) return $"{daysUntil} 天后到期";
            
            return DueDate.Value.ToString("MM-dd");
        }
    }
    
    public int? DaysUntilDue
    {
        get
        {
            if (!DueDate.HasValue) return null;
            return (DueDate.Value.Date - DateTime.Now.Date).Days;
        }
    }

    public void RefreshTimeSensitiveProperties()
    {
        OnPropertyChanged(nameof(IsOverdue));
        OnPropertyChanged(nameof(DueDateDisplay));
        OnPropertyChanged(nameof(DaysUntilDue));
        OnPropertyChanged(nameof(DaysUntilPermanentDelete));
        OnPropertyChanged(nameof(DeleteTimeDisplay));
    }
    
    public string CompletedDateDisplay
    {
        get
        {
            if (!CompletedDate.HasValue) return "";
            return CompletedDate.Value.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public bool IsDeleted
    {
        get => _isDeleted;
        set
        {
            _isDeleted = value;
            if (_isDeleted)
            {
                DeletedDate = DateTime.Now;
            }
            else
            {
                DeletedDate = null;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(DaysUntilPermanentDelete));
            OnPropertyChanged(nameof(DeleteTimeDisplay));
        }
    }

    public DateTime? DeletedDate
    {
        get => _deletedDate;
        set
        {
            _deletedDate = value;
            OnPropertyChanged();
        }
    }

    public int? DaysUntilPermanentDelete
    {
        get
        {
            if (!DeletedDate.HasValue) return null;
            var daysSinceDeleted = (DateTime.Now - DeletedDate.Value).Days;
            return Math.Max(0, 7 - daysSinceDeleted);
        }
    }

    public string DeleteTimeDisplay
    {
        get
        {
            if (!DeletedDate.HasValue) return "";
            var daysLeft = DaysUntilPermanentDelete;
            if (daysLeft <= 0) return "即将永久删除";
            if (daysLeft == 1) return "1天后永久删除";
            return $"{daysLeft}天后永久删除";
        }
    }

    public bool IsArchived
    {
        get => _isArchived;
        set
        {
            _isArchived = value;
            if (_isArchived)
            {
                ArchivedDate = DateTime.Now;
            }
            else
            {
                ArchivedDate = null;
            }
            OnPropertyChanged();
        }
    }

    public DateTime? ArchivedDate
    {
        get => _archivedDate;
        set
        {
            _archivedDate = value;
            OnPropertyChanged();
        }
    }

    public string ArchivedDateDisplay
    {
        get
        {
            if (!ArchivedDate.HasValue) return "";
            return ArchivedDate.Value.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal static TodoItem FromStorage(TodoStorageItem storageItem)
    {
        ArgumentNullException.ThrowIfNull(storageItem);

        var title = storageItem.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidDataException("待办事项标题不能为空。");
        }

        if (title.Length > AppConstants.MaxTitleLength)
        {
            throw new InvalidDataException($"待办事项标题长度不能超过{AppConstants.MaxTitleLength}个字符。");
        }

        return new TodoItem
        {
            _title = title,
            _isCompleted = storageItem.IsCompleted,
            _createdDate = storageItem.CreatedDate,
            _completedDate = storageItem.CompletedDate,
            _dueDate = storageItem.DueDate,
            _hasReminder = storageItem.HasReminder,
            _isDeleted = storageItem.IsDeleted,
            _deletedDate = storageItem.DeletedDate,
            _isArchived = storageItem.IsArchived,
            _archivedDate = storageItem.ArchivedDate
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
