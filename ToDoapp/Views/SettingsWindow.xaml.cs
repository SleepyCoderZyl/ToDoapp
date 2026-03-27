using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ToDoapp.Models;
using ToDoapp.Services;
using ToDoapp.ViewModels;

namespace ToDoapp.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private bool _isInitializing = true;

    public SettingsWindow()
    {
        InitializeComponent();
        
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
        
        Loaded += SettingsWindow_Loaded;
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettingItems();
        _isInitializing = false;
        
        if (GeneralSettingsList.Items.Count > 0)
        {
            GeneralSettingsList.SelectedIndex = 0;
        }
        else if (AppearanceSettingsList.Items.Count > 0)
        {
            AppearanceSettingsList.SelectedIndex = 0;
        }
    }

    private void LoadSettingItems()
    {
        var generalItems = _viewModel.SettingItems
            .Where(i => i.Category == SettingCategory.General)
            .ToList();
        
        var appearanceItems = _viewModel.SettingItems
            .Where(i => i.Category == SettingCategory.Appearance)
            .ToList();

        foreach (var item in generalItems)
        {
            GeneralSettingsList.Items.Add(item);
        }

        foreach (var item in appearanceItems)
        {
            AppearanceSettingsList.Items.Add(item);
        }
    }

    private void SettingItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (sender is ListBox listBox && listBox.SelectedItem is SettingItem selectedItem)
        {
            _viewModel.SelectedSettingItem = selectedItem;
            ContentArea.Content = selectedItem.ContentControl;
            
            if (listBox == GeneralSettingsList && AppearanceSettingsList != null)
            {
                AppearanceSettingsList.SelectedItem = null;
            }
            else if (listBox == AppearanceSettingsList && GeneralSettingsList != null)
            {
                GeneralSettingsList.SelectedItem = null;
            }
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
