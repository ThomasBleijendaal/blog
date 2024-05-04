using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Rendering;

public static class YamlHelper
{
    public static IDeserializer GetYamlDeserializer()
    {
        var builder = new DeserializerBuilder()
            .WithNamingConvention(LowerCaseNamingConvention.Instance);

        return builder.Build();
    }
}
