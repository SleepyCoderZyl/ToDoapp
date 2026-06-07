using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ToDoapp.ViewModels;
using ToDoapp.ViewModels.Settings;

namespace ToDoapp.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();

        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;

        Loaded += SettingsWindow_Loaded;
    }

    private void SettingsWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // 默认选中第一项（已在 ViewModel 构造时设过 CurrentPage）
        // 这里把 ListBox 的选中状态同步过去，以便高亮显示
        if (_viewModel.CurrentPage != null)
        {
            SelectInListBox(_viewModel.CurrentPage);
        }
    }

    private void SelectInListBox(SettingsPageViewModel page)
    {
        if (page.Category == ToDoapp.Models.SettingCategory.General)
        {
            AppearanceSettingsList.SelectedItem = null;
            GeneralSettingsList.SelectedItem = page;
        }
        else
        {
            GeneralSettingsList.SelectedItem = null;
            AppearanceSettingsList.SelectedItem = page;
        }
    }

    private void SettingItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not SettingsPageViewModel selected) return;

        _viewModel.CurrentPage = selected;

        // 互斥：另一个 ListBox 取消选中
        if (listBox == GeneralSettingsList)
        {
            AppearanceSettingsList.SelectedItem = null;
        }
        else if (listBox == AppearanceSettingsList)
        {
            GeneralSettingsList.SelectedItem = null;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
