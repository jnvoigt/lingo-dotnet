using System.Collections.Generic;
using System.Threading.Tasks;
using Lingo.Core.Files;

namespace Lingo.UI.Services;

public interface IDialogService
{
    Task<string?> ShowRenameKeyDialogAsync(string currentKey);
    Task<LoadedTranslationGroup?> ShowMoveKeyDialogAsync(string currentKey, IReadOnlyList<LoadedTranslationGroup> availableGroups, LoadedTranslationGroup? currentGroup);
}
