using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lingo.UI.Services;

public interface IFileDialogService
{
    Task<string?> OpenFileDialogAsync();
    Task<IReadOnlyList<string>> OpenFilesDialogAsync();
}
