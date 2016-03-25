using NGraphics.Custom.Codes;
using NGraphics.Custom.Interfaces;
using NGraphics.Custom.Models.Brushes;
using System.Collections.Generic;
using System.Linq;

namespace NGraphics.Custom.Models.Elements
{
	public class Text : Element
	{
		public Rect Frame;
		public TextAlignment Alignment;
		public Font Font;
		public List<TextSpan> Spans;

		public string String { get { return string.Join ("", Spans.Select (x => x.Text)); } }

		public Text ()
			: base (null, null)
		{
			Spans = new List<TextSpan> ();
		}

		public Text (string text, Rect frame, Font font, TextAlignment alignment = TextAlignment.Left, Pen pen = null, BaseBrush brush = null)
			: base (pen, brush)
		{
			Frame = frame;
			Font = font;
			Alignment = alignment;
			Spans = new List<TextSpan> { new TextSpan (text) };
		}

		public Text (Rect frame, Font font, TextAlignment alignment = TextAlignment.Left, Pen pen = null, BaseBrush brush = null)
			: base (pen, brush)
		{
			Frame = frame;
			Font = font;
			Alignment = alignment;
			Spans = new List<TextSpan> ();
		}

		protected override void DrawElement (ICanvas canvas)
		{
			foreach (var s in Spans) {
				if (s.Position != null) {
					canvas.DrawText (s.Text, new Rect (s.Position.Value, Size.MaxValue), s.Font ?? Font, TextAlignment.Left, Pen, Brush);
				} else {
					canvas.DrawText (s.Text, Frame, s.Font ?? Font, Alignment, Pen, Brush);
				}
			}
		}


		public void Trim ()
		{
			while (Spans.Count > 0 && string.IsNullOrWhiteSpace (Spans [0].Text)) {
				Spans.RemoveAt (0);
			}
			if (Spans.Count > 0) {
				Spans [0].Text = Spans [0].Text.TrimStart ();
			}
			while (Spans.Count > 0 && string.IsNullOrWhiteSpace (Spans.Last().Text)) {
				Spans.RemoveAt (Spans.Count - 1);
			}
			if (Spans.Count > 0) {
				Spans.Last().Text = Spans.Last().Text.TrimEnd ();
			}
		}
	}

	public class TextSpan
	{
		public Point? Position;
		public Font Font;
		public string Text;

		public TextSpan (string text)
		{
			Text = text;
		}
	}

	public struct TextMetrics
	{
		public double Width;
		public double Ascent;
		public double Descent;
		public Size Size {
			get {
				return new Size (Width, Ascent + Descent);
			}
		}
	}
}