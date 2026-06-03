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
            Margin = new Thickness(0, 8, 0, 8)
        };

        var titleLabel = new TextBlock
        {
            Text = "标题",
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 0, 0, 6)
        };

        var titleTextBox = new TextBox
        {
            Text = selectedItem.Title,
            FontSize = 14,
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 16),
            MaxLength = Constants.AppConstants.MaxTitleLength,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (Application.Current.Resources["ModernTextBoxStyle"] is Style titleStyle)
        {
            titleTextBox.Style = titleStyle;
        }

        var dateLabel = new TextBlock
        {
            Text = "截止日期（可选）",
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 0, 0, 6)
        };

        var datePicker = new global::HandyControl.Controls.DatePicker
        {
            SelectedDate = selectedItem.DueDate,
            FontSize = 14,
            Height = 36,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 8),
            MinWidth = 160,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        datePicker.SetResourceReference(Control.BackgroundProperty, "InputBrush");
        datePicker.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");
        datePicker.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
        datePicker.SetResourceReference(System.Windows.Controls.DatePicker.CalendarStyleProperty, "ModernDatePickerCalendarStyle");


        editPanel.Children.Add(titleLabel);
        editPanel.Children.Add(titleTextBox);
        editPanel.Children.Add(dateLabel);
        editPanel.Children.Add(datePicker);

        var originalTitle = selectedItem.Title;
        var originalDueDate = selectedItem.DueDate;

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

            selectedItem.Title = textBox.Text.Trim();

            if (panel.Children[3] is global::HandyControl.Controls.DatePicker picker)
            {
                selectedItem.DueDate = picker.SelectedDate;
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
        }

        DialogService.OnDialogConfirmed = null;
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
