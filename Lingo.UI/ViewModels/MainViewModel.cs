using System.Collections.Generic;
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
    private bool _isKeyTreeViewMode = true;

    [ObservableProperty]
    private KeyEditorViewModel _keyEditor = new();

    [ObservableProperty]
    private string _statusInfo = "Ready. Open a translation file to begin.";

    [ObservableProperty]
    private LoadedTranslationGroup? _loadedGroup;

    [ObservableProperty]
    private bool _hasLoadedFiles;

    public MainViewModel(IFileDialogService? fileDialogService = null)
    {
        _fileDialogService = fileDialogService;
        _keyEditor.Clear();
    }

    [RelayCommand]
    public void ToggleViewMode()
    {
        IsKeyTreeViewMode = !IsKeyTreeViewMode;
    }

    [RelayCommand]
    public async Task OpenFileAsync()
    {
        if (_fileDialogService == null)
        {
            StatusInfo = "File dialog service is not configured.";
            return;
        }

        var path = await _fileDialogService.OpenFileDialogAsync();
        if (!string.IsNullOrEmpty(path))
        {
            LoadTranslationFile(path);
        }
    }

    public void LoadTranslationFile(string path)
    {
        var group = _fileLoader.LoadGroupFromPath(path);
        if (group == null || group.Files.Count == 0)
        {
            StatusInfo = $"Error: Unable to load translation file or find siblings from '{path}'.";
            return;
        }

        LoadedGroup = group;
        KeyEditor.LoadTranslationGroup(group);
        HasLoadedFiles = true;

        Title = $"Lingo Localization Editor - {group.GroupStub} ({group.Files.Count} files)";
        StatusInfo = $"Loaded '{group.GroupStub}' with {group.Files.Count} sibling file(s) from {group.DirectoryPath}";
    }

    [RelayCommand]
    public void SaveAllFiles()
    {
        if (LoadedGroup == null)
        {
            StatusInfo = "No files loaded to save.";
            return;
        }

        KeyEditor.SaveAllTranslations();
        _fileLoader.SaveGroup(LoadedGroup);
        StatusInfo = $"Saved {LoadedGroup.Files.Count} translation files to disk.";
    }

    [RelayCommand]
    public void CloseFiles()
    {
        KeyEditor.Clear();
        LoadedGroup = null;
        HasLoadedFiles = false;
        Title = "Lingo Localization Editor";
        StatusInfo = "Ready. Open a translation file to begin.";
    }
}
