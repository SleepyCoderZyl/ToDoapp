using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ToDoapp.Models;
using ToDoapp.Services;
using ToDoapp.ViewModels;

namespace ToDoapp.Views;

public partial class MainWindow
{
    private void SmartReadButton_Click(object sender, RoutedEventArgs e)
    {
        SmartAddTask();
    }

    private void SmartAddTask()
    {
        var input = NewTaskTextBox?.Text?.Trim() ?? string.Empty;
        var todoItem = _viewModel.AddSmartTask(input, DueDatePicker?.SelectedDate);
        if (todoItem == null)
        {
            UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
            return;
        }

        RefreshTaskCollections();

        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (TasksListBox.ItemContainerGenerator.ContainerFromItem(todoItem) is ListBoxItem listBoxItem)
            {
                listBoxItem.RenderTransform = new ScaleTransform(0.8, 0.8);
                listBoxItem.RenderTransformOrigin = new Point(0.5, 0.5);
                listBoxItem.Opacity = 0;

                PlayAnimationOnItemContainer(listBoxItem, "TaskAddAnimation", null);
            }
        }), DispatcherPriority.Render);

        ResetNewTaskInputs();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void ResetNewTaskInputs()
    {
        if (NewTaskTextBox != null)
        {
            NewTaskTextBox.Clear();
        }

        if (DueDatePicker != null)
        {
            DueDatePicker.SelectedDate = null;
        }

        UpdatePlaceholderVisibility();
    }

    private void NewTaskTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SmartAddTask();
        }
    }

    private void TasksListBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            CopySelectedTask();
            e.Handled = true;
        }
    }

    private void TasksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        CopySelectedTask();
    }

    private void CopyTask_Click(object sender, RoutedEventArgs e)
    {
        CopySelectedTask();
    }

    private void CopySelectedTask()
    {
        var todoItem = GetSelectedPendingOrCompletedTask();
        if (todoItem == null)
        {
            return;
        }

        try
        {
            Clipboard.SetText(MainWindowViewModel.BuildTaskClipboardText(todoItem));
            UpdateStatus("已复制到剪贴板");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"复制失败: {ex.Message}");
        }
    }

    private TodoItem? GetSelectedPendingOrCompletedTask()
    {
        if (TasksListBox?.SelectedItem is TodoItem pendingItem)
        {
            return pendingItem;
        }

        if (CompletedTasksListBox?.SelectedItem is TodoItem completedItem)
        {
            return completedItem;
        }

        return null;
    }

    private void EditTask_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = GetSelectedPendingOrCompletedTask();
        if (selectedItem == null)
        {
            UpdateStatus("请先选择一个待办事项");
            return;
        }

        var editPanel = new StackPanel
        {
            Margin = new Thickness(0, 4, 0, 4)
        };

        var titleLabel = new TextBlock
        {
            Text = "标题",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 0, 0, 6)
        };

        var titleTextBox = new TextBox
        {
            Text = selectedItem.Title,
            FontSize = 14,
            MinHeight = 40,
            Margin = new Thickness(0, 0, 0, 14),
            MaxLength = AppConstants.MaxTitleLength,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (Application.Current.Resources["ModernTextBoxStyle"] is Style titleStyle)
        {
            titleTextBox.Style = titleStyle;
        }

        var dateLabel = new TextBlock
        {
            Text = "截止日期（可选）",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 0, 0, 6)
        };

        var datePicker = new Controls.CalendarPopup
        {
            SelectedDate = selectedItem.DueDate,
            Margin = new Thickness(0, 0, 0, 8),
            MinWidth = 160,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var timeLabel = new TextBlock
        {
            Text = "提醒时间（可选，HH:mm）",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 10, 0, 6)
        };

        var timeTextBox = new TextBox
        {
            Text = selectedItem.DueTime?.ToString("HH:mm") ?? string.Empty,
            FontSize = 14,
            MinHeight = 40,
            Margin = new Thickness(0, 0, 0, 4),
            MaxLength = 5,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        if (Application.Current.Resources["ModernTextBoxStyle"] is Style timeStyle)
        {
            timeTextBox.Style = timeStyle;
        }

        var timeErrorText = new TextBlock
        {
            Text = string.Empty,
            Foreground = (Brush)Application.Current.Resources["DangerBrush"],
            FontSize = 12,
            Margin = new Thickness(2, 0, 0, 6),
            Visibility = Visibility.Collapsed
        };

        var offsetLabel = new TextBlock
        {
            Text = "提前提醒",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 10, 0, 6)
        };

        var offsetOptions = new System.Collections.Generic.List<OffsetOption>
        {
            new("不提前（截止时提醒）", 0),
            new("提前 5 分钟", 5),
            new("提前 10 分钟", 10),
            new("提前 15 分钟", 15),
            new("提前 30 分钟", 30),
            new("提前 1 小时", 60),
            new("提前 2 小时", 120)
        };

        var offsetComboBox = new ComboBox
        {
            ItemsSource = offsetOptions,
            DisplayMemberPath = "Display",
            SelectedValuePath = "Minutes",
            SelectedValue = selectedItem.ReminderOffsetMinutes ?? 0,
            Margin = new Thickness(0, 0, 0, 4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            FontSize = 14
        };
        if (Application.Current.Resources["ModernComboBoxStyle"] is Style comboBoxStyle)
        {
            offsetComboBox.Style = comboBoxStyle;
        }

        editPanel.Children.Add(titleLabel);
        editPanel.Children.Add(titleTextBox);
        editPanel.Children.Add(dateLabel);
        editPanel.Children.Add(datePicker);
        editPanel.Children.Add(timeLabel);
        editPanel.Children.Add(timeTextBox);
        editPanel.Children.Add(timeErrorText);
        editPanel.Children.Add(offsetLabel);
        editPanel.Children.Add(offsetComboBox);

        var originalTitle = selectedItem.Title;
        var originalDueDate = selectedItem.DueDate;
        var originalDueTime = selectedItem.DueTime;
        var originalOffset = selectedItem.ReminderOffsetMinutes;

        DialogService.OnDialogConfirmed = content =>
        {
            if (content is not StackPanel panel)
            {
                return false;
            }

            if (panel.Children[1] is not TextBox textBox || string.IsNullOrWhiteSpace(textBox.Text))
            {
                UpdateStatus("标题不能为空");
                return false;
            }

            if (!TryParseTimeText(panel.Children[5] as TextBox, out var parsedTime, out var timeError))
            {
                timeErrorText.Text = timeError;
                timeErrorText.Visibility = Visibility.Visible;
                return false;
            }

            timeErrorText.Text = string.Empty;
            timeErrorText.Visibility = Visibility.Collapsed;

            selectedItem.Title = textBox.Text.Trim();

            if (panel.Children[3] is Controls.CalendarPopup picker)
            {
                selectedItem.DueDate = picker.SelectedDate;
            }

            selectedItem.DueTime = parsedTime;

            if (panel.Children[8] is ComboBox combo)
            {
                var minutes = combo.SelectedValue is int value ? value : 0;
                selectedItem.ReminderOffsetMinutes = minutes <= 0 ? null : minutes;
                // 仅当"日期 + 具体时间"同时存在时，才开启提醒
                selectedItem.HasReminder = selectedItem.DueDate.HasValue && selectedItem.DueTime.HasValue;
            }

            RefreshTaskCollections();
            SaveData();
            UpdateStatus($"已修改: {selectedItem.Title}");
            return true;
        };

        var result = DialogService.ShowCustomDialog("修改待办事项", DialogType.None, editPanel, "保存", "取消");
        if (result == Services.DialogResult.Cancel)
        {
            selectedItem.Title = originalTitle;
            selectedItem.DueDate = originalDueDate;
            selectedItem.DueTime = originalDueTime;
            selectedItem.ReminderOffsetMinutes = originalOffset;
        }

        DialogService.OnDialogConfirmed = null;
    }

    private static bool TryParseTimeText(TextBox? textBox, out TimeOnly? time, out string error)
    {
        time = null;
        error = string.Empty;
        if (textBox == null)
        {
            return true;
        }

        var raw = textBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            return true;
        }

        if (!TimeOnly.TryParseExact(raw, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
        {
            error = "时间格式应为 HH:mm（如 09:30 或 18:00）";
            return false;
        }

        time = parsed;
        return true;
    }

    private sealed record OffsetOption(string Display, int Minutes)
    {
        public override string ToString() => Display;
    }

    private void TaskCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            var listBoxItem = FindParent<ListBoxItem>(checkBox);
            if (listBoxItem != null)
            {
                PlayAnimationOnItemContainer(listBoxItem, "TaskActionAnimation", () =>
                {
                    RefreshTaskCollections();
                    UpdateTaskCount();
                    SaveData();
                });
                return;
            }
        }

        RefreshTaskCollections();
        UpdateTaskCount();
        SaveData();
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            ExecuteWithAnimation(button, () => MoveTaskToTrash(todoItem));
        }
    }

    private void RestoreTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            ExecuteWithAnimation(button, () => RestoreDeletedTask(todoItem));
        }
    }

    private void RestoreTask_Click(object sender, RoutedEventArgs e)
    {
        if (DeletedTasksListBox?.SelectedItem is TodoItem todoItem)
        {
            ExecuteWithAnimation(DeletedTasksListBox, todoItem, () => RestoreDeletedTask(todoItem));
        }
    }

    private void PermanentDeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            ExecuteWithAnimation(button, () => PermanentlyDeleteTask(todoItem));
        }
    }

    private void PermanentDeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (DeletedTasksListBox?.SelectedItem is TodoItem todoItem)
        {
            ExecuteWithAnimation(DeletedTasksListBox, todoItem, () => PermanentlyDeleteTask(todoItem));
        }
    }

    private void EmptyTrashButton_Click(object sender, RoutedEventArgs e)
    {
        var result = DialogService.ShowConfirm("确定要清空垃圾箱吗？所有任务将被永久删除！", "确认清空");
        if (result != Services.DialogResult.OK)
        {
            return;
        }

        var itemsToRemove = _todoItems.Where(t => t.IsDeleted).ToList();
        ExecuteBatchAnimation(DeletedTasksListBox, itemsToRemove, () =>
        {
            _viewModel.EmptyTrash();
            RefreshTaskCollections();
            UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
        });
    }

    private void ArchiveTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            ExecuteWithAnimation(button, () => ArchiveTask(todoItem));
        }
    }

    private void ArchiveTask_Click(object sender, RoutedEventArgs e)
    {
        if (CompletedTasksListBox?.SelectedItem is TodoItem todoItem)
        {
            ExecuteWithAnimation(CompletedTasksListBox, todoItem, () => ArchiveTask(todoItem));
        }
    }

    private void UnarchiveTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            ExecuteWithAnimation(button, () => UnarchiveTask(todoItem));
        }
    }

    private void UnarchiveTask_Click(object sender, RoutedEventArgs e)
    {
        if (ArchivedGroupsListBox?.SelectedItem is TodoItem todoItem)
        {
            ExecuteWithAnimation(ArchivedGroupsListBox, todoItem, () => UnarchiveTask(todoItem));
        }
    }

    private void YearExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button && button.Tag is ArchivedGroup group)
        {
            if (button.IsChecked == true)
            {
                group.ExpandAll();
            }
            else
            {
                group.CollapseAll();
            }
        }
    }

    private void UnarchiveGroupTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ArchivedGroup group)
        {
            return;
        }

        _viewModel.UnarchiveGroup(group);
        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void UnarchiveAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = DialogService.ShowConfirm("确定要恢复所有归档的任务吗？", "确认恢复");
        if (result != Services.DialogResult.OK)
        {
            return;
        }

        _viewModel.UnarchiveAll();
        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void MoveTaskToTrash(TodoItem todoItem)
    {
        _viewModel.MoveTaskToTrash(todoItem);
        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void RestoreDeletedTask(TodoItem todoItem)
    {
        _viewModel.RestoreDeletedTask(todoItem);
        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void PermanentlyDeleteTask(TodoItem todoItem)
    {
        _viewModel.PermanentlyDeleteTask(todoItem);
        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void ArchiveTask(TodoItem todoItem)
    {
        _viewModel.ArchiveTask(todoItem);
        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void UnarchiveTask(TodoItem todoItem)
    {
        _viewModel.UnarchiveTask(todoItem);
        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }
}
