using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class MainWindow
{
    private void NewTaskTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        UpdatePlaceholderVisibility(forceHidden: true);
    }

    private void NewTaskTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        UpdatePlaceholderVisibility();
    }

    private void NewTaskTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholderVisibility();
    }

    private void UpdatePlaceholderVisibility(bool forceHidden = false)
    {
        if (PlaceholderText == null || NewTaskTextBox == null)
        {
            return;
        }

        PlaceholderText.Visibility = forceHidden || !string.IsNullOrEmpty(NewTaskTextBox.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void TaskTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskTabs == null || e.Source != TaskTabs)
        {
            return;
        }

        if (TaskTabs.Template.FindName("PART_SelectedContentHost", TaskTabs) is not ContentPresenter contentHost)
        {
            return;
        }

        if (Application.Current.TryFindResource("TabSlideInAnimation") is not Storyboard storyboard)
        {
            return;
        }

        try
        {
            contentHost.RenderTransform = new TranslateTransform(50, 0);
            contentHost.Opacity = 0;

            var clonedStoryboard = storyboard.Clone();
            clonedStoryboard.Begin(contentHost);
        }
        catch
        {
            contentHost.RenderTransform = new TranslateTransform(0, 0);
            contentHost.Opacity = 1;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ResizeMode == ResizeMode.CanResize)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void ClearDateButton_Click(object sender, RoutedEventArgs e)
    {
        if (DueDatePicker != null)
        {
            DueDatePicker.SelectedDate = null;
            UpdateStatus("已清除截止日期");
        }
    }

    private void DueDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DueDatePicker?.SelectedDate != null && DueDatePicker.SelectedDate < DateTime.Now.Date)
        {
            UpdateStatus("提醒：选择的日期已过期");
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsWindow();
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Instance.ToggleExplicitTheme();
        UpdateStatus(ThemeService.Instance.IsDarkTheme ? "已切换到深色主题" : "已切换到浅色主题");
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyNativeWindowAppearance();
        UpdateThemeToggleButton();
        WidgetView?.RefreshThemeBrushes();
        _widgetWindow?.RefreshThemeBrushes();
    }

    private void UpdateThemeToggleButton()
    {
        if (ThemeToggleButton == null || ThemeToggleButtonIcon == null || ThemeToggleButtonIconBox == null)
        {
            return;
        }

        var isDarkTheme = ThemeService.Instance.IsDarkTheme;
        var iconKey = isDarkTheme ? "DayIconGeometry" : "NightIconGeometry";
        var iconSizeKey = isDarkTheme ? "ThemeDayIconSize" : "ThemeNightIconSize";

        ThemeToggleButton.ToolTip = isDarkTheme ? "切换到浅色" : "切换到深色";
        if (Application.Current.TryFindResource(iconKey) is Geometry iconGeometry)
        {
            ThemeToggleButtonIcon.Data = iconGeometry;
        }

        if (Application.Current.TryFindResource(iconSizeKey) is double iconSize)
        {
            ThemeToggleButtonIconBox.Width = iconSize;
            ThemeToggleButtonIconBox.Height = iconSize;
        }
    }

    public void ShowSettingsWindow()
    {
        var settingsWindow = new SettingsWindow
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }

    private void AdjustFontSizeForDpi()
    {
        var dpiScale = VisualTreeHelper.GetDpi(this);
        var dpiFactor = dpiScale.DpiScaleX;

        var baseFontSize = 14;
        if (dpiFactor > 1.5)
        {
            baseFontSize = 16;
        }
        else if (dpiFactor > 1.25)
        {
            baseFontSize = 15;
        }

        FontSize = baseFontSize;
    }

    private void PlayAnimationOnItemContainer(FrameworkElement itemContainer, string resourceKey, Action? onCompleted)
    {
        if (Application.Current.TryFindResource(resourceKey) is not Storyboard storyboard)
        {
            onCompleted?.Invoke();
            return;
        }

        try
        {
            if (itemContainer.RenderTransform == null || itemContainer.RenderTransform is not ScaleTransform)
            {
                itemContainer.RenderTransform = new ScaleTransform(1, 1);
                itemContainer.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            var clonedStoryboard = storyboard.Clone();
            clonedStoryboard.Completed += (_, _) => onCompleted?.Invoke();
            clonedStoryboard.Begin(itemContainer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"动画播放失败: {ex.Message}");
            onCompleted?.Invoke();
        }
    }

    private static T? FindParent<T>(DependencyObject child)
        where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T typedParent)
            {
                return typedParent;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    private void ExecuteWithAnimation(Button button, Action action)
    {
        var itemContainer = FindAnimationContainer(button);
        if (itemContainer != null)
        {
            PlayAnimationOnItemContainer(itemContainer, "TaskActionAnimation", action);
            return;
        }

        action();
    }

    private void ExecuteWithAnimation(ListBox listBox, TodoItem todoItem, Action action)
    {
        var listBoxItem = listBox.ItemContainerGenerator.ContainerFromItem(todoItem) as ListBoxItem;
        if (listBoxItem != null)
        {
            PlayAnimationOnItemContainer(listBoxItem, "TaskActionAnimation", action);
            return;
        }

        action();
    }

    private void ExecuteWithAnimation(TreeView treeView, TodoItem todoItem, Action action)
    {
        treeView.UpdateLayout();
        var treeViewItem = FindTreeViewItem(treeView, todoItem);
        if (treeViewItem != null)
        {
            PlayAnimationOnItemContainer(treeViewItem, "TaskActionAnimation", action);
            return;
        }

        action();
    }

    private static FrameworkElement? FindAnimationContainer(DependencyObject child)
    {
        var listBoxItem = FindParent<ListBoxItem>(child);
        if (listBoxItem != null)
        {
            return listBoxItem;
        }

        return FindParent<TreeViewItem>(child);
    }

    private static TreeViewItem? FindTreeViewItem(ItemsControl parent, object item)
    {
        if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem directItem)
        {
            return directItem;
        }

        foreach (var child in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(child) is not TreeViewItem treeViewItem)
            {
                continue;
            }

            var nestedItem = FindTreeViewItem(treeViewItem, item);
            if (nestedItem != null)
            {
                return nestedItem;
            }
        }

        return null;
    }

    private void ExecuteBatchAnimation(ListBox listBox, List<TodoItem> items, Action onCompleted)
    {
        if (items.Count == 0)
        {
            onCompleted();
            return;
        }

        var listBoxItems = new List<ListBoxItem>();
        foreach (var item in items)
        {
            if (listBox.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem listBoxItem)
            {
                listBoxItems.Add(listBoxItem);
            }
        }

        if (listBoxItems.Count == 0)
        {
            onCompleted();
            return;
        }

        if (Application.Current.TryFindResource("TaskActionAnimation") is not Storyboard storyboard)
        {
            onCompleted();
            return;
        }

        var completedCount = 0;
        var totalCount = listBoxItems.Count;

        foreach (var listBoxItem in listBoxItems)
        {
            try
            {
                var clonedStoryboard = storyboard.Clone();
                clonedStoryboard.Completed += (_, _) =>
                {
                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        onCompleted();
                    }
                };
                clonedStoryboard.Begin(listBoxItem);
            }
            catch
            {
                completedCount++;
                if (completedCount >= totalCount)
                {
                    onCompleted();
                }
            }
        }
    }
}
