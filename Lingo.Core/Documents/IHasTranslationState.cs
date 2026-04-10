namespace Lingo.Core.Documents;

public interface IHasTranslationState
{
    TranslationState GetTargetState(string unitId);
}
