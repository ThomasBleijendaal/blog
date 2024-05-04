using Markdig;
using Markdown.ColorCode;
using Rendering.Asides;
using Rendering.Languages;
using StringBuilder = System.Text.StringBuilder;

namespace Rendering;

public static class MarkdownHelper
{
    public static MarkdownPipeline GetPipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseDefinitionLists()
            .UseColorCode(
                htmlFormatterType: HtmlFormatterType.Css,
                additionalLanguages: [new Ascii(), new Bicep()])
            .Use<AsideContainerExtension>();

        return builder.Build();
    }

    public static string InlineReferences(string rootDirectory, IEnumerable<string> lines)
    {
        var stringBuilder = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("{!") && line.EndsWith("!}"))
            {
                var path = line[2..^2].Replace('/', Path.DirectorySeparatorChar);

                var filePath = Path.Combine(rootDirectory, path);
                var extension = Path.GetExtension(filePath);

                var fileContents = File.ReadAllLines(filePath);

                stringBuilder.AppendLine($"```{extension[1..]}");
                foreach (var fileLine in fileContents)
                {
                    stringBuilder.AppendLine(fileLine);
                }
                stringBuilder.AppendLine("```");
            }
            else
            {
                stringBuilder.AppendLine(line);
            }
        }

        return stringBuilder.ToString();
    }
}
