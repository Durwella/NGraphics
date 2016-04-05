﻿using System;
using System.Collections.Generic;
using System.IO;
using Android.Graphics;
using Android.Text;
using NGraphics.Custom.Codes;
using NGraphics.Custom.Interfaces;
using NGraphics.Custom.Models;
using NGraphics.Custom.Models.Brushes;
using NGraphics.Custom.Models.Operations;
using NGraphics.Custom.Models.Transforms;
using Color = NGraphics.Custom.Models.Color;
using Path = Android.Graphics.Path;
using Point = NGraphics.Custom.Models.Point;
using Rect = NGraphics.Custom.Models.Rect;

namespace NGraphics.Android.Custom
{
  public class AndroidPlatform : IPlatform
  {
    public string Name
    {
      get { return "Android"; }
    }

    public IImageCanvas CreateImageCanvas(Size size, double scale = 1.0, bool transparency = true)
    {
      var pixelWidth = (int) Math.Ceiling(size.Width*scale);
      var pixelHeight = (int) Math.Ceiling(size.Height*scale);
      var bitmap = Bitmap.CreateBitmap(pixelWidth, pixelHeight, Bitmap.Config.Argb8888);
      if (!transparency)
      {
        bitmap.EraseColor(Colors.Black.Argb);
      }
      return new BitmapCanvas(bitmap, scale);
    }

    public IImage LoadImage(Stream stream)
    {
      var bitmap = BitmapFactory.DecodeStream(stream);
      return new BitmapImage(bitmap);
    }

    public IImage LoadImage(string path)
    {
      var bitmap = BitmapFactory.DecodeFile(path);
      return new BitmapImage(bitmap);
    }

    public IImage CreateImage(Color[] colors, int width, double scale = 1.0)
    {
      var pixelWidth = width;
      var pixelHeight = colors.Length/width;
      var acolors = new int[pixelWidth*pixelHeight];
      for (var i = 0; i < colors.Length; i++)
      {
        acolors[i] = colors[i].Argb;
      }
      var bitmap = Bitmap.CreateBitmap(acolors, pixelWidth, pixelHeight, Bitmap.Config.Argb8888);
      return new BitmapImage(bitmap, scale);
    }
  }

  public class BitmapImage : IImage
  {
    private readonly Bitmap bitmap;
    //		readonly double scale;

    public Bitmap Bitmap
    {
      get { return bitmap; }
    }

    public BitmapImage(Bitmap bitmap, double scale = 1.0)
    {
      this.bitmap = bitmap;
      //			this.scale = scale;
    }

    public Size Size
    {
      get { throw new NotImplementedException(); }
    }

    public double Scale
    {
      get { throw new NotImplementedException(); }
    }

    public void SaveAsPng(string path)
    {
      using (var f = File.OpenWrite(path))
      {
        bitmap.Compress(Bitmap.CompressFormat.Png, 100, f);
      }
    }

    public void SaveAsPng(Stream stream)
    {
      bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
    }
  }

  public class BitmapCanvas : CanvasCanvas, IImageCanvas
  {
    private readonly Bitmap bitmap;
    private readonly double scale;

    public Size Size
    {
      get { return new Size(bitmap.Width/scale, bitmap.Height/scale); }
    }

    public double Scale
    {
      get { return scale; }
    }

    public BitmapCanvas(Bitmap bitmap, double scale = 1.0)
      : base(new Canvas(bitmap))
    {
      this.bitmap = bitmap;
      this.scale = scale;

      graphics.Scale((float) scale, (float) scale);
    }

    public IImage GetImage()
    {
      return new BitmapImage(bitmap, scale);
    }
  }

  public class CanvasCanvas : ICanvas
  {
    protected readonly Canvas graphics;

    public CanvasCanvas(Canvas graphics)
    {
      this.graphics = graphics;
    }

    public void SaveState()
    {
      graphics.Save(SaveFlags.Matrix | SaveFlags.Clip);
    }

    public void Transform(Transform transform)
    {
      var t = new Matrix();
      t.SetValues(new[]
      {
        (float) transform.A, (float) transform.C, (float) transform.E,
        (float) transform.B, (float) transform.D, (float) transform.F,
        0, 0, 1
      });
      t.PostConcat(graphics.Matrix);
      graphics.Matrix = t;
    }

    public void RestoreState()
    {
      graphics.Restore();
    }

    private TextPaint GetFontPaint(Font font, TextAlignment alignment)
    {
      var paint = new TextPaint(PaintFlags.AntiAlias);
      paint.TextAlign = Paint.Align.Left;
      if (alignment == TextAlignment.Center)
        paint.TextAlign = Paint.Align.Left;
      else if (alignment == TextAlignment.Right)
        paint.TextAlign = Paint.Align.Right;

      paint.TextSize = (float) font.Size;
      var typeface = Typeface.Create(font.Family, font.IsBold ? TypefaceStyle.Bold : TypefaceStyle.Normal);
      paint.SetTypeface(typeface);

      return paint;
    }

    private Paint GetImagePaint(double alpha)
    {
      var paint = new Paint(PaintFlags.AntiAlias);
      paint.FilterBitmap = true;
      paint.Alpha = (int) (alpha*255);
      return paint;
    }

    private Paint GetPenPaint(Pen pen)
    {
      var paint = new Paint(PaintFlags.AntiAlias);
      paint.SetStyle(Paint.Style.Stroke);
      paint.SetARGB(pen.Color.A, pen.Color.R, pen.Color.G, pen.Color.B);
      paint.StrokeWidth = (float) pen.Width;
      return paint;
    }

    private Paint GetBrushPaint(BaseBrush brush, Rect frame)
    {
      var paint = new Paint(PaintFlags.AntiAlias);
      AddBrushPaint(paint, brush, frame);
      return paint;
    }

    private void AddBrushPaint(Paint paint, BaseBrush brush, Rect frame)
    {
      paint.SetStyle(Paint.Style.Fill);

      var sb = brush as SolidBrush;

      if (sb != null)
      {
        paint.SetARGB(sb.Color.A, sb.Color.R, sb.Color.G, sb.Color.B);
        return;
      }

      var lgb = brush as LinearGradientBrush;
      if (lgb != null)
      {
        var n = lgb.Stops.Count;
        if (n >= 2)
        {
          var locs = new float[n];
          var comps = new int[n];
          for (var i = 0; i < n; i++)
          {
            var s = lgb.Stops[i];
            locs[i] = (float) s.Offset;
            comps[i] = s.Color.Argb;
          }
          var p1 = lgb.Absolute ? lgb.Start : frame.Position + lgb.Start*frame.Size;
          var p2 = lgb.Absolute ? lgb.End : frame.Position + lgb.End*frame.Size;
          var lg = new LinearGradient(
            (float) p1.X, (float) p1.Y,
            (float) p2.X, (float) p2.Y,
            comps,
            locs,
            Shader.TileMode.Clamp);
          paint.SetShader(lg);
        }
        return;
      }

      var rgb = brush as RadialGradientBrush;
      if (rgb != null)
      {
        var n = rgb.Stops.Count;
        if (n >= 2)
        {
          var locs = new float[n];
          var comps = new int[n];
          for (var i = 0; i < n; i++)
          {
            var s = rgb.Stops[i];
            locs[i] = (float) s.Offset;
            comps[i] = s.Color.Argb;
          }
          var p1 = rgb.GetAbsoluteCenter(frame);
          var r = rgb.GetAbsoluteRadius(frame);
          var rg = new RadialGradient(
            (float) p1.X, (float) p1.Y,
            (float) r.Max,
            comps,
            locs,
            Shader.TileMode.Clamp);

          paint.SetShader(rg);
        }
        return;
      }

      throw new NotSupportedException("Brush " + brush);
    }

    public void DrawText(string text, Rect frame, Font font, TextAlignment alignment = TextAlignment.Left,
      Pen pen = null, BaseBrush brush = null)
    {
      if (brush == null)
        return;

      var paint = GetFontPaint(font, alignment);
      var w = paint.MeasureText(text);
      var fm = paint.GetFontMetrics();
      var h = fm.Ascent + fm.Descent;
      var point = frame.Position;
      var fr = new Rect(point, new Size(w, h));
      AddBrushPaint(paint, brush, fr);
      graphics.DrawText(text, (float) point.X, (float) point.Y, paint);
    }

    public void DrawPath(IEnumerable<PathOperation> ops, Pen pen = null, BaseBrush brush = null)
    {
      using (var path = new Path())
      {
        var bb = new BoundingBoxBuilder();

        foreach (var op in ops)
        {
          var moveTo = op as MoveTo;
          if (moveTo != null)
          {
            var start = moveTo.Start;
            var end = moveTo.End;

            path.MoveTo((float) start.X, (float) start.Y);

            bb.Add(start);
            bb.Add(end);
            continue;
          }
          var lineTo = op as LineTo;
          if (lineTo != null)
          {
            var start = lineTo.Start;
            var end = lineTo.End;
            path.LineTo((float) start.X, (float) start.Y);
            path.LineTo((float) end.X, (float) end.Y);
            bb.Add(start);
            bb.Add(end);
            continue;
          }
          var at = op as ArcTo;
          if (at != null)
          {
            var p = at.Point;
            path.LineTo((float) p.X, (float) p.Y);
            bb.Add(p);
            continue;
          }
          var curveTo = op as CurveTo;
          if (curveTo != null)
          {
            var end = curveTo.End;
            var firstControlPoint = curveTo.FirstControlPoint;
            var secondControlPoint = curveTo.SecondControlPoint;

            path.CubicTo((float) firstControlPoint.X, (float) firstControlPoint.Y, (float) secondControlPoint.X,
              (float) secondControlPoint.Y, (float) end.X, (float) end.Y);

            bb.Add(firstControlPoint);
            bb.Add(secondControlPoint);
            bb.Add(end);
            continue;
          }
          var cp = op as ClosePath;
          if (cp != null)
          {
            path.Close();
            continue;
          }

          throw new NotSupportedException("Path Op " + op);
        }

        var frame = bb.BoundingBox;

        if (brush != null)
        {
          var solidBrush = brush as SolidBrush;

          if (solidBrush != null)
          {
            path.SetFillType(GetPathFillType(((SolidBrush)brush).FillMode));
          }
          
          var brushPaint = GetBrushPaint(brush, frame);
          graphics.DrawPath(path, brushPaint);
        }
        if (pen != null)
        {
          var penPaint = GetPenPaint(pen);
          graphics.DrawPath(path, penPaint);
        }
      }
    }

    private Path.FillType GetPathFillType(FillMode fillMode)
    {
      switch (fillMode)
      {
          case FillMode.EvenOdd:
          return Path.FillType.EvenOdd;
      }

      return Path.FillType.Winding;
    }

    public void DrawRectangle(Rect frame, Pen pen = null, BaseBrush brush = null)
    {
      if (brush != null)
      {
        var paint = GetBrushPaint(brush, frame);
        graphics.DrawRect((float) (frame.X), (float) (frame.Y), (float) (frame.X + frame.Width),
          (float) (frame.Y + frame.Height), paint);
      }
      if (pen != null)
      {
        var paint = GetPenPaint(pen);
        graphics.DrawRect((float) (frame.X), (float) (frame.Y), (float) (frame.X + frame.Width),
          (float) (frame.Y + frame.Height), paint);
      }
    }

    public void DrawEllipse(Rect frame, Pen pen = null, BaseBrush brush = null)
    {
      if (brush != null)
      {
        var paint = GetBrushPaint(brush, frame);
        graphics.DrawOval(frame.GetRectF(), paint);
      }
      if (pen != null)
      {
        var paint = GetPenPaint(pen);
        graphics.DrawOval(frame.GetRectF(), paint);
      }
    }

    public void DrawImage(IImage image, Rect frame, double alpha = 1.0)
    {
      var ii = image as BitmapImage;
      if (ii != null)
      {
        var paint = GetImagePaint(alpha);
        var isize = new Size(ii.Bitmap.Width, ii.Bitmap.Height);
        var scale = frame.Size/isize;
        var m = new Matrix();
        m.PreTranslate((float) frame.X, (float) frame.Y);
        m.PreScale((float) scale.Width, (float) scale.Height);
        graphics.DrawBitmap(ii.Bitmap, m, paint);
      }
    }
  }

  public static class Conversions
  {
    public static PointF GetPointF(this Point point)
    {
      return new PointF((float) point.X, (float) point.Y);
    }

    public static RectF GetRectF(this Rect frame)
    {
      return new RectF((float) frame.X, (float) frame.Y, (float) (frame.X + frame.Width),
        (float) (frame.Y + frame.Height));
    }
  }
}