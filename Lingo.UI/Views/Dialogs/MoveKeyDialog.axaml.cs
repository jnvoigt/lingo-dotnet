using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Lingo.Core.Files;

namespace Lingo.UI.Views.Dialogs;

public partial class MoveKeyDialog : Window
{
    public MoveKeyDialog() : this(string.Empty, new List<LoadedTranslationGroup>(), null)
    {
    }

    public MoveKeyDialog(string keyPath, IReadOnlyList<LoadedTranslationGroup> availableGroups, LoadedTranslationGroup? currentGroup)
    {
        InitializeComponent();
        KeyToMoveText.Text = keyPath;
        CurrentGroupText.Text = currentGroup != null ? $"{currentGroup.GroupStub} ({currentGroup.Files.Count} files)" : "(Unknown)";
        TargetGroupComboBox.ItemsSource = availableGroups;
        TargetGroupComboBox.SelectedItem = availableGroups.FirstOrDefault();
    }

    private void OnMoveClick(object? sender, RoutedEventArgs e)
    {
        if (TargetGroupComboBox.SelectedItem is not LoadedTranslationGroup targetGroup)
        {
            ErrorText.Text = "Please select a destination group.";
            ErrorText.IsVisible = true;
            return;
        }

        Close(targetGroup);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
