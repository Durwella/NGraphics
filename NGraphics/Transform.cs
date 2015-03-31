using System;
using System.Globalization;

namespace NGraphics
{
    public abstract class Transform
    {
        protected Transform(Transform previous = null)
        {
            Previous = previous;
        }

        public Transform Previous;
        protected abstract string ToCode();

        public override string ToString()
        {
            var s = ToCode();
            if (Previous != null)
            {
                s = Previous + " " + s;
            }
            return s;
        }
    }

    public class MatrixTransform : Transform
    {
        public MatrixTransform(Transform previous = null)
            : base(previous)
        {
            Elements = new double[6];
        }

        public MatrixTransform(double[] elements, Transform previous = null)
            : this(previous)
        {
            if (elements == null)
                throw new ArgumentNullException("elements");
            if (elements.Length != 6)
                throw new ArgumentException("6 elements were expected");
            Array.Copy(elements, Elements, 6);
        }

        public double[] Elements;

        protected override string ToCode()
        {
            return string.Format(CultureInfo.InvariantCulture, "matrix(...)");
        }
    }

    public class Translate : Transform
    {
        public Translate(Size size, Transform previous = null)
            : base(previous)
        {
            Size = size;
        }

        public Translate(Point offset, Transform previous = null)
            : base(previous)
        {
            Size = new Size(offset.X, offset.Y);
        }

        public Translate(double dx, double dy, Transform previous = null)
            : this(new Size(dx, dy), previous)
        {
        }

        public Size Size;

        protected override string ToCode()
        {
            return string.Format(CultureInfo.InvariantCulture, "translate({0}, {1})", Size.Width, Size.Height);
        }
    }

    public class Scale : Transform
    {
        public Scale(Size size, Transform previous = null)
            : base(previous)
        {
            Size = size;
        }

        public Scale(double dx, double dy, Transform previous = null)
            : this(new Size(dx, dy), previous)
        {
        }

        public Size Size;

        protected override string ToCode()
        {
            return string.Format(CultureInfo.InvariantCulture, "scale({0}, {1})", Size.Width, Size.Height);
        }
    }

    public class Rotate : Transform
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NGraphics.Rotate" /> class.
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <param name="previous">Previous.</param>
        public Rotate(double angle, Transform previous = null)
            : base(previous)
        {
            Angle = angle;
        }

        /// <summary>
        ///     The angle in degrees.
        /// </summary>
        public double Angle;

        protected override string ToCode()
        {
            return string.Format(CultureInfo.InvariantCulture, "rotate({0})", Angle);
        }
    }
}