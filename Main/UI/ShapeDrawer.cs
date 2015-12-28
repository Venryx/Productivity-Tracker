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
			if (Width == 0 || Height == 0)
				return;

			Bitmap drawCanvas_bitmap = null;
			Canvas drawCanvas = canvas;
			// if drawing to special canvas (i.e. not the internal same-size-as-view one)
			if (canvas.Width > Width || canvas.Height > Height)
			{
				drawCanvas_bitmap = Bitmap.CreateBitmap(Width, Height, Bitmap.Config.Argb8888);
				drawCanvas = new Canvas(drawCanvas_bitmap);
			}

			foreach (VShape shape in shapes)
				shape.Draw(drawCanvas);

			if (drawCanvas_bitmap != null)
				//canvas.DrawBitmap(drawCanvas_bitmap, this.GetPositionFrom().x, this.GetPositionFrom().y, null);
				canvas.DrawBitmap(drawCanvas_bitmap, 0, 0, null); // just draw at '0 0', since a matrix is apparently auto-applied for drawing at correct place on overall-canvas
		}
	}
	// probably make-so: ShapeDrawer is powerful enough for BorderDrawer to work just by sending different prop-values to it
	/*public class BorderDrawer : View
	{
		public BorderDrawer(Context context) : base(context) {}
		public BorderDrawer(Context context, Color color, int leftWidth, int topWidth, int rightWidth, int bottomWidth) : base(context)
		{
			this.color = color;
			this.leftWidth = leftWidth;
			this.topWidth = topWidth;
			this.rightWidth = rightWidth;
			this.bottomWidth = bottomWidth;

			paint = new Paint();
			paint.Flags |= PaintFlags.AntiAlias;
			paint.SetStyle(Paint.Style.Fill);
		}

		Color color;
		int leftWidth;
		int topWidth;
		int rightWidth;
		int bottomWidth;

		Paint paint;

		public List<VShape> shapes = new List<VShape>();
		public void AddShape(VShape shape) { shapes.Add(shape); }
		protected override void OnDraw(Canvas canvas)
		{
			canvas.DrawRect(new RectF(0, 0, leftWidth, canvas.Height), paint);
			canvas.DrawRect(new RectF(0, 0, canvas.Width, topWidth), paint);
			canvas.DrawRect(new RectF(canvas.Width - rightWidth, 0, rightWidth, canvas.Height), paint);
			canvas.DrawRect(new RectF(0, canvas.Height - bottomWidth, canvas.Width, bottomWidth), paint);
		}
	}*/
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