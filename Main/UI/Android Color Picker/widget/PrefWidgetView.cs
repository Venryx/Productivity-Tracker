using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace AndroidColorPicker
{
	public class ColorPickerPrefWidgetView : View
	{
		internal Paint paint;
		internal float rectSize;
		internal float strokeWidth;

		public ColorPickerPrefWidgetView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			float density = context.Resources.DisplayMetrics.Density;
			rectSize = (float)Math.Floor(24.0f * density + 0.5f);
			strokeWidth = (float)Math.Floor(1.0f * density + 0.5f);

			paint = new Paint();
			paint.Color = Color.White;
			paint.SetStyle(Paint.Style.Stroke);
			paint.StrokeWidth = strokeWidth;
		}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			canvas.DrawRect(strokeWidth, strokeWidth, rectSize - strokeWidth, rectSize - strokeWidth, paint);
		}
	}
}