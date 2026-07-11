using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ToDoapp.Services;

public enum DialogResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

public enum DialogType
{
    None,
    Information,
    Warning,
    Error,
    Question,
    Success
}

public static class DialogService
{
    public static Func<FrameworkElement, bool>? OnDialogConfirmed { get; set; }

    public static DialogResult ShowCustomDialog(
        string title,
        DialogType dialogType,
        FrameworkElement customContent,
        string primaryButtonText = "确定",
        string? secondaryButtonText = "取消",
        Action<Button, Button>? configureButtons = null,
        bool showTitleCloseButton = false,
        double? dialogWidth = null)
    {
        var dialog = new Window
        {
            Title = title,
            Width = dialogWidth ?? 380,
            SizeToContent = SizeToContent.Height,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
            ShowInTaskbar = false,
            Owner = Application.Current.MainWindow
        };

        var border = new Border { Padding = new Thickness(24) };
        border.SetResourceReference(FrameworkElement.StyleProperty, "DialogFrameStyle");

        var contentGrid = new Grid();
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titlePanel = new Grid
        {
            Margin = new Thickness(0, 0, 0, 12)
        };
        titlePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titlePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleContentPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var iconTextBlock = new TextBlock
        {
            FontSize = 20,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.Bold
        };

        string icon = "";
        string? iconBrushKey = null;

        switch (dialogType)
        {
            case DialogType.None:
                iconTextBlock.Visibility = Visibility.Collapsed;
                break;
            case DialogType.Information:
                icon = "\u2139";
                iconBrushKey = "DialogForegroundBrush";
                break;
            case DialogType.Warning:
                icon = "\u26A0";
                iconBrushKey = "WarningBrush";
                break;
            case DialogType.Error:
                icon = "\u2717";
                iconBrushKey = "DangerBrush";
                break;
            case DialogType.Question:
                icon = "?";
                iconBrushKey = "DialogSecondaryForegroundBrush";
                break;
            case DialogType.Success:
                icon = "\u2713";
                iconBrushKey = "PrimaryButtonBackgroundBrush";
                break;
        }

        iconTextBlock.Text = icon;
        if (iconBrushKey is not null)
        {
            iconTextBlock.SetResourceReference(TextBlock.ForegroundProperty, iconBrushKey);
        }

        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        titleText.SetResourceReference(TextBlock.ForegroundProperty, "DialogForegroundBrush");

        var titleCloseButton = new Button
        {
            Style = Application.Current.Resources["IconButtonStyle"] as Style,
            Visibility = showTitleCloseButton ? Visibility.Visible : Visibility.Collapsed,
            Margin = new Thickness(12, 0, 0, 0),
            ToolTip = "关闭"
        };

        var closeIconPath = new Path
        {
            Data = (Geometry)Application.Current.Resources["CloseIconGeometry"],
            Stretch = Stretch.Uniform
        };
        closeIconPath.SetBinding(Shape.FillProperty, new Binding(nameof(Button.Foreground))
        {
            Source = titleCloseButton
        });

        titleCloseButton.Content = new Viewbox
        {
            Width = (double)Application.Current.Resources["DialogTitleBarIconSize"],
            Height = (double)Application.Current.Resources["DialogTitleBarIconSize"],
            Child = closeIconPath
        };

        titleContentPanel.Children.Add(iconTextBlock);
        titleContentPanel.Children.Add(titleText);
        Grid.SetColumn(titleContentPanel, 0);
        Grid.SetColumn(titleCloseButton, 1);
        titlePanel.Children.Add(titleContentPanel);
        titlePanel.Children.Add(titleCloseButton);
        Grid.SetRow(titlePanel, 0);
        Grid.SetRow(customContent, 1);

        var buttonGrid = new Grid();
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var primaryButton = new Button
        {
            Content = primaryButtonText,
            MinWidth = 80,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var secondaryButton = new Button
        {
            Content = secondaryButtonText,
            MinWidth = 80,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(10, 0, 0, 0)
        };

        var primaryStyle = Application.Current.Resources["DialogPrimaryButtonStyle"] as Style;
        if (primaryStyle != null)
        {
            primaryButton.Style = primaryStyle;
        }

        var secondaryStyle = Application.Current.Resources["DialogButtonStyle"] as Style;
        if (secondaryStyle != null)
        {
            secondaryButton.Style = secondaryStyle;
        }

        configureButtons?.Invoke(primaryButton, secondaryButton);

        var hasVisibleButtons = primaryButton.Visibility != Visibility.Collapsed
            || secondaryButton.Visibility != Visibility.Collapsed;
        buttonGrid.Visibility = hasVisibleButtons ? Visibility.Visible : Visibility.Collapsed;
        customContent.Margin = new Thickness(0, 0, 0, hasVisibleButtons ? 20 : 0);

        bool? dialogCloseResult = null;

        void CloseAsCancel()
        {
            dialogCloseResult = false;
            dialog.DialogResult = false;
            dialog.Close();
        }

        primaryButton.Click += (s, e) =>
        {
            if (OnDialogConfirmed != null && OnDialogConfirmed(customContent))
            {
                dialogCloseResult = true;
                dialog.DialogResult = true;
            }
            else if (OnDialogConfirmed == null)
            {
                dialogCloseResult = true;
                dialog.DialogResult = true;
            }
            dialog.Close();
        };

        secondaryButton.Click += (s, e) => CloseAsCancel();
        titleCloseButton.Click += (s, e) => CloseAsCancel();

        Grid.SetColumn(primaryButton, 0);
        Grid.SetColumn(secondaryButton, 1);
        buttonGrid.Children.Add(primaryButton);
        buttonGrid.Children.Add(secondaryButton);
        Grid.SetRow(buttonGrid, 2);

        contentGrid.Children.Add(titlePanel);
        contentGrid.Children.Add(customContent);
        contentGrid.Children.Add(buttonGrid);

        border.Child = contentGrid;
        dialog.Content = border;

        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                dialog.DragMove();
            }
        };

        dialog.ShowDialog();

        if (dialogCloseResult == true)
        {
            return DialogResult.OK;
        }
        return DialogResult.Cancel;
    }

    public static DialogResult ShowConfirm(string message, string title = "确认")
    {
        return ShowDialog(title, message, DialogType.Question, "确定", "取消");
    }

    public static DialogResult ShowConfirm(string message, string title, DialogType dialogType)
    {
        return ShowDialog(title, message, dialogType, "确定", "取消");
    }

    private static DialogResult ShowDialog(string title, string message, DialogType dialogType, string primaryButtonText, string? secondaryButtonText = null)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 380,
            Height = 200,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
            ShowInTaskbar = false,
            Owner = Application.Current.MainWindow
        };

        var border = new Border { Padding = new Thickness(24) };
        border.SetResourceReference(FrameworkElement.StyleProperty, "DialogFrameStyle");

        var contentGrid = new Grid();
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titlePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var iconTextBlock = new TextBlock
        {
            FontSize = 20,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        string icon;
        string iconBrushKey;

        switch (dialogType)
        {
            case DialogType.Information:
                icon = "\u2139";
                iconBrushKey = "DialogForegroundBrush";
                break;
            case DialogType.Warning:
                icon = "\u26A0";
                iconBrushKey = "WarningBrush";
                break;
            case DialogType.Error:
                icon = "\u2717";
                iconBrushKey = "DangerBrush";
                break;
            case DialogType.Question:
                icon = "?";
                iconBrushKey = "DialogSecondaryForegroundBrush";
                break;
            case DialogType.Success:
                icon = "\u2713";
                iconBrushKey = "PrimaryButtonBackgroundBrush";
                break;
            default:
                icon = "\u2139";
                iconBrushKey = "DialogForegroundBrush";
                break;
        }

        iconTextBlock.Text = icon;
        iconTextBlock.SetResourceReference(TextBlock.ForegroundProperty, iconBrushKey);
        iconTextBlock.FontWeight = FontWeights.Bold;

        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleText.SetResourceReference(TextBlock.ForegroundProperty, "DialogForegroundBrush");

        titlePanel.Children.Add(iconTextBlock);
        titlePanel.Children.Add(titleText);
        Grid.SetRow(titlePanel, 0);

        var messageText = new TextBlock
        {
            Text = message,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20),
            VerticalAlignment = VerticalAlignment.Center
        };
        messageText.SetResourceReference(TextBlock.ForegroundProperty, "DialogSecondaryForegroundBrush");
        Grid.SetRow(messageText, 1);

        var buttonGrid = new Grid();
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        var primaryButton = new Button
        {
            Content = primaryButtonText,
            MinWidth = 80,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 10, 0)
        };

        if (secondaryButtonText != null)
        {
            var secondaryButton = new Button
            {
                Content = secondaryButtonText,
                MinWidth = 80,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(10, 0, 0, 0)
            };

            var secondaryStyle = Application.Current.Resources["DialogButtonStyle"] as Style;
            if (secondaryStyle != null)
            {
                secondaryButton.Style = secondaryStyle;
            }

            var primaryStyle = Application.Current.Resources["DialogPrimaryButtonStyle"] as Style;
            if (primaryStyle != null)
            {
                primaryButton.Style = primaryStyle;
            }

            secondaryButton.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            Grid.SetColumn(secondaryButton, 1);
            buttonGrid.Children.Add(secondaryButton);
        }
        else
        {
            var primaryStyle = Application.Current.Resources["DialogPrimaryButtonStyle"] as Style;
            if (primaryStyle != null)
            {
                primaryButton.Style = primaryStyle;
            }
        }

        primaryButton.Click += (s, e) =>
        {
            dialog.DialogResult = true;
            dialog.Close();
        };

        Grid.SetColumn(primaryButton, 0);
        buttonGrid.Children.Add(primaryButton);
        Grid.SetRow(buttonGrid, 2);

        contentGrid.Children.Add(titlePanel);
        contentGrid.Children.Add(messageText);
        contentGrid.Children.Add(buttonGrid);

        border.Child = contentGrid;
        dialog.Content = border;

        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                dialog.DragMove();
            }
        };

        var result = dialog.ShowDialog();

        if (result == true)
        {
            return primaryButtonText switch
            {
                "确定" => DialogResult.OK,
                "是" => DialogResult.Yes,
                "取消" => DialogResult.Cancel,
                "否" => DialogResult.No,
                _ => DialogResult.OK
            };
        }

        return secondaryButtonText switch
        {
            "取消" => DialogResult.Cancel,
            "否" => DialogResult.No,
            _ => DialogResult.None
        };
    }
}
