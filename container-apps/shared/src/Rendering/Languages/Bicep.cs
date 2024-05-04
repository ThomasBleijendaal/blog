using ColorCode;
using ColorCode.Common;

namespace Rendering.Languages;

/// <summary>
/// Rules borrowed from https://github.com/PrismJS/prism/blob/master/components/prism-bicep.js
/// </summary>
internal class Bicep : ILanguage
{
    public string Id => "bicep";

    public string Name => "bicep";

    public string CssClassName => "bicep";

    public string? FirstLinePattern => null;

    public IList<LanguageRule> Rules => [
        new LanguageRule(@"\b(metadata|targetScope|resource|module|param|var|output|for|in|if|existing|import|as|type|with|using|func|assert|provider|true|false)\b", new Dictionary<int, string>() { { 0, ScopeName.Keyword } }),
        new LanguageRule(@"(^|[^\\])\/\*[\s\S]*?(?:\*\/|$)", new Dictionary<int, string>() { { 0, ScopeName.Comment } }),
        new LanguageRule(@"(^|[^\\:])\/\/.*", new Dictionary<int, string>() { { 0, ScopeName.Comment } }),
        new LanguageRule(@"'''[^'][\s\S]*?'''", new Dictionary<int, string>() { { 0, ScopeName.String } }),
        new LanguageRule(@"(^|[^\\'])'(?:\\.|\$(?!\{)|[^'\\\r\n$])*'", new Dictionary<int, string>() { { 0, ScopeName.String } }),
        new LanguageRule(@"(^|[^\\'])'(?:\\.|\$(?:(?!\{)|\{[^{}\r\n]*\})|[^'\\\r\n$])*'", new Dictionary<int, string>() { { 0, ScopeName.String } }),
        new LanguageRule(@"(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:E[+-]?\d+)?", new Dictionary<int, string>() { { 0, ScopeName.Number } })
        ];

    public bool HasAlias(string lang)
        => lang.Equals("bicep", StringComparison.InvariantCultureIgnoreCase);

    public override string ToString() => Name;
}
