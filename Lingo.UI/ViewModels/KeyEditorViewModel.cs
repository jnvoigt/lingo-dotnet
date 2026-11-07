using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingo.Core.Documents;
using Lingo.Core.Files;
using Lingo.Core.Hierarchy;
using Lingo.Core.Models;
using Lingo.Core.Query;
using Lingo.Core.Sync;
using Lingo.UI.Services;

namespace Lingo.UI.ViewModels;

public partial class KeyEditorViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogService;
    private readonly KeyHierarchyBuilder _hierarchyBuilder = new('.');
    private readonly KeySynchronizer _keySynchronizer = new();
    private readonly KeyQueryService _queryService = new();
    private readonly List<(ILingoDocument Document, string Name, string Language)> _documents = new();
    private List<KeyNode> _rawHierarchyNodes = new();

    public KeyEditorViewModel(IDialogService? dialogService = null)
    {
        _dialogService = dialogService;
    }

    [ObservableProperty]
    private ObservableCollection<LoadedTranslationGroup> _loadedGroups = new();

    [ObservableProperty]
    private LoadedTranslationGroup? _selectedGroup;

    public LoadedTranslationGroup? LoadedGroup => LoadedGroups.FirstOrDefault();

    [ObservableProperty]
    private ObservableCollection<KeyTreeNodeViewModel> _rootNodes = new();

    [ObservableProperty]
    private KeyTreeNodeViewModel? _selectedNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedKey))]
    [NotifyPropertyChangedFor(nameof(CanRevertKeyChanges))]
    [NotifyPropertyChangedFor(nameof(CanMoveKey))]
    [NotifyCanExecuteChangedFor(nameof(RevertKeyChangesCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedKeyCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameSelectedKeyCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenRenameKeyDialogCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveSelectedKeyCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenMoveKeyDialogCommand))]
    private string? _selectedKeyFullPath;

    public bool HasSelectedKey => !string.IsNullOrEmpty(SelectedKeyFullPath);

    public bool CanMoveKey => HasSelectedKey && LoadedGroups.Count > 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRevertKeyChanges))]
    [NotifyCanExecuteChangedFor(nameof(RevertKeyChangesCommand))]
    private bool _isKeyModified;

    public bool CanRevertKeyChanges => HasSelectedKey && IsKeyModified;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SyncSelectedKeyAcrossLanguagesCommand))]
    private bool _canSyncSelectedKey;

    [ObservableProperty]
    private ObservableCollection<KeyTranslationEntryViewModel> _currentKeyTranslations = new();

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private KeyFilterMode _currentFilterMode = KeyFilterMode.All;

    [ObservableProperty]
    private string _newKeyPath = string.Empty;

    [ObservableProperty]
    private string _newKeyDefaultSource = string.Empty;

    [ObservableProperty]
    private string _renameKeyNewName = string.Empty;

    [ObservableProperty]
    private LoadedTranslationGroup? _targetMoveGroup;

    [ObservableProperty]
    private string _statusText = "Ready";

    public void LoadTranslationGroup(LoadedTranslationGroup group)
    {
        LoadTranslationGroups(new[] { group });
    }

    public void LoadTranslationGroups(IEnumerable<LoadedTranslationGroup> groups)
    {
        LoadedGroups.Clear();
        foreach (var grp in groups)
        {
            LoadedGroups.Add(grp);
        }

        SelectedGroup = LoadedGroups.FirstOrDefault();
        TargetMoveGroup = LoadedGroups.Skip(1).FirstOrDefault() ?? SelectedGroup;

        var allDocs = LoadedGroups.SelectMany(g => g.Files)
            .Select(f => (f.Document, f.FileName, f.LanguageLabel))
            .ToList();

        SetDocuments(allDocs);
        StatusText = $"Loaded {LoadedGroups.Count} group(s) with {allDocs.Count} total files.";
    }

    public void Clear()
    {
        LoadedGroups.Clear();
        SelectedGroup = null;
        TargetMoveGroup = null;
        _documents.Clear();
        _rawHierarchyNodes.Clear();
        RootNodes.Clear();
        CurrentKeyTranslations.Clear();
        SelectedNode = null;
        SelectedKeyFullPath = null;
        RenameKeyNewName = string.Empty;
        UpdateKeyModifiedState();
        StatusText = "No translation files loaded. Open a file to begin.";
    }

    public void SetDocuments(IEnumerable<(ILingoDocument Document, string Name, string Language)> documents)
    {
        _documents.Clear();
        _documents.AddRange(documents);
        RebuildHierarchy();
    }

    public void RebuildHierarchy()
    {
        var allDocs = GetAllDocuments().ToList();
        if (allDocs.Count == 0)
        {
            _rawHierarchyNodes.Clear();
            RootNodes.Clear();
            StatusText = "No translation files loaded. Open a file to begin.";
            return;
        }

        _rawHierarchyNodes = _hierarchyBuilder.Build(allDocs);
        ApplyHierarchyFilter();
    }

    private IEnumerable<ILingoDocument> GetAllDocuments()
    {
        if (LoadedGroups.Count > 0)
        {
            return LoadedGroups.SelectMany(g => g.Files).Select(f => f.Document);
        }
        return _documents.Select(d => d.Document);
    }

    public void ApplyHierarchyFilter()
    {
        RootNodes.Clear();
        if (_rawHierarchyNodes.Count == 0) return;

        var criteria = new KeyQueryCriteria
        {
            FilterMode = CurrentFilterMode,
            SearchText = string.IsNullOrWhiteSpace(SearchFilter) ? null : SearchFilter.Trim(),
            IncludeMissingKeys = true,
            IncludeEmptyTargets = true
        };

        var allDocs = GetAllDocuments().ToList();
        var filteredNodes = _queryService.FilterHierarchy(_rawHierarchyNodes, criteria, allDocs);

        foreach (var node in filteredNodes)
        {
            var nodeVm = KeyTreeNodeViewModel.FromKeyNode(node);
            if (!string.IsNullOrWhiteSpace(SearchFilter) || CurrentFilterMode != KeyFilterMode.All)
            {
                nodeVm.ExpandAll();
            }
            RootNodes.Add(nodeVm);
        }

        var filterLabel = CurrentFilterMode switch
        {
            KeyFilterMode.UntranslatedOnly => "Untranslated",
            KeyFilterMode.OutdatedOnly => "Outdated",
            KeyFilterMode.MissingOnly => "Missing",
            KeyFilterMode.TranslatedOnly => "Translated",
            _ => "All"
        };

        StatusText = $"Filter: {filterLabel} | Showing {RootNodes.Count} groups across {allDocs.Count} documents.";
    }

    partial void OnSearchFilterChanged(string value)
    {
        ApplyHierarchyFilter();
    }

    partial void OnCurrentFilterModeChanged(KeyFilterMode value)
    {
        ApplyHierarchyFilter();
    }

    [RelayCommand]
    public void SetFilter(string modeName)
    {
        CurrentFilterMode = modeName switch
        {
            "Untranslated" => KeyFilterMode.UntranslatedOnly,
            "Outdated" => KeyFilterMode.OutdatedOnly,
            "Missing" => KeyFilterMode.MissingOnly,
            "Translated" => KeyFilterMode.TranslatedOnly,
            _ => KeyFilterMode.All
        };
    }

    partial void OnSelectedNodeChanged(KeyTreeNodeViewModel? value)
    {
        CurrentKeyTranslations.Clear();
        if (value == null)
        {
            SelectedKeyFullPath = null;
            RenameKeyNewName = string.Empty;
            return;
        }

        SelectedKeyFullPath = value.FullPath;
        RenameKeyNewName = value.FullPath;

        if (value.IsLeaf)
        {
            LoadTranslationsForKey(value.FullPath);
        }
    }

    private bool _isUpdatingSourceText;

    private void LoadTranslationsForKey(string key)
    {
        CurrentKeyTranslations.Clear();

        // 1. If we have loaded groups, find which group(s) contain this key
        if (LoadedGroups.Count > 0)
        {
            var matchingGroups = LoadedGroups
                .Where(g => g.Files.Any(f => f.Document.GetUnit(key) != null))
                .ToList();

            if (matchingGroups.Count == 0)
            {
                matchingGroups = new List<LoadedTranslationGroup> { SelectedGroup ?? LoadedGroups.First() };
            }

            foreach (var grp in matchingGroups)
            {
                // Discover primary source text for this group
                var sourceFile = grp.SourceFile;
                var primarySource = sourceFile?.Document.GetUnit(key)?.Source
                                    ?? grp.Files.Select(f => f.Document.GetUnit(key)?.Source).FirstOrDefault(s => !string.IsNullOrEmpty(s));

                foreach (var file in grp.Files)
                {
                    var entry = new KeyTranslationEntryViewModel(file, key, grp.GroupStub);
                    if (entry.IsMissing && !string.IsNullOrEmpty(primarySource))
                    {
                        entry.SourceText = primarySource;
                        entry.IsModified = false;
                    }

                    entry.OnEntryModifiedChanged = UpdateKeyModifiedState;

                    // Real-time sibling-isolated source text sync
                    entry.OnSourceTextEdited = newSource =>
                    {
                        if (_isUpdatingSourceText) return;
                        _isUpdatingSourceText = true;
                        try
                        {
                            foreach (var other in CurrentKeyTranslations)
                            {
                                if (other != entry && other.GroupStub == entry.GroupStub && other.SourceText != newSource)
                                {
                                    other.SourceText = newSource;
                                }
                            }
                        }
                        finally
                        {
                            _isUpdatingSourceText = false;
                        }
                    };

                    CurrentKeyTranslations.Add(entry);
                }
            }
        }
        else
        {
            // Fallback for document-only collections (e.g. in tests)
            string? primarySource = null;
            foreach (var (doc, _, _) in _documents)
            {
                var u = doc.GetUnit(key);
                if (!string.IsNullOrEmpty(u?.Source))
                {
                    primarySource = u.Source;
                    break;
                }
            }

            foreach (var (doc, name, lang) in _documents)
            {
                var entry = new KeyTranslationEntryViewModel(doc, name, key, lang);
                if (entry.IsMissing && !string.IsNullOrEmpty(primarySource))
                {
                    entry.SourceText = primarySource;
                    entry.IsModified = false;
                }

                entry.OnEntryModifiedChanged = UpdateKeyModifiedState;

                entry.OnSourceTextEdited = newSource =>
                {
                    if (_isUpdatingSourceText) return;
                    _isUpdatingSourceText = true;
                    try
                    {
                        foreach (var other in CurrentKeyTranslations)
                        {
                            if (other != entry && other.SourceText != newSource)
                            {
                                other.SourceText = newSource;
                            }
                        }
                    }
                    finally
                    {
                        _isUpdatingSourceText = false;
                    }
                };

                CurrentKeyTranslations.Add(entry);
            }
        }

        UpdateKeyModifiedState();
    }

    public void UpdateKeyModifiedState()
    {
        IsKeyModified = CurrentKeyTranslations.Any(t => t.IsModified);
        UpdateSyncState();
    }

    public void UpdateSyncState()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath))
        {
            CanSyncSelectedKey = false;
            return;
        }

        if (LoadedGroups.Count > 0)
        {
            var matchingGroup = LoadedGroups.FirstOrDefault(g => g.Files.Any(f => f.Document.GetUnit(SelectedKeyFullPath) != null))
                                ?? SelectedGroup ?? LoadedGroups.First();

            var sourceDoc = matchingGroup.SourceFile?.Document;
            var targetDocs = matchingGroup.Files.Where(f => f != matchingGroup.SourceFile).Select(f => f.Document).ToList();

            CanSyncSelectedKey = _keySynchronizer.HasKeyDifferencesInSiblingGroup(SelectedKeyFullPath, sourceDoc, targetDocs);
        }
        else if (_documents.Count > 0)
        {
            var sourceDoc = _documents.FirstOrDefault().Document;
            var targetDocs = _documents.Skip(1).Select(d => d.Document).ToList();
            CanSyncSelectedKey = _keySynchronizer.HasKeyDifferencesInSiblingGroup(SelectedKeyFullPath, sourceDoc, targetDocs);
        }
        else
        {
            CanSyncSelectedKey = false;
        }
    }

    [RelayCommand]
    public void AddNewKey()
    {
        if (string.IsNullOrWhiteSpace(NewKeyPath))
        {
            StatusText = "Error: Key path cannot be empty.";
            return;
        }

        var key = NewKeyPath.Trim();
        var sourceText = string.IsNullOrWhiteSpace(NewKeyDefaultSource) ? key : NewKeyDefaultSource.Trim();

        if (LoadedGroups.Count > 0)
        {
            var targetGroup = SelectedGroup ?? LoadedGroups.First();
            var sourceDoc = targetGroup.SourceFile?.Document;
            var targetDocs = targetGroup.Files.Where(f => f != targetGroup.SourceFile).Select(f => f.Document).ToList();

            var updated = _keySynchronizer.AddKeyToSiblingGroup(key, sourceText, sourceDoc, targetDocs);
            foreach (var f in targetGroup.Files)
            {
                if (updated.Contains(f.Document))
                {
                    f.IsModified = true;
                }
            }
            StatusText = $"Key '{key}' added to group '{targetGroup.GroupStub}' across {targetGroup.Files.Count} files.";
        }
        else if (_documents.Count > 0)
        {
            var sourceDoc = _documents[0].Document;
            var targetDocs = _documents.Skip(1).Select(d => d.Document).ToList();
            var updated = _keySynchronizer.AddKeyToSiblingGroup(key, sourceText, sourceDoc, targetDocs);
            StatusText = $"Key '{key}' added and synchronized across {_documents.Count} languages.";
        }

        NewKeyPath = string.Empty;
        NewKeyDefaultSource = string.Empty;

        RebuildHierarchy();
        SelectKeyByPath(key);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedKey))]
    public void DeleteSelectedKey()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath)) return;
        DeleteKey(SelectedKeyFullPath);
    }

    public void DeleteKey(string keyPath)
    {
        if (string.IsNullOrEmpty(keyPath)) return;

        var deletedFromCount = 0;
        if (LoadedGroups.Count > 0)
        {
            foreach (var grp in LoadedGroups)
            {
                foreach (var file in grp.Files)
                {
                    if (file.Document.RemoveUnit(keyPath))
                    {
                        file.IsModified = true;
                        deletedFromCount++;
                    }
                }
            }
        }
        else
        {
            foreach (var (doc, _, _) in _documents)
            {
                if (doc.RemoveUnit(keyPath))
                {
                    deletedFromCount++;
                }
            }
        }

        SelectedKeyFullPath = null;
        SelectedNode = null;
        CurrentKeyTranslations.Clear();
        UpdateKeyModifiedState();
        RebuildHierarchy();

        StatusText = $"Key '{keyPath}' deleted from {deletedFromCount} file(s).";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedKey))]
    public async Task OpenRenameKeyDialogAsync()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath)) return;

        if (_dialogService != null)
        {
            var newKey = await _dialogService.ShowRenameKeyDialogAsync(SelectedKeyFullPath);
            if (!string.IsNullOrWhiteSpace(newKey) && !string.Equals(SelectedKeyFullPath, newKey, StringComparison.Ordinal))
            {
                RenameKey(SelectedKeyFullPath, newKey.Trim());
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(RenameKeyNewName) && !string.Equals(SelectedKeyFullPath, RenameKeyNewName, StringComparison.Ordinal))
            {
                RenameKey(SelectedKeyFullPath, RenameKeyNewName.Trim());
            }
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedKey))]
    public void RenameSelectedKey()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath) || string.IsNullOrWhiteSpace(RenameKeyNewName)) return;
        RenameKey(SelectedKeyFullPath, RenameKeyNewName.Trim());
    }

    public void RenameKey(string oldKeyPath, string newKeyPath)
    {
        if (string.IsNullOrWhiteSpace(newKeyPath) || string.Equals(oldKeyPath, newKeyPath, StringComparison.Ordinal))
        {
            return;
        }

        var renamedCount = 0;
        if (LoadedGroups.Count > 0)
        {
            foreach (var grp in LoadedGroups)
            {
                foreach (var file in grp.Files)
                {
                    if (file.Document.RenameUnit(oldKeyPath, newKeyPath))
                    {
                        file.IsModified = true;
                        renamedCount++;
                    }
                }
            }
        }
        else
        {
            foreach (var (doc, _, _) in _documents)
            {
                if (doc.RenameUnit(oldKeyPath, newKeyPath))
                {
                    renamedCount++;
                }
            }
        }

        RebuildHierarchy();
        SelectKeyByPath(newKeyPath);
        StatusText = $"Renamed key '{oldKeyPath}' to '{newKeyPath}' across {renamedCount} file(s).";
    }

    [RelayCommand(CanExecute = nameof(CanMoveKey))]
    public async Task OpenMoveKeyDialogAsync()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath) || LoadedGroups.Count <= 1) return;

        var currentGroup = LoadedGroups.FirstOrDefault(g => g.Files.Any(f => f.Document.GetUnit(SelectedKeyFullPath) != null));
        var availableGroups = LoadedGroups.Where(g => g != currentGroup).ToList();
        if (availableGroups.Count == 0) return;

        if (_dialogService != null)
        {
            var targetGroup = await _dialogService.ShowMoveKeyDialogAsync(SelectedKeyFullPath, availableGroups, currentGroup);
            if (targetGroup != null)
            {
                MoveKeyToGroup(SelectedKeyFullPath, targetGroup);
            }
        }
        else
        {
            if (TargetMoveGroup != null)
            {
                MoveKeyToGroup(SelectedKeyFullPath, TargetMoveGroup);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveKey))]
    public void MoveSelectedKey()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath) || TargetMoveGroup == null) return;
        MoveKeyToGroup(SelectedKeyFullPath, TargetMoveGroup);
    }

    public void MoveKeyToGroup(string keyPath, LoadedTranslationGroup targetGroup)
    {
        var sourceGroup = LoadedGroups.FirstOrDefault(g => g.Files.Any(f => f.Document.GetUnit(keyPath) != null));
        if (sourceGroup == null || targetGroup == null || sourceGroup == targetGroup)
        {
            return;
        }

        var sourceUnits = sourceGroup.Files
            .Select(f => (File: f, Unit: f.Document.GetUnit(keyPath)))
            .Where(x => x.Unit != null)
            .ToList();

        if (sourceUnits.Count == 0) return;

        var masterSourceUnit = sourceUnits.FirstOrDefault(x => x.File.IsSource).Unit ?? sourceUnits.First().Unit;
        var masterSourceText = masterSourceUnit!.Source ?? keyPath;

        // 1. Sync into targetGroup source file first
        var targetSource = targetGroup.SourceFile;
        if (targetSource != null)
        {
            targetSource.Document.SyncUnit(new Unit
            {
                Id = keyPath,
                Source = masterSourceText,
                Target = targetSource.IsSource ? masterSourceText : (sourceUnits.FirstOrDefault(x => x.File.LanguageLabel == targetSource.LanguageLabel).Unit?.Target ?? masterSourceText),
                State = TranslationState.Translated
            });
            targetSource.IsModified = true;
        }

        // 2. Sync into targetGroup target sibling files
        foreach (var targetFile in targetGroup.Files.Where(f => f != targetSource))
        {
            var matchedUnit = sourceUnits.FirstOrDefault(x => x.File.LanguageLabel == targetFile.LanguageLabel).Unit;
            var targetUnit = new Unit
            {
                Id = keyPath,
                Source = masterSourceText,
                Target = matchedUnit?.Target,
                State = matchedUnit?.State ?? (matchedUnit?.Target != null ? TranslationState.Translated : TranslationState.NeedsTranslation)
            };
            targetFile.Document.SyncUnit(targetUnit);
            targetFile.IsModified = true;
        }

        // 3. Remove key from sourceGroup
        foreach (var file in sourceGroup.Files)
        {
            if (file.Document.RemoveUnit(keyPath))
            {
                file.IsModified = true;
            }
        }

        RebuildHierarchy();
        SelectKeyByPath(keyPath);
        StatusText = $"Moved key '{keyPath}' from '{sourceGroup.GroupStub}' to '{targetGroup.GroupStub}'.";
    }

    [RelayCommand(CanExecute = nameof(CanSyncSelectedKey))]
    public void SyncSelectedKeyAcrossLanguages()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath)) return;

        var keyToSync = SelectedKeyFullPath;
        if (LoadedGroups.Count > 0)
        {
            var matchingGroup = LoadedGroups.FirstOrDefault(g => g.Files.Any(f => f.Document.GetUnit(keyToSync) != null))
                                ?? SelectedGroup ?? LoadedGroups.First();

            var sourceDoc = matchingGroup.SourceFile?.Document;
            var targetDocs = matchingGroup.Files.Where(f => f != matchingGroup.SourceFile).Select(f => f.Document).ToList();

            var updated = _keySynchronizer.SyncKeyInSiblingGroup(keyToSync, sourceDoc, targetDocs);
            foreach (var doc in updated)
            {
                var file = matchingGroup.Files.FirstOrDefault(f => f.Document == doc);
                if (file != null) file.IsModified = true;
            }

            RebuildHierarchy();
            var node = FindNodeByPath(RootNodes, keyToSync);
            if (node == null)
            {
                SelectedKeyFullPath = null;
                SelectedNode = null;
                CurrentKeyTranslations.Clear();
                UpdateKeyModifiedState();
                StatusText = $"Key '{keyToSync}' was missing in the source file and has been deleted from all sibling files.";
            }
            else
            {
                foreach (var tr in CurrentKeyTranslations)
                {
                    tr.LoadData();
                }
                UpdateKeyModifiedState();
                StatusText = $"Synchronized '{keyToSync}' across {matchingGroup.GroupStub} siblings ({updated.Count} files updated).";
            }
        }
        else if (_documents.Count > 0)
        {
            var sourceDoc = _documents.FirstOrDefault().Document;
            var targetDocs = _documents.Skip(1).Select(d => d.Document).ToList();
            var updated = _keySynchronizer.SyncKeyInSiblingGroup(keyToSync, sourceDoc, targetDocs);

            RebuildHierarchy();
            var node = FindNodeByPath(RootNodes, keyToSync);
            if (node == null)
            {
                SelectedKeyFullPath = null;
                SelectedNode = null;
                CurrentKeyTranslations.Clear();
                UpdateKeyModifiedState();
                StatusText = $"Key '{keyToSync}' was missing in the source file and has been deleted from all sibling files.";
            }
            else
            {
                foreach (var tr in CurrentKeyTranslations)
                {
                    tr.LoadData();
                }
                UpdateKeyModifiedState();
                StatusText = $"Synchronized '{keyToSync}' to {updated.Count} missing language files.";
            }
        }
    }

    [RelayCommand]
    public void SyncAllMissingKeys()
    {
        if (LoadedGroups.Count > 0)
        {
            var totalCount = 0;
            foreach (var grp in LoadedGroups)
            {
                var sourceDoc = grp.SourceFile?.Document;
                var targetDocs = grp.Files.Where(f => f != grp.SourceFile).Select(f => f.Document).ToList();
                var synced = _keySynchronizer.SyncMissingKeysInSiblingGroup(sourceDoc, targetDocs);

                foreach (var list in synced.Values)
                {
                    foreach (var doc in list)
                    {
                        var file = grp.Files.FirstOrDefault(f => f.Document == doc);
                        if (file != null) file.IsModified = true;
                    }
                }
                totalCount += synced.Count;
            }

            RebuildHierarchy();
            if (SelectedKeyFullPath != null)
            {
                var node = FindNodeByPath(RootNodes, SelectedKeyFullPath);
                if (node == null)
                {
                    SelectedKeyFullPath = null;
                    SelectedNode = null;
                    CurrentKeyTranslations.Clear();
                    UpdateKeyModifiedState();
                }
                else
                {
                    LoadTranslationsForKey(SelectedKeyFullPath);
                }
            }

            StatusText = $"Batch sync completed: {totalCount} key operations synchronized in sibling groups.";
        }
        else if (_documents.Count > 0)
        {
            var sourceDoc = _documents.FirstOrDefault().Document;
            var targetDocs = _documents.Skip(1).Select(d => d.Document).ToList();
            var synced = _keySynchronizer.SyncMissingKeysInSiblingGroup(sourceDoc, targetDocs);

            RebuildHierarchy();
            if (SelectedKeyFullPath != null)
            {
                var node = FindNodeByPath(RootNodes, SelectedKeyFullPath);
                if (node == null)
                {
                    SelectedKeyFullPath = null;
                    SelectedNode = null;
                    CurrentKeyTranslations.Clear();
                    UpdateKeyModifiedState();
                }
                else
                {
                    LoadTranslationsForKey(SelectedKeyFullPath);
                }
            }

            StatusText = $"Batch sync completed: {synced.Count} key operations synchronized.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanRevertKeyChanges))]
    public void RevertKeyChanges()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath)) return;

        LoadTranslationsForKey(SelectedKeyFullPath);
        StatusText = $"Reverted changes for key '{SelectedKeyFullPath}'.";
    }

    [RelayCommand]
    public void SaveAllTranslations()
    {
        foreach (var tr in CurrentKeyTranslations)
        {
            if (tr.IsModified || tr.IsMissing)
            {
                tr.SaveChanges();
            }
        }
        UpdateKeyModifiedState();
        StatusText = $"All translations saved for '{SelectedKeyFullPath}'.";
    }

    public void SelectKeyByPath(string path)
    {
        var found = FindNodeByPath(RootNodes, path);
        if (found != null)
        {
            SelectedNode = found;
        }
    }

    private KeyTreeNodeViewModel? FindNodeByPath(IEnumerable<KeyTreeNodeViewModel> nodes, string path)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.FullPath, path, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var childFound = FindNodeByPath(node.Children, path);
            if (childFound != null) return childFound;
        }
        return null;
    }
}
