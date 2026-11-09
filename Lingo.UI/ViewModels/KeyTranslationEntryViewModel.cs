using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lingo.Core.Documents;
using Lingo.Core.Files;
using Lingo.Core.Models;

namespace Lingo.UI.ViewModels;

public partial class KeyTranslationEntryViewModel : ViewModelBase
{
    private readonly ILingoDocument _document;
    private readonly string _keyId;
    private bool _isLoading;

    [ObservableProperty]
    private string _languageCode = string.Empty;

    [ObservableProperty]
    private string _documentName = string.Empty;

    [ObservableProperty]
    private string? _groupStub;

    [ObservableProperty]
    private string? _sourceText;

    [ObservableProperty]
    private string? _targetText;

    [ObservableProperty]
    private TranslationState _state;

    [ObservableProperty]
    private bool _isMissing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RevertChangesCommand))]
    private bool _isModified;

    public bool IsSource { get; set; } = true;
    public LoadedTranslationFile? File { get; set; }
    public Action<string?>? OnSourceTextEdited { get; set; }
    public Action? OnEntryModifiedChanged { get; set; }

    public KeyTranslationEntryViewModel(ILingoDocument document, string documentName, string keyId, string languageCode, bool isSource = true)
    {
        _document = document;
        _documentName = documentName;
        _keyId = keyId;
        _languageCode = languageCode;
        IsSource = isSource;

        LoadData();
    }

    public KeyTranslationEntryViewModel(LoadedTranslationFile file, string keyId, string? groupStub = null)
        : this(file.Document, file.FileName, keyId, file.LanguageLabel, file.IsSource)
    {
        File = file;
        GroupStub = groupStub;
    }

    public ILingoDocument Document => _document;
    public string KeyId => _keyId;

    public void LoadData()
    {
        _isLoading = true;
        try
        {
            var unit = _document.GetUnit(_keyId);
            if (unit == null)
            {
                IsMissing = true;
                SourceText = string.Empty;
                TargetText = string.Empty;
                State = TranslationState.NeedsTranslation;
            }
            else
            {
                IsMissing = false;
                SourceText = unit.Source;
                TargetText = unit.Target ?? _document.GetValue(_keyId);
                State = unit.State ?? TranslationState.None;
            }
            IsModified = false;
        }
        finally
        {
            _isLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsModified))]
    public void RevertChanges()
    {
        LoadData();
    }

    [RelayCommand]
    public void MarkAsValidated()
    {
        State = TranslationState.None;
        IsModified = true;
    }

    partial void OnIsModifiedChanged(bool value)
    {
        OnEntryModifiedChanged?.Invoke();
    }

    partial void OnSourceTextChanged(string? value)
    {
        if (_isLoading) return;
        IsModified = true;
        OnSourceTextEdited?.Invoke(value);
    }

    partial void OnTargetTextChanged(string? value)
    {
        if (_isLoading) return;
        IsModified = true;
        if (string.IsNullOrWhiteSpace(value))
        {
            State = TranslationState.NeedsTranslation;
        }
        else
        {
            State = TranslationState.NeedsReview;
        }
    }

    public void SaveChanges()
    {
        if (IsMissing)
        {
            _document.SyncUnit(new Unit
            {
                Id = _keyId,
                Source = SourceText,
                Target = TargetText,
                State = State
            });
            IsMissing = false;
        }
        else
        {
            // Only update master Source in document if this file represents the source language.
            // Target documents obtain updated source text exclusively via synchronization, which triggers state adaptation.
            if (IsSource)
            {
                if (_document is IHasSourceValue sourceDoc && SourceText != null)
                {
                    sourceDoc.SetSourceValue(_keyId, SourceText);
                }
                else
                {
                    var unit = _document.GetUnit(_keyId);
                    if (unit != null && unit.Source != SourceText && SourceText != null)
                    {
                        unit.Source = SourceText;
                        _document.SyncUnit(unit);
                    }
                }
            }

            _document.SetValue(_keyId, TargetText ?? string.Empty);
            if (_document is IHasTranslationState stateDoc)
            {
                stateDoc.SetTargetState(_keyId, State);
            }
        }
        if (File != null)
        {
            File.IsModified = true;
        }
        IsModified = false;
    }
}
