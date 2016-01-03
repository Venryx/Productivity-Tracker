using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Views;
using Android.Widget;

public static class Drawables
{
	// colors
	public static ColorDrawable green;
	public static ColorDrawable green_dark;
	public static ColorDrawable blue;
	public static ColorDrawable blue_dark;

	// x-plus clips
	public static ClipDrawable clip_xPlus_green;
	public static ClipDrawable clip_xPlus_green_dark;
	public static ClipDrawable clip_xPlus_blue;
	public static ClipDrawable clip_xPlus_blue_dark;

	// y-plus clips
	public static ClipDrawable clip_yPlus_green;
	public static ClipDrawable clip_yPlus_green_dark;
	public static ClipDrawable clip_yPlus_blue;
	public static ClipDrawable clip_yPlus_blue_dark;

	static Drawables()
	{
		green = new ColorDrawable(new Color(0, 255, 0));
		green_dark = new ColorDrawable(new Color(0, 128, 0));
		blue = new ColorDrawable(new Color(0, 0, 255));
		blue_dark = new ColorDrawable(new Color(0, 0, 128));

		clip_xPlus_green = new ClipDrawable(green.Clone(), GravityFlags.Left, ClipDrawableOrientation.Horizontal);
		clip_xPlus_green_dark = new ClipDrawable(green_dark.Clone(), GravityFlags.Left, ClipDrawableOrientation.Horizontal);
		clip_xPlus_blue = new ClipDrawable(blue.Clone(), GravityFlags.Left, ClipDrawableOrientation.Horizontal);
		clip_xPlus_blue_dark = new ClipDrawable(blue_dark.Clone(), GravityFlags.Left, ClipDrawableOrientation.Horizontal);

		clip_yPlus_green = new ClipDrawable(green.Clone(), GravityFlags.Bottom, ClipDrawableOrientation.Vertical);
		clip_yPlus_green_dark = new ClipDrawable(green_dark.Clone(), GravityFlags.Bottom, ClipDrawableOrientation.Vertical);
		clip_yPlus_blue = new ClipDrawable(blue.Clone(), GravityFlags.Bottom, ClipDrawableOrientation.Vertical);
		clip_yPlus_blue_dark = new ClipDrawable(blue_dark.Clone(), GravityFlags.Bottom, ClipDrawableOrientation.Vertical);
	}

	public static ColorDrawable CreateColor(Color color) { return new ColorDrawable(color); }
	public static ShapeDrawable CreateFill(Color color)
	{
		var rect = new RectShape();
		var result = new ShapeDrawable(rect);
		result.Paint.Color = color;
		result.Paint.SetStyle(Paint.Style.Fill);
		return result;
	}
	public static ShapeDrawable CreateStroke(Color color, int strokeWidth)
	{
		var rect = new RectShape();
		var result = new ShapeDrawable(rect);
		result.Paint.Color = color;
		result.Paint.SetStyle(Paint.Style.Stroke);
		result.Paint.StrokeWidth = strokeWidth;
		return result;
	}
}

public class BorderDrawable : Drawable
{
	public BorderDrawable(Color color, int leftWidth, int topWidth, int rightWidth, int bottomWidth)
	{
		//this.color = color;
		this.leftWidth = leftWidth;
		this.topWidth = topWidth;
		this.rightWidth = rightWidth;
		this.bottomWidth = bottomWidth;

		paint = new Paint();
		//paint.Flags |= PaintFlags.AntiAlias;
		paint.SetStyle(Paint.Style.Fill);
		paint.Color = color;
	}

	//Color color;
	int leftWidth;
	int topWidth;
	int rightWidth;
	int bottomWidth;

	Paint paint;

	public override void Draw(Canvas canvas)
	{
		Bitmap drawCanvas_bitmap = null;
		Canvas drawCanvas = canvas;
		// if drawing to special canvas (i.e. not the internal same-size-as-view one)
		//if (canvas.Width > Width || canvas.Height > Height)
		if (canvas.Width != canvas.ClipBounds.Width() || canvas.Height != canvas.ClipBounds.Height())
		{
			if (canvas.ClipBounds.Width() == 0 || canvas.ClipBounds.Height() == 0) // (happens when the "Snap to nearest row" option is enabled, for the graph exporter; not sure why)
				return;
			drawCanvas_bitmap = Bitmap.CreateBitmap(canvas.ClipBounds.Width(), canvas.ClipBounds.Height(), Bitmap.Config.Argb8888);
			drawCanvas = new Canvas(drawCanvas_bitmap);
		}

		drawCanvas.DrawRect(new RectF(0, 0, leftWidth, drawCanvas.Height), paint);
		drawCanvas.DrawRect(new RectF(0, 0, drawCanvas.Width, topWidth), paint);
		drawCanvas.DrawRect(new RectF(drawCanvas.Width - rightWidth, 0, drawCanvas.Width, drawCanvas.Height), paint);
		drawCanvas.DrawRect(new RectF(0, drawCanvas.Height - bottomWidth, drawCanvas.Width, drawCanvas.Height), paint);

		if (drawCanvas_bitmap != null)
			//canvas.DrawBitmap(drawCanvas_bitmap, this.GetPositionFrom().x, this.GetPositionFrom().y, null);
			canvas.DrawBitmap(drawCanvas_bitmap, 0, 0, null); // just draw at '0 0', since a matrix is apparently auto-applied for drawing at correct place on overall-canvas
	}
	
	// required members
	public override void SetAlpha(int alpha) {}
	public override void SetColorFilter(ColorFilter cf) {}
	public override int Opacity { get; }
}