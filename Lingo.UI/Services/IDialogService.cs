using System.Collections.Generic;
using System.Threading.Tasks;
using Lingo.Core.Files;

namespace Lingo.UI.Services;

public record AddKeyDialogResult(string KeyPath, string DefaultSourceText, LoadedTranslationGroup? TargetGroup);

public interface IDialogService
{
    Task<string?> ShowRenameKeyDialogAsync(string currentKey);
    Task<LoadedTranslationGroup?> ShowMoveKeyDialogAsync(string currentKey, IReadOnlyList<LoadedTranslationGroup> availableGroups, LoadedTranslationGroup? currentGroup);
    Task<AddKeyDialogResult?> ShowAddKeyDialogAsync(IReadOnlyList<LoadedTranslationGroup> availableGroups, LoadedTranslationGroup? defaultGroup);
}
