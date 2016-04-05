namespace NGraphics.Custom.Models
{
	public class Font
	{
		string name = "Georgia";

		public Font ()
		{
			Size = 16;
		}

		public Font (string name, double size)
		{
			this.name = name;
			this.Size = size;
		}

		public string Name { get { return name; } set { name = value; } }
		public string Family { get { return name; } set { name = value; } }
		public bool IsBold { get; set; } = false;

		public double Size { get; set; }

		public Font WithFamily (string family)
		{
			return new Font (family, Size);
		}

		public Font WithStyle (string style)
		{
			return this;
		}

		public Font WithWeight (string weight)
		{
			this.IsBold = (weight == "bold");
			return this;
		}

		public Font WithSize (double newSize)
		{
			this.Size = newSize;
			return this;
		}

		public override string ToString ()
		{
			return string.Format ("Font(\"{0}\", {1})", Name, Size);
		}
	}
}