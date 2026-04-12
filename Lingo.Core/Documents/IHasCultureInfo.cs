namespace Lingo.Core.Documents;

public interface IHasCultureInfo
{
    CultureInfo GetSourceCulture();
    CultureInfo? GetTargetCulture(); // null = source-only document
}
