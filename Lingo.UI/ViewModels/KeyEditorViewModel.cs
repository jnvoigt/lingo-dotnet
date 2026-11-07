using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingo.Core.Documents;
using Lingo.Core.Files;
using Lingo.Core.Hierarchy;
using Lingo.Core.Models;
using Lingo.Core.Query;
using Lingo.Core.Sync;

namespace Lingo.UI.ViewModels;

public partial class KeyEditorViewModel : ViewModelBase
{
    private readonly KeyHierarchyBuilder _hierarchyBuilder = new('.');
    private readonly KeySynchronizer _keySynchronizer = new();
    private readonly KeyQueryService _queryService = new();
    private readonly List<(ILingoDocument Document, string Name, string Language)> _documents = new();
    private List<KeyNode> _rawHierarchyNodes = new();
    private LoadedTranslationGroup? _loadedGroup;

    public LoadedTranslationGroup? LoadedGroup => _loadedGroup;

    [ObservableProperty]
    private ObservableCollection<KeyTreeNodeViewModel> _rootNodes = new();

    [ObservableProperty]
    private KeyTreeNodeViewModel? _selectedNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedKey))]
    [NotifyPropertyChangedFor(nameof(CanRevertKeyChanges))]
    [NotifyCanExecuteChangedFor(nameof(RevertKeyChangesCommand))]
    private string? _selectedKeyFullPath;

    public bool HasSelectedKey => !string.IsNullOrEmpty(SelectedKeyFullPath);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRevertKeyChanges))]
    [NotifyCanExecuteChangedFor(nameof(RevertKeyChangesCommand))]
    private bool _isKeyModified;

    public bool CanRevertKeyChanges => HasSelectedKey && IsKeyModified;

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
    private string _statusText = "Ready";

    public void LoadTranslationGroup(LoadedTranslationGroup group)
    {
        _loadedGroup = group;
        var docs = group.Files.Select(f => (f.Document, f.FileName, f.LanguageLabel)).ToList();
        SetDocuments(docs);
        StatusText = $"Loaded group '{group.GroupStub}' with {group.Files.Count} files.";
    }

    public void Clear()
    {
        _loadedGroup = null;
        _documents.Clear();
        _rawHierarchyNodes.Clear();
        RootNodes.Clear();
        CurrentKeyTranslations.Clear();
        SelectedNode = null;
        SelectedKeyFullPath = null;
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
        if (_documents.Count == 0)
        {
            _rawHierarchyNodes.Clear();
            RootNodes.Clear();
            StatusText = "No translation files loaded. Open a file to begin.";
            return;
        }

        _rawHierarchyNodes = _hierarchyBuilder.Build(_documents.Select(d => d.Document));
        ApplyHierarchyFilter();
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

        var filteredNodes = _queryService.FilterHierarchy(_rawHierarchyNodes, criteria, _documents.Select(d => d.Document));

        foreach (var node in filteredNodes)
        {
            var nodeVm = KeyTreeNodeViewModel.FromKeyNode(node);
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

        StatusText = $"Filter: {filterLabel} | Showing {RootNodes.Count} groups across {_documents.Count} documents.";
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
            return;
        }

        SelectedKeyFullPath = value.FullPath;

        if (value.IsLeaf)
        {
            LoadTranslationsForKey(value.FullPath);
        }
    }

    private bool _isUpdatingSourceText;

    private void LoadTranslationsForKey(string key)
    {
        CurrentKeyTranslations.Clear();

        // Find primary source text from any doc that has it
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

        UpdateKeyModifiedState();
    }

    public void UpdateKeyModifiedState()
    {
        IsKeyModified = CurrentKeyTranslations.Any(t => t.IsModified);
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

        var unit = new Unit
        {
            Id = key,
            Source = sourceText,
            Target = sourceText,
            State = TranslationState.NeedsTranslation
        };

        // Add to first (source) document
        if (_documents.Count > 0)
        {
            _documents[0].Document.SyncUnit(unit);
            _keySynchronizer.SyncKeyToTargets(unit, _documents.Skip(1).Select(d => d.Document));
        }

        NewKeyPath = string.Empty;
        NewKeyDefaultSource = string.Empty;

        RebuildHierarchy();

        // Select the new key
        SelectKeyByPath(key);

        StatusText = $"Key '{key}' added and synchronized across {_documents.Count} languages.";
    }

    [RelayCommand]
    public void SyncSelectedKeyAcrossLanguages()
    {
        if (string.IsNullOrEmpty(SelectedKeyFullPath) || CurrentKeyTranslations.Count == 0) return;

        // Find best source text
        var sourceText = CurrentKeyTranslations.FirstOrDefault(t => !string.IsNullOrEmpty(t.SourceText))?.SourceText ?? SelectedKeyFullPath;
        var unit = new Unit
        {
            Id = SelectedKeyFullPath,
            Source = sourceText,
            State = TranslationState.NeedsTranslation
        };

        var updated = _keySynchronizer.SyncKeyToTargets(unit, _documents.Select(d => d.Document));
        foreach (var tr in CurrentKeyTranslations)
        {
            tr.LoadData();
        }
        UpdateKeyModifiedState();

        StatusText = $"Synchronized '{SelectedKeyFullPath}' to {updated.Count} missing language files.";
    }

    [RelayCommand]
    public void SyncAllMissingKeys()
    {
        if (_documents.Count == 0) return;

        var synced = _keySynchronizer.SyncMissingKeysAcrossDocuments(_documents.Select(d => d.Document));
        RebuildHierarchy();

        if (SelectedKeyFullPath != null)
        {
            LoadTranslationsForKey(SelectedKeyFullPath);
        }

        StatusText = $"Batch sync completed: {synced.Count} missing keys propagated.";
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
