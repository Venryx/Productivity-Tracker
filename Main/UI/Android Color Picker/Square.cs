using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace AndroidColorPicker
{
	public class ColorPickerSquare : View
	{
		internal Paint paint;
		internal Shader luar;
		internal readonly float[] color = {1.0f, 1.0f, 1.0f};

		public ColorPickerSquare(Context context) : base(context) {}
		public ColorPickerSquare(Context context, IAttributeSet attrs) : base(context, attrs) {}
		public ColorPickerSquare(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) {}

		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);
			if (paint == null)
			{
				paint = new Paint();
				luar = new LinearGradient(0.0f, 0.0f, 0.0f, this.MeasuredHeight, new Color(255, 255, 255, 255), new Color(0, 0, 0, 255), Shader.TileMode.Clamp);
			}
			int rgb = Color.HSVToColor(color);
			Shader dalam = new LinearGradient(0.0f, 0.0f, this.MeasuredWidth, 0.0f, new Color(255, 255, 255, 255), Color.ParseColor(rgb.ToHexStr()), Shader.TileMode.Clamp);
			ComposeShader shader = new ComposeShader(luar, dalam, PorterDuff.Mode.Multiply);
			paint.SetShader(shader);
			canvas.DrawRect(0.0f, 0.0f, this.MeasuredWidth, this.MeasuredHeight, paint);
		}

		internal virtual float Hue
		{
			set
			{
				color[0] = value;
				Invalidate();
			}
		}
	}
}