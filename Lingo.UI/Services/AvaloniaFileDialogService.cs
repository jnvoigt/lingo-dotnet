using System;
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
        var window = _windowProvider();
        if (window == null) return null;

        var storageProvider = TopLevel.GetTopLevel(window)?.StorageProvider;
        if (storageProvider == null) return null;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Translation File",
            AllowMultiple = false,
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

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}
