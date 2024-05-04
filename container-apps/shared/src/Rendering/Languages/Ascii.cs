using ColorCode;
using ColorCode.Common;

namespace Rendering.Languages;

internal class Ascii : ILanguage
{
    public string Id => "ascii";

    public string Name => "ascii";

    public string CssClassName => "ascii";

    public string FirstLinePattern => null;

    public IList<LanguageRule> Rules => [
        new LanguageRule(@"([^A-Za-z0-9\s\-_\.,]+)", new Dictionary<int, string>() { { 0, ScopeName.Comment } })
        ];

    public bool HasAlias(string lang)
        => lang.Equals("ascii", StringComparison.InvariantCultureIgnoreCase);
}
