using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Lingo.Core.Files;
using Lingo.UI.Views.Dialogs;

namespace Lingo.UI.Services;

public class AvaloniaDialogService : IDialogService
{
    private readonly Func<Window?> _windowProvider;

    public AvaloniaDialogService(Func<Window?> windowProvider)
    {
        _windowProvider = windowProvider;
    }

    public async Task<string?> ShowRenameKeyDialogAsync(string currentKey)
    {
        var owner = _windowProvider();
        if (owner == null) return null;

        var dialog = new RenameKeyDialog(currentKey);
        return await dialog.ShowDialog<string?>(owner);
    }

    public async Task<LoadedTranslationGroup?> ShowMoveKeyDialogAsync(
        string currentKey,
        IReadOnlyList<LoadedTranslationGroup> availableGroups,
        LoadedTranslationGroup? currentGroup)
    {
        var owner = _windowProvider();
        if (owner == null) return null;

        var dialog = new MoveKeyDialog(currentKey, availableGroups, currentGroup);
        return await dialog.ShowDialog<LoadedTranslationGroup?>(owner);
    }

    public async Task<AddKeyDialogResult?> ShowAddKeyDialogAsync(
        IReadOnlyList<LoadedTranslationGroup> availableGroups,
        LoadedTranslationGroup? defaultGroup)
    {
        var owner = _windowProvider();
        if (owner == null) return null;

        var dialog = new AddKeyDialog(availableGroups, defaultGroup);
        return await dialog.ShowDialog<AddKeyDialogResult?>(owner);
    }
}
