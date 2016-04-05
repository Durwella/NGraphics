using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Linq;
using NGraphics.Custom.Codes;
using NGraphics.Custom.Parsers;
using NGraphics.Custom.Models.Elements;
using System.Xml;
using NGraphics.Custom.Models;

namespace NGraphics.Custom
{
	public class TextParser
	{
		static Regex keyValueRe = new Regex (@"\s*([\w-]+)\s*:\s*(.*)");
		static Dictionary<string, string> ParseStyle(string style)
		{
			var d = new Dictionary<string, string> ();
			var kvs = style.Split (new[]{ ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var kv in kvs) {
				var m = keyValueRe.Match (kv);
				if (m.Success) {
					var k = m.Groups [1].Value;
					var v = m.Groups [2].Value;
					d [k] = v;
				}
			}
			return d;
		}

		static string GetString (Dictionary<string, string> style, string name, string defaultValue = "")
		{
			string v;
			if (style.TryGetValue (name, out v))
				return v;
			return defaultValue;
		}


		static string ReadTextFontAttr (XElement element, string attr)
		{
			string value = null;
			if (element != null)
			{
				var attrib = element.Attribute(attr);
				if (attrib != null && !string.IsNullOrWhiteSpace(attrib.Value))
					value = attrib.Value.Trim();
				else
				{
					var style = element.Attribute("style");
					if (style != null && !string.IsNullOrWhiteSpace(style.Value))
					{
						value = GetString(ParseStyle(style.Value), attr);
					}
				}
			}
			return value;

		}

		public static string ReadTextFontFamily (XElement element)
		{
			return ReadTextFontAttr (element, "font-family");
		}

		public static string ReadTextFontWeight (XElement element)
		{
			return ReadTextFontAttr (element, "font-weight");
		}

		public static string ReadTextFontStyle (XElement element)
		{
			return ReadTextFontAttr (element, "font-style");
		}

		public static double ReadTextFontSize(XElement element)
		{
			double value = -1;
			if (element != null)
			{
				var attrib = element.Attribute("font-size");
				if (attrib != null && !string.IsNullOrWhiteSpace(attrib.Value))
					value = new ValuesParser().ReadNumber(attrib.Value);
				else
				{
					var style = element.Attribute("style");
					if (style != null && !string.IsNullOrWhiteSpace(style.Value))
					{
						value = new ValuesParser().ReadNumber(GetString(ParseStyle(style.Value), "font-size", "-1"));
					}
				}
			}

			return value;
		}

		public static TextAlignment ReadTextAlignment(XElement element)
		{
			string value = null;
			if (element != null) {
				var attrib = element.Attribute ("text-anchor");
				if (attrib != null && !string.IsNullOrWhiteSpace (attrib.Value))
					value = attrib.Value;
				else {
					var style = element.Attribute ("style");
					if (style != null && !string.IsNullOrWhiteSpace (style.Value)) {
						value = GetString (ParseStyle (style.Value), "text-anchor");
					}
				}
			}

			switch (value) {
			case "end":
				return TextAlignment.Right;
			case "middle":
				return TextAlignment.Center;
			default:
				return TextAlignment.Left;
			}
		}

		public static void ReadTextSpans (Text txt, XElement e)
		{
			foreach (XNode c in e.Nodes ()) {
				if (c.NodeType == XmlNodeType.Text) {
					txt.Spans.Add (new TextSpan (((XText)c).Value));
				} else if (c.NodeType == XmlNodeType.Element) {
					var ce = (XElement)c;
					if (ce.Name.LocalName == "tspan") {
						var tspan = new TextSpan (ce.Value);
						var valuesParser = new ValuesParser ();
						var x = valuesParser.ReadOptionalNumber (ce.Attribute ("x"));
						var y = valuesParser.ReadOptionalNumber (ce.Attribute ("y"));
						if (x.HasValue && y.HasValue) {
							tspan.Position = new Point (x.Value, y.Value);
						}

						var font = txt.Font;

						var ffamily = ReadTextFontFamily (ce);
						ffamily = string.IsNullOrWhiteSpace (ffamily) ? ReadTextFontFamily (e) : ffamily;
						if (!string.IsNullOrWhiteSpace (ffamily)) {
							font = font.WithFamily (ffamily);
						}
						var fweight = ReadTextFontWeight (ce);
						fweight = string.IsNullOrWhiteSpace (fweight) ? ReadTextFontWeight (e) : fweight;
						if (!string.IsNullOrWhiteSpace (fweight)) {
							font = font.WithWeight (fweight);
						}
						var fstyle = ReadTextFontStyle (ce);
						fstyle = string.IsNullOrWhiteSpace (fstyle) ? ReadTextFontStyle (e) : fstyle;
						if (!string.IsNullOrWhiteSpace (fstyle)) {
							font = font.WithStyle (fstyle);
						}
						var fsize = ReadTextFontSize (ce);
						fsize = fsize <= 0 ? ReadTextFontSize (e) : fsize;
						if (fsize > 0) {
							font = font.WithSize (fsize);
						}

						if (font != txt.Font) {
							tspan.Font = font;
						}

						txt.Spans.Add (tspan);
					}
				}
			}
			txt.Trim ();
		}
	}
}
