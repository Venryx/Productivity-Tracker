using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;

namespace Main
{
	public class ShapeDrawer : View
	{
		public ShapeDrawer(Context context) : base(context) { }

		public List<VShape> shapes = new List<VShape>();
		public void AddShape(VShape shape) { shapes.Add(shape); }
		protected override void OnDraw(Canvas canvas)
		{
			foreach (VShape shape in shapes)
				shape.Draw(canvas);
		}
	}
}

/*public enum VShapeOp
{
	Clear
}*/
public class VShape
{
	public VShape()
	{
		paint = new Paint();
		paint.Flags |= PaintFlags.AntiAlias;
		paint.SetStyle(Paint.Style.Fill);
		//paint.StrokeWidth = 1;
	}
	public Paint paint;
	public Color Color
	{
		get { return paint.Color; }
		set { paint.Color = value; }
	}
	/*VShapeOp op;
	public VShapeOp Op
	{
		get { return op; }
		set
		{
			op = value;
			//paint.SetXfermode(op == VShapeOp.Clear ? new PorterDuffXfermode(PorterDuff.Mode.Clear) : null);
		}
	}*/
	RectF clipRect;
	public RectF ClipRect
	{
		get { return clipRect; }
		set { clipRect = value; }
	}
	public virtual void Draw(Canvas canvas) { throw new NotImplementedException(); }
}
public class VRectangle : VShape
{
	public VRectangle(double left, double top, double right, double bottom) { rect = new RectF((float)left, (float)top, (float)right, (float)bottom); }
	public RectF rect;
	public override void Draw(Canvas canvas)
	{
		//canvas.DrawRect((float)(canvas.Width * left), (float)(canvas.Height * top), (float)(canvas.Width * right), (float)(canvas.Height * bottom), paint);
		//canvas.ClipRect(ClipRect?.Times(canvas.Width, canvas.Height).Extend(0, 0, 0, -1) ?? canvas.GetRect(), Region.Op.Replace);
		canvas.ClipRect(ClipRect?.Times(canvas.Width, canvas.Height) ?? canvas.GetRect(), Region.Op.Replace);
		canvas.DrawRect(rect.Times(canvas.Width, canvas.Height), paint);
	}
}
public class VOval : VShape
{
	public VOval(double left, double top, double right, double bottom) { rect = new RectF((float)left, (float)top, (float)right, (float)bottom); }
	public RectF rect;
	public override void Draw(Canvas canvas)
	{
		//canvas.DrawOval((float)(canvas.Width * left), (float)(canvas.Height * top), (float)(canvas.Width * right), (float)(canvas.Height * bottom), paint);
		canvas.ClipRect(ClipRect?.Times(canvas.Width, canvas.Height) ?? canvas.GetRect(), Region.Op.Replace);
		canvas.DrawOval(rect.Times(canvas.Width, canvas.Height), paint);
	}
}