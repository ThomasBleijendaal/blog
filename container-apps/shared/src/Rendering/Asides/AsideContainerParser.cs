using Markdig.Parsers;

namespace Rendering.Asides;

internal class AsideContainerParser : FencedBlockParserBase<AsideContainer>
{
    public AsideContainerParser()
    {
        OpeningCharacters = [':'];
        InfoPrefix = null;
    }

    protected override AsideContainer CreateFencedBlock(BlockProcessor processor)
    {
        return new AsideContainer(this);
    }
}
