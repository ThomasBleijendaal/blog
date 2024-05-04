using System.Text;
using System.Xml;

namespace Rendering;

public class HtmlHelper
{
    public static string PrettyPrint(string xml)
    {
        var result = "";

        using var ms = new MemoryStream();
        using var writer = new XmlTextWriter(ms, Encoding.Unicode);

        var xmlDoc = new XmlDocument();

        try
        {
            xmlDoc.LoadXml(xml);

            writer.Formatting = Formatting.Indented;

            xmlDoc.WriteContentTo(writer);
            writer.Flush();
            ms.Flush();

            ms.Position = 0;

            using var streamReader = new StreamReader(ms);
            result = streamReader.ReadToEnd();
        }
        catch (XmlException ex)
        {
            result = ex.ToString();
        }

        writer.Close();
        ms.Close();

        return result.Replace("<!DOCTYPE html[]>", "<!DOCTYPE html>");
    }
}
