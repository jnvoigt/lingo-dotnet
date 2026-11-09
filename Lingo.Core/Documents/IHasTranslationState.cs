namespace Lingo.Core.Documents;

public interface IHasTranslationState
{
    TranslationState GetTargetState(string unitId);
    void SetTargetState(string unitId, TranslationState state);
}
