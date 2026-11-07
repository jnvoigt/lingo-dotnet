using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Lingo.UI.Services;

public class AvaloniaFileDialogService : IFileDialogService
{
    private readonly Func<Window?> _windowProvider;

    public AvaloniaFileDialogService(Func<Window?> windowProvider)
    {
        _windowProvider = windowProvider;
    }

    public async Task<string?> OpenFileDialogAsync()
    {
        var files = await OpenFilesDialogAsync(allowMultiple: false);
        return files.Count > 0 ? files[0] : null;
    }

    public Task<IReadOnlyList<string>> OpenFilesDialogAsync()
    {
        return OpenFilesDialogAsync(allowMultiple: true);
    }

    private async Task<IReadOnlyList<string>> OpenFilesDialogAsync(bool allowMultiple)
    {
        var window = _windowProvider();
        if (window == null) return Array.Empty<string>();

        var storageProvider = TopLevel.GetTopLevel(window)?.StorageProvider;
        if (storageProvider == null) return Array.Empty<string>();

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = allowMultiple ? "Open Translation Files" : "Open Translation File",
            AllowMultiple = allowMultiple,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Translation Files (*.xlf, *.xliff, *.xml)")
                {
                    Patterns = new[] { "*.xlf", "*.xliff", "*.xml" }
                },
                new FilePickerFileType("All Files (*.*)")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        return files.Select(f => f.Path.LocalPath).ToList();
    }
}
