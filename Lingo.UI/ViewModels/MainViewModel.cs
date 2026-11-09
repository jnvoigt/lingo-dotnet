using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingo.Core.Documents;
using Lingo.Core.Files;
using Lingo.Core.Models;
using Lingo.UI.Services;

namespace Lingo.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IFileDialogService? _fileDialogService;
    private readonly TranslationFileLoader _fileLoader = new();

    [ObservableProperty]
    private string _title = "Lingo Localization Editor";

    [ObservableProperty]
    private KeyEditorViewModel _keyEditor;

    [ObservableProperty]
    private string _statusInfo = "Ready. Open translation file(s) to begin.";

    [ObservableProperty]
    private ObservableCollection<LoadedTranslationGroup> _loadedGroups = new();

    public LoadedTranslationGroup? LoadedGroup => LoadedGroups.FirstOrDefault();

    [ObservableProperty]
    private bool _hasLoadedFiles;

    public MainViewModel(IFileDialogService? fileDialogService = null, IDialogService? dialogService = null)
    {
        _fileDialogService = fileDialogService;
        _keyEditor = new KeyEditorViewModel(dialogService);
        _keyEditor.Clear();
    }

    [RelayCommand]
    public async Task OpenFileAsync()
    {
        if (_fileDialogService == null)
        {
            StatusInfo = "File dialog service is not configured.";
            return;
        }

        var paths = await _fileDialogService.OpenFilesDialogAsync();
        if (paths.Count > 0)
        {
            LoadTranslationFiles(paths);
        }
    }

    public void LoadTranslationFile(string path)
    {
        LoadTranslationFiles(new[] { path });
    }

    public void LoadTranslationFiles(IEnumerable<string> paths)
    {
        var groups = _fileLoader.LoadGroups(paths);
        if (groups.Count == 0)
        {
            StatusInfo = "Error: Unable to load translation file(s) or find siblings.";
            return;
        }

        LoadedGroups.Clear();
        foreach (var grp in groups)
        {
            LoadedGroups.Add(grp);
        }

        KeyEditor.LoadTranslationGroups(groups);
        HasLoadedFiles = true;

        var totalFiles = LoadedGroups.Sum(g => g.Files.Count);
        if (LoadedGroups.Count == 1)
        {
            var grp = LoadedGroups[0];
            Title = $"Lingo Localization Editor - {grp.GroupStub} ({grp.Files.Count} files)";
            StatusInfo = $"Loaded '{grp.GroupStub}' with {grp.Files.Count} sibling file(s) from {grp.DirectoryPath}";
        }
        else
        {
            Title = $"Lingo Localization Editor - {LoadedGroups.Count} translation groups ({totalFiles} files)";
            StatusInfo = $"Loaded {LoadedGroups.Count} translation group(s) with {totalFiles} total files.";
        }
    }

    [RelayCommand]
    public void SaveAllFiles()
    {
        if (LoadedGroups.Count == 0)
        {
            StatusInfo = "No files loaded to save.";
            return;
        }

        KeyEditor.SaveAllTranslations();
        var savedCount = _fileLoader.SaveModifiedFiles(LoadedGroups);
        StatusInfo = $"Saved {savedCount} modified translation file(s) to disk.";
    }

    [RelayCommand]
    public void ReloadAllFiles()
    {
        if (LoadedGroups.Count == 0)
        {
            StatusInfo = "No files loaded to reload.";
            return;
        }

        var filePaths = LoadedGroups
            .SelectMany(g => g.Files)
            .Select(f => f.FilePath)
            .Distinct()
            .ToList();

        LoadTranslationFiles(filePaths);
        StatusInfo = $"Reloaded {LoadedGroups.Count} translation group(s) from disk.";
    }

    [RelayCommand]
    public void ReloadGroup(LoadedTranslationGroup? group)
    {
        if (group == null) return;

        var groupPaths = group.Files.Select(f => f.FilePath).ToList();
        var reloadedGroups = _fileLoader.LoadGroups(groupPaths);
        var reloadedGroup = reloadedGroups.FirstOrDefault();
        if (reloadedGroup == null)
        {
            StatusInfo = $"Error: Unable to reload group '{group.GroupStub}' from disk.";
            return;
        }

        var idx = LoadedGroups.IndexOf(group);
        if (idx >= 0)
        {
            LoadedGroups[idx] = reloadedGroup;
        }
        else
        {
            var match = LoadedGroups.FirstOrDefault(g => g.GroupStub == group.GroupStub && g.DirectoryPath == group.DirectoryPath);
            if (match != null)
            {
                var matchIdx = LoadedGroups.IndexOf(match);
                LoadedGroups[matchIdx] = reloadedGroup;
            }
        }

        KeyEditor.LoadTranslationGroups(LoadedGroups);
        StatusInfo = $"Reloaded translation group '{reloadedGroup.GroupStub}' from disk.";
    }

    [RelayCommand]
    public void CloseFiles()
    {
        KeyEditor.Clear();
        LoadedGroups.Clear();
        HasLoadedFiles = false;
        Title = "Lingo Localization Editor";
        StatusInfo = "Ready. Open translation file(s) to begin.";
    }
}
