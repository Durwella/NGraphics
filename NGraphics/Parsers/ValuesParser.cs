using System;
using System.Globalization;
using System.Xml.Linq;

namespace NGraphics.Custom.Parsers
{
  public class ValuesParser : IValuesParser
  {
    private readonly IFormatProvider icult = CultureInfo.InvariantCulture;

    private SvgReader _svgReader;

    public double ReadNumber(XAttribute a)
    {
      if (a == null)
        return 0;
      return ReadNumber(a.Value);
    }

    public double ReadNumber(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return 0;

      var s = raw.Trim();
      var m = 1.0;

      if (s.EndsWith("px"))
      {
        s = s.Substring(0, s.Length - 2);
      }
      else if (s.EndsWith("%"))
      {
        s = s.Substring(0, s.Length - 1);
        m = 0.01;
      }

      double v;
      if (!double.TryParse(s, NumberStyles.Float, icult, out v))
      {
        v = 0;
      }
      return m*v;
    }

	public double? ReadOptionalNumber (XAttribute a)
	{
		if (a == null)
			return null;
		return ReadNumber (a.Value.Split(' ')[0]);
	}
  }
}