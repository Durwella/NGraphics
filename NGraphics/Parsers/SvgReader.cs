using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NGraphics.Custom.Codes;
using NGraphics.Custom.Interfaces;
using NGraphics.Custom.Models;
using NGraphics.Custom.Models.Brushes;
using NGraphics.Custom.Models.Elements;
using NGraphics.Custom.Models.Transforms;
using Group = NGraphics.Custom.Models.Elements.Group;
using Path = NGraphics.Custom.Models.Elements.Path;

namespace NGraphics.Custom.Parsers
{
  public class SvgReader
  {
    private readonly IStylesParser _stylesParser;
    private readonly IValuesParser _valuesParser;
    //		readonly XNamespace ns;

    public SvgReader(TextReader reader, IStylesParser stylesParser, IValuesParser valuesParser)
    {
      _stylesParser = stylesParser;
      _valuesParser = valuesParser;
      Read(XDocument.Load(reader));
    }

    private static readonly char[] WS = {' ', '\t', '\n', '\r'};
    private readonly Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
    public Graphic Graphic { get; private set; }

    private void Read(XDocument doc)
    {
      var svg = doc.Root;
      var ns = svg.Name.Namespace;

      //
      // Find the defs (gradients)
      //
      foreach (var d in svg.Descendants())
      {
        var idA = d.Attribute("id");
        if (idA != null)
        {
          defs[ReadString(idA).Trim()] = d;
        }
      }

      //
      // Get the dimensions
      //
      var widthA = svg.Attribute("width");
      var heightA = svg.Attribute("height");
      var width = _valuesParser.ReadNumber(widthA);
      var height = _valuesParser.ReadNumber(heightA);
      var size = new Size(width, height);

      var viewBox = new Rect(size);
      var viewBoxA = svg.Attribute("viewBox") ?? svg.Attribute("viewPort");
      if (viewBoxA != null)
      {
        viewBox = ReadRectangle(viewBoxA.Value);
      }

      if (widthA != null && widthA.Value.Contains("%"))
      {
        size.Width *= viewBox.Width;
      }
      if (heightA != null && heightA.Value.Contains("%"))
      {
        size.Height *= viewBox.Height;
      }

      //
      // Add the elements
      //
      Graphic = new Graphic(size, viewBox);

      AddElements(Graphic.Children, svg.Elements(), null, null);
    }

    private void AddElements(IList<IDrawable> list, IEnumerable<XElement> es, Pen inheritPen,
      BaseBrush inheritBaseBrush)
    {
      foreach (var e in es)
        AddElement(list, e, inheritPen, inheritBaseBrush);
    }

    private void AddElement(IList<IDrawable> list, XElement e, Pen inheritPen, BaseBrush inheritBaseBrush)
    {
      Element element = null;

      var styleAttributedDictionary = e.Attributes().ToDictionary(k => k.Name.LocalName, v => v.Value);

      var pen = _stylesParser.GetPen(styleAttributedDictionary);
      var baseBrush = _stylesParser.GetBrush(styleAttributedDictionary,defs, pen);

      var style = ReadString(e.Attribute("style"));
      
      if (!string.IsNullOrWhiteSpace(style))
      {
        ApplyStyle(style, ref pen, ref baseBrush);
      }
      
      pen = pen ?? inheritPen;
      baseBrush = baseBrush ?? inheritBaseBrush;

      //var id = ReadString (e.Attribute ("id"));

      //
      // Elements
      //
      switch (e.Name.LocalName)
      {
        case "text":
        {
          var x = _valuesParser.ReadNumber(e.Attribute("x"));
          var y = _valuesParser.ReadNumber(e.Attribute("y"));
          var text = e.Value.Trim();
          var font = new Font();
		  var fontFamily = TextParser.ReadTextFontFamily(e);
		  if (!string.IsNullOrEmpty(fontFamily))
			font.Family = fontFamily;
		  var fontSize = TextParser.ReadTextFontSize(e);
		  if (fontSize >= 0)
			font.Size = fontSize;
		  var textAlignment = TextParser.ReadTextAlignment(e);

          Text txt;
          if (e.HasElements) {
             txt = new Text (new Rect (new Point (x, y), new Size (double.MaxValue, double.MaxValue)), font,
                textAlignment, pen, baseBrush);
             TextParser.ReadTextSpans (txt, e);
          } else {
             txt = new Text (e.Value.Trim (), new Rect (new Point (x, y), new Size (double.MaxValue, double.MaxValue)), font,
                textAlignment, pen, baseBrush);
          }
          element = txt;
        }
          break;
        case "rect":
        {
          var x = _valuesParser.ReadNumber(e.Attribute("x"));
          var y = _valuesParser.ReadNumber(e.Attribute("y"));
          var width = _valuesParser.ReadNumber(e.Attribute("width"));
          var height = _valuesParser.ReadNumber(e.Attribute("height"));
          element = new Rectangle(new Point(x, y), new Size(width, height), pen, baseBrush);
        }
          break;
        case "ellipse":
        {
          var cx = _valuesParser.ReadNumber(e.Attribute("cx"));
          var cy = _valuesParser.ReadNumber(e.Attribute("cy"));
          var rx = _valuesParser.ReadNumber(e.Attribute("rx"));
          var ry = _valuesParser.ReadNumber(e.Attribute("ry"));
          element = new Ellipse(new Point(cx - rx, cy - ry), new Size(2*rx, 2*ry), pen, baseBrush);
        }
          break;
        case "circle":
        {
          var cx = _valuesParser.ReadNumber(e.Attribute("cx"));
          var cy = _valuesParser.ReadNumber(e.Attribute("cy"));
          var rr = _valuesParser.ReadNumber(e.Attribute("r"));
          element = new Ellipse(new Point(cx - rr, cy - rr), new Size(2*rr, 2*rr), pen, baseBrush);
        }
          break;
        case "path":
        {
          var dA = e.Attribute("d");
          if (dA != null && !string.IsNullOrWhiteSpace(dA.Value))
          {
            var p = new Path(pen, baseBrush);
            SvgPathParser.Parse(p, dA.Value);
            element = p;
          }
        }
          break;
        case "g":
        {
          var g = new Group();
          AddElements(g.Children, e.Elements(), pen, baseBrush);
          element = g;
        }
          break;
        case "use":
        {
          var href = ReadString(e.Attributes().FirstOrDefault(x => x.Name.LocalName == "href"));
          if (!string.IsNullOrWhiteSpace(href))
          {
            XElement useE;
            if (defs.TryGetValue(href.Trim().Replace("#", ""), out useE))
            {
              var useList = new List<IDrawable>();
              AddElement(useList, useE, pen, baseBrush);
              element = useList.OfType<Element>().FirstOrDefault();
            }
          }
        }
          break;
        case "title":
          Graphic.Title = ReadString(e);
          break;
        case "description":
          Graphic.Description = ReadString(e);
          break;
        case "defs":
          // Already read in earlier pass
          break;
        case "namedview":
        case "metadata":
        case "SVGTestCase":
      case "switch":
         break;
        default:
          throw new NotSupportedException("SVG element \"" + e.Name.LocalName + "\" is not supported");
      }

      if (element != null)
      {
        element.Transform = ReadTransform(ReadString(e.Attribute("transform")));
        list.Add(element);
      }
    }

    private void ApplyStyle(string style, ref Pen pen, ref BaseBrush baseBrush)
    {
      var stylesDictionary = _stylesParser.ParseStyleValues(style);
      pen = _stylesParser.GetPen(stylesDictionary);
      baseBrush = _stylesParser.GetBrush(stylesDictionary, defs, pen);
    }

    Transform ReadTransform(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Transform.Identity;

        var s = raw.Trim();

        var calls = s.Split(new[] { ')' }, StringSplitOptions.RemoveEmptyEntries);

        var t = Transform.Identity;

        foreach (var c in calls)
        {
            var args = c.Split(new[] { '(', ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var nt = Transform.Identity;
            switch (args[0])
            {
                case "matrix":
                    if (args.Length == 7)
                    {
                        nt = new Transform(
                            _valuesParser.ReadNumber(args[1]),
                            _valuesParser.ReadNumber(args[2]),
                            _valuesParser.ReadNumber(args[3]),
                            _valuesParser.ReadNumber(args[4]),
                           _valuesParser.ReadNumber(args[5]),
                           _valuesParser.ReadNumber(args[6]));
                    }
                    else
                    {
                        throw new NotSupportedException("Matrices are expected to have 6 elements, this one has " + (args.Length - 1));
                    }
                    break;
                case "translate":
                    if (args.Length >= 3)
                    {
                        nt = Transform.Translate(new Size(_valuesParser.ReadNumber(args[1]), _valuesParser.ReadNumber(args[2])));
                    }
                    else if (args.Length >= 2)
                    {
                        nt = Transform.Translate(new Size(_valuesParser.ReadNumber(args[1]), 0));
                    }
                    break;
                case "scale":
                    if (args.Length >= 3)
                    {
                        nt = Transform.Scale(new Size(_valuesParser.ReadNumber(args[1]), _valuesParser.ReadNumber(args[2])));
                    }
                    else if (args.Length >= 2)
                    {
                        var sx = _valuesParser.ReadNumber(args[1]);
                        nt = Transform.Scale(new Size(sx, sx));
                    }
                    break;
                case "rotate":
                    var a = _valuesParser.ReadNumber(args[1]);
                    if (args.Length >= 4)
                    {
                        var x = _valuesParser.ReadNumber(args[2]);
                        var y = _valuesParser.ReadNumber(args[3]);
                        var t1 = Transform.Translate(new Size(x, y));
                        var t2 = Transform.Rotate(a);
                        var t3 = Transform.Translate(new Size(-x, -y));
                        nt = t1 * t2 * t3;
                    }
                    else
                    {
                        nt = Transform.Rotate(a);
                    }
                    break;
                default:
                    throw new NotSupportedException("Can't transform " + args[0]);
            }
            t = t * nt;
        }

        return t;
    }

    private string ReadString(XElement e, string defaultValue = "")
    {
      if (e == null)
        return defaultValue;
      return e.Value ?? defaultValue;
    }

    private string ReadString(XAttribute a, string defaultValue = "")
    {
      if (a == null)
        return defaultValue;
      return a.Value ?? defaultValue;
    }

    private Rect ReadRectangle(string s)
    {
      var r = new Rect();
      var p = s.Split(WS, StringSplitOptions.RemoveEmptyEntries);
      if (p.Length > 0)
        r.X = _valuesParser.ReadNumber(p[0]);
      if (p.Length > 1)
        r.Y = _valuesParser.ReadNumber(p[1]);
      if (p.Length > 2)
        r.Width = _valuesParser.ReadNumber(p[2]);
      if (p.Length > 3)
        r.Height = _valuesParser.ReadNumber(p[3]);
      return r;
    }
  }
}