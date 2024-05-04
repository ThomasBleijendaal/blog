using Markdig;
using Markdig.Renderers;

namespace Rendering.Asides;

internal class AsideContainerExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.BlockParsers.Insert(0, new AsideContainerParser());
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            htmlRenderer.ObjectRenderers.Insert(0, new AsideContainerRenderer());
        }
    }
}
