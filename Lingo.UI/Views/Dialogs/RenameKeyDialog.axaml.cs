using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Lingo.UI.Views.Dialogs;

public partial class RenameKeyDialog : Window
{
    private readonly string _originalKey;

    public RenameKeyDialog() : this(string.Empty)
    {
    }

    public RenameKeyDialog(string originalKey)
    {
        _originalKey = originalKey;
        InitializeComponent();
        CurrentKeyText.Text = originalKey;
        NewKeyTextBox.Text = originalKey;
        NewKeyTextBox.Focus();
        NewKeyTextBox.SelectAll();
    }

    private void OnRenameClick(object? sender, RoutedEventArgs e)
    {
        var newKey = NewKeyTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(newKey))
        {
            ErrorText.Text = "Key name cannot be empty.";
            ErrorText.IsVisible = true;
            return;
        }

        if (string.Equals(newKey, _originalKey, StringComparison.Ordinal))
        {
            ErrorText.Text = "New key name must be different from current key.";
            ErrorText.IsVisible = true;
            return;
        }

        Close(newKey);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
