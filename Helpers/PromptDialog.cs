using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace ArkCommander.Helpers;

public static class PromptDialog
{
    public static bool TryPrompt(string title, string label, out string value)
    {
        string result = string.Empty;
        bool accepted = false;

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };

        if (Application.Current?.MainWindow != null)
            dialog.Owner = Application.Current.MainWindow;

        var panel = new StackPanel
        {
            Margin = new Thickness(16)
        };

        var textBlock = new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var textBox = new TextBox
        {
            Margin = new Thickness(0, 0, 0, 12)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 90,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };

        var cancelButton = new Button
        {
            Content = "Отмена",
            Width = 90,
            IsCancel = true
        };

        okButton.Click += (s, e) =>
        {
            result = textBox.Text;
            accepted = true;
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            dialog.Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        panel.Children.Add(textBlock);
        panel.Children.Add(textBox);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;
        dialog.ShowDialog();

        value = result;
        return accepted;
    }
}