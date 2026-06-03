using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;
public static partial class SettingsContentFactory
{
    private static FrameworkElement CreateOpacitySettingContentCore()
    {
        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "透明度设置",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "分别调整小组件的背景和内容透明度。",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var backgroundLabel = new TextBlock
        {
            Text = "背景透明度",
            FontSize = 14,
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 10, 0, 10)
        };
        Grid.SetRow(backgroundLabel, 2);
        container.Children.Add(backgroundLabel);

        var backgroundSliderPanel = new Grid { Margin = new Thickness(0, 0, 0, 15) };
        backgroundSliderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        backgroundSliderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var backgroundSlider = new Slider
        {
            Minimum = 0.2,
            Maximum = 1.0,
            Value = WidgetOpacityManager.Instance.WidgetOpacity,
            Width = 300,
            Foreground = PrimaryBrush,
            SmallChange = 0.1,
            LargeChange = 0.1,
            IsMoveToPointEnabled = true,
            TickFrequency = 0.1,
            TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center
        };



        var backgroundOpacityValueText = new TextBlock
        {
            Text = $"{Math.Round(WidgetOpacityManager.Instance.WidgetOpacity * 100)}%",
            FontSize = 14,
            Foreground = TextSecondaryBrush,
            Width = 50,
            TextAlignment = TextAlignment.Left,
            Margin = new Thickness(15, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        backgroundSlider.ValueChanged += (s, e) =>
        {
            var value = (int)Math.Round(e.NewValue * 100);
            backgroundOpacityValueText.Text = $"{value}%";
            WidgetOpacityManager.Instance.SetOpacity(e.NewValue);
        };

        Grid.SetColumn(backgroundSlider, 0);
        Grid.SetColumn(backgroundOpacityValueText, 1);
        backgroundSliderPanel.Children.Add(backgroundSlider);
        backgroundSliderPanel.Children.Add(backgroundOpacityValueText);

        Grid.SetRow(backgroundSliderPanel, 3);
        container.Children.Add(backgroundSliderPanel);

        var contentLabel = new TextBlock
        {
            Text = "内容透明度",
            FontSize = 14,
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 15, 0, 10)
        };
        Grid.SetRow(contentLabel, 4);
        container.Children.Add(contentLabel);

        var contentSliderPanel = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        contentSliderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentSliderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var contentSlider = new Slider
        {
            Minimum = 0.2,
            Maximum = 1.0,
            Value = WidgetOpacityManager.Instance.WidgetContentOpacity,
            Width = 300,
            Foreground = PrimaryBrush,
            SmallChange = 0.1,
            LargeChange = 0.1,
            IsMoveToPointEnabled = true,
            TickFrequency = 0.1,
            TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center
        };



        var contentOpacityValueText = new TextBlock
        {
            Text = $"{Math.Round(WidgetOpacityManager.Instance.WidgetContentOpacity * 100)}%",
            FontSize = 14,
            Foreground = TextSecondaryBrush,
            Width = 50,
            TextAlignment = TextAlignment.Left,
            Margin = new Thickness(15, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        contentSlider.ValueChanged += (s, e) =>
        {
            var value = (int)Math.Round(e.NewValue * 100);
            contentOpacityValueText.Text = $"{value}%";
            WidgetOpacityManager.Instance.SetContentOpacity(e.NewValue);
        };

        Grid.SetColumn(contentSlider, 0);
        Grid.SetColumn(contentOpacityValueText, 1);
        contentSliderPanel.Children.Add(contentSlider);
        contentSliderPanel.Children.Add(contentOpacityValueText);

        Grid.SetRow(contentSliderPanel, 5);
        container.Children.Add(contentSliderPanel);

        return container;
    }

    private static FrameworkElement CreateAlwaysOnTopSettingContentCore()
    {
        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "小组件置顶",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "启用后，小组件将始终显示在所有其他窗口的最上层。",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var togglePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var toggleSwitch = new CheckBox
        {
            Name = "AlwaysOnTopToggle",
            IsChecked = SettingsService.Instance.Settings.WidgetAlwaysOnTop,
            FontSize = 14,
            Foreground = TextPrimaryBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Name = "AlwaysOnTopStatusText",
            Text = SettingsService.Instance.Settings.WidgetAlwaysOnTop ? "已启用" : "已禁用",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        toggleSwitch.Checked += (s, e) =>
        {
            SettingsService.Instance.UpdateWidgetAlwaysOnTop(true);
            statusText.Text = "已启用";
        };

        toggleSwitch.Unchecked += (s, e) =>
        {
            SettingsService.Instance.UpdateWidgetAlwaysOnTop(false);
            statusText.Text = "已禁用";
        };

        togglePanel.Children.Add(toggleSwitch);
        togglePanel.Children.Add(statusText);

        Grid.SetRow(togglePanel, 2);
        container.Children.Add(togglePanel);

        return container;
    }

}


