using System.Xml.Linq;

namespace NGraphics.Custom.Parsers
{
  public interface IValuesParser
  {
    double ReadNumber(XAttribute a);
    double ReadNumber(string raw);
    double? ReadOptionalNumber(XAttribute a);
  }
}