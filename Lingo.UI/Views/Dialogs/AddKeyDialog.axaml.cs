using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Lingo.Core.Files;
using Lingo.UI.Services;

namespace Lingo.UI.Views.Dialogs;

public partial class AddKeyDialog : Window
{
    private readonly IReadOnlyList<LoadedTranslationGroup> _availableGroups;

    public AddKeyDialog() : this(Array.Empty<LoadedTranslationGroup>(), null)
    {
    }

    public AddKeyDialog(IReadOnlyList<LoadedTranslationGroup> availableGroups, LoadedTranslationGroup? defaultGroup)
    {
        _availableGroups = availableGroups;
        InitializeComponent();

        if (availableGroups.Count > 0)
        {
            TargetGroupComboBox.ItemsSource = availableGroups;
            TargetGroupComboBox.SelectedItem = defaultGroup ?? availableGroups.FirstOrDefault();
            GroupSelectionPanel.IsVisible = availableGroups.Count > 1;
        }
        else
        {
            GroupSelectionPanel.IsVisible = false;
        }

        KeyPathTextBox.Focus();
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        var keyPath = KeyPathTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            ErrorText.Text = "Key path cannot be empty.";
            ErrorText.IsVisible = true;
            return;
        }

        var defaultSource = SourceTextBox.Text?.Trim() ?? string.Empty;
        var selectedGroup = TargetGroupComboBox.SelectedItem as LoadedTranslationGroup;

        Close(new AddKeyDialogResult(keyPath, defaultSource, selectedGroup));
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
