using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Rendering.Asides;

internal class AsideContainerRenderer : HtmlObjectRenderer<AsideContainer>
{
    protected override void Write(HtmlRenderer renderer, AsideContainer obj)
    {
        renderer.EnsureLine();
        renderer.Write("<aside").WriteAttributes(obj).WriteLine('>');

        var @class = obj.GetAttributes().Classes?.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(@class))
        {
            renderer.WriteLine($"<header>{@class}</header>");
        }

        renderer.WriteChildren(obj);

        renderer.WriteLine("</aside>");
    }
}
