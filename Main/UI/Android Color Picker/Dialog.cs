using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Main;

namespace AndroidColorPicker
{
	public class ColorPickerDialog
	{
		public interface ColorPickerDialogListener
		{
			void onCancel(ColorPickerDialog dialog);

			void onOk(ColorPickerDialog dialog, int color);
		}

		internal readonly AlertDialog dialog;
		readonly bool supportsAlpha;
		public Action<Color> OnOK = delegate {};
		public Action OnCancel = delegate {};

		// views
		internal readonly ImageView viewHue;
		internal readonly ColorPickerSquare viewSatVal;
		internal readonly ImageView viewCursor;
		internal readonly ImageView viewAlphaCursor;
		internal readonly View viewOldColor;
		internal readonly View viewNewColor;
		internal readonly View viewAlphaOverlay;
		internal readonly ImageView viewTarget;
		internal readonly ImageView viewAlphaCheckered;
		internal readonly ViewGroup viewContainer;

		internal readonly float[] currentColorHsv = new float[3];
		internal int alpha;

		/// <summary>Create an ColorPickerDialog.</summary>
		/// <param name = "context"> activity context </param>
		/// <param name = "color"> current color </param>
		/// <param name = "supportsAlpha"> whether alpha/transparency controls are enabled </param>
		/// <param name = "listener"> an OnAmbilWarnaListener, allowing you to get back error or OK </param>
		public ColorPickerDialog(Context context, Color color, bool supportsAlpha = false, string dialogTitle = "Color")
		{
			this.supportsAlpha = supportsAlpha;

			if (!supportsAlpha) // remove alpha if not supported
				//color = color | unchecked((int)0xff000000);
				color.A = 255;

			//Color.ColorToHSV(color, currentColorHsv);
			Color.ColorToHSV(color, currentColorHsv);
			//alpha = Color.Alpha(color);
			alpha = color.A;

			//View view = LayoutInflater.From(context).Inflate(Resource.Layout.ambilwarna_dialog, null);
			var view = new FrameLayout(context);
			{
				viewContainer = view.AddChild(new RelativeLayout(context), new FrameLayout.LayoutParams(V.WrapContent, V.WrapContent) {Gravity = GravityFlags.Center});
				viewContainer.SetPadding(8, 8, 8, 8);
				{
					viewSatVal = viewContainer.AddChild(new ColorPickerSquare(context).SetID(), new RelativeLayout.LayoutParams(240, 240));
					viewSatVal.SetLayerType(LayerType.Software, null);
					viewHue = viewContainer.AddChild(new ImageView(context).SetID(), new RelativeLayout.LayoutParams(30, 240) {LeftMargin = 8}.VAddRule(LayoutRules.RightOf, viewSatVal.Id));
					viewHue.SetScaleType(ImageView.ScaleType.FitXy);
					viewHue.SetBackgroundResource(Resource.Drawable.ambilwarna_hue);
					viewAlphaCheckered = viewContainer.AddChild(new ImageView(context), new RelativeLayout.LayoutParams(30, 240) {LeftMargin = 8}.VAddRule(LayoutRules.RightOf, viewHue.Id));
					viewAlphaCheckered.SetScaleType(ImageView.ScaleType.FitXy);
					viewAlphaCheckered.SetBackgroundResource(Resource.Drawable.ambilwarna_alphacheckered_tiled);
					viewAlphaOverlay = viewContainer.AddChild(new View(context), new RelativeLayout.LayoutParams(30, 240) {LeftMargin = 8}.VAddRule(LayoutRules.RightOf, viewHue.Id));
					viewCursor = viewContainer.AddChild(new ImageView(context), new RelativeLayout.LayoutParams(9, 9));
					viewCursor.SetScaleType(ImageView.ScaleType.Matrix);
					viewCursor.SetBackgroundResource(Resource.Drawable.ambilwarna_cursor);
					viewAlphaCursor = viewContainer.AddChild(new ImageView(context), new RelativeLayout.LayoutParams(9, 9));
					viewAlphaCursor.SetScaleType(ImageView.ScaleType.Matrix);
					viewAlphaCursor.SetBackgroundResource(Resource.Drawable.ambilwarna_cursor);
					viewTarget = viewContainer.AddChild(new ImageView(context), new RelativeLayout.LayoutParams(15, 15));
					viewTarget.SetScaleType(ImageView.ScaleType.Matrix);
					viewTarget.SetBackgroundResource(Resource.Drawable.ambilwarna_target);
					var state = viewContainer.AddChild(new LinearLayout(context) { Orientation = Orientation.Horizontal}, new RelativeLayout.LayoutParams(V.WrapContent, V.WrapContent) {TopMargin = 8}.VAddRule(LayoutRules.Below, viewSatVal.Id));
					state.SetGravity(GravityFlags.Center);
					//state.SetHorizontalGravity(GravityFlags.Center);
					{
						var oldColorPanel = state.AddChild(new FrameLayout(context), new LinearLayout.LayoutParams(60, 30));
						{
							oldColorPanel.AddChild(new View(context), new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent)).SetBackgroundResource(Resource.Drawable.ambilwarna_alphacheckered_tiled);
							viewOldColor = oldColorPanel.AddChild(new View(context), new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
							viewOldColor.SetBackgroundColor(new Color(255, 160, 160));
						}
						//var arrow = state.AddChild(new ImageView(context), new LinearLayout.LayoutParams(V.WrapContent, V.WrapContent));
						var arrow = state.AddChild(new ImageView(context), new LinearLayout.LayoutParams(40, 40));
						arrow.SetPadding(8, 0, 8, 0);
						arrow.SetImageResource(Resource.Drawable.ambilwarna_arrow_right);
						var newColorPanel = state.AddChild(new FrameLayout(context), new LinearLayout.LayoutParams(60, 30));
						{
							newColorPanel.AddChild(new View(context), new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent)).SetBackgroundResource(Resource.Drawable.ambilwarna_alphacheckered_tiled);
							viewNewColor = newColorPanel.AddChild(new View(context), new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
							viewNewColor.SetBackgroundColor(new Color(160, 160, 255));
						}
					}
				}
			}

			{
				// hide/show alpha
				viewAlphaOverlay.Visibility = supportsAlpha ? ViewStates.Visible : ViewStates.Gone;
				viewAlphaCursor.Visibility = supportsAlpha ? ViewStates.Visible : ViewStates.Gone;
				viewAlphaCheckered.Visibility = supportsAlpha ? ViewStates.Visible : ViewStates.Gone;
			}

			viewSatVal.Hue = Hue;
			viewOldColor.SetBackgroundColor(color);
			viewNewColor.SetBackgroundColor(color);

			viewHue.Touch += (v, e)=>
			{
				if (e.Event.Action == MotionEventActions.Move || e.Event.Action == MotionEventActions.Down || e.Event.Action == MotionEventActions.Up)
				{
					//float y = e.Y;
					float y = e.Event.GetY();
					if (y < 0f)
						y = 0;
					if (y > viewHue.MeasuredHeight)
						y = viewHue.MeasuredHeight - .001f; // to avoid jumping the cursor from bottom to top.
					float hue = (1 - (y / viewHue.MeasuredHeight)) * 360;
					if (hue == 360f)
						hue = 0;
					Hue = hue;

					// update view
					viewSatVal.Hue = Hue;
					moveCursor();
					viewNewColor.SetBackgroundColor(CurrentColor);
					updateAlphaView();
					//return true;
				}
				//return false;
			};

			if (supportsAlpha)
				viewAlphaCheckered.Touch += (v, e)=>
				{
					if ((e.Event.Action == MotionEventActions.Move) || (e.Event.Action == MotionEventActions.Down) || (e.Event.Action == MotionEventActions.Up))
					{
						float y = e.Event.GetY();
						if (y < 0f)
							y = 0;
						if (y > viewAlphaCheckered.MeasuredHeight)
							y = viewAlphaCheckered.MeasuredHeight - .001f; // to avoid jumping the cursor from bottom to top.
						int a = (int)Math.Round(255f - ((255f / viewAlphaCheckered.MeasuredHeight) * y));
						setAlpha(a);

						// update view
						moveAlphaCursor();
						viewNewColor.SetBackgroundColor(CurrentColor);
						//return true;
					}
					//return false;
				};
			viewSatVal.Touch += (v, e)=>
			{
				if (e.Event.Action == MotionEventActions.Move || e.Event.Action == MotionEventActions.Down || e.Event.Action == MotionEventActions.Up)
				{
					float x = e.Event.GetX(); // touch event are in dp units.
					float y = e.Event.GetY();

					if (x < 0f)
						x = 0;
					if (x > viewSatVal.MeasuredWidth)
						x = viewSatVal.MeasuredWidth;
					if (y < 0f)
						y = 0;
					if (y > viewSatVal.MeasuredHeight)
						y = viewSatVal.MeasuredHeight;

					Sat = 1f / viewSatVal.MeasuredWidth * x;
					Val = 1f - (1f / viewSatVal.MeasuredHeight * y);

					// update view
					moveTarget();
					viewNewColor.SetBackgroundColor(CurrentColor);

					//return true;
				}
				//return false;
			};

			dialog = (new AlertDialog.Builder(context)).SetPositiveButton(Android.Resource.String.Ok, (sender, e)=>OnOK(CurrentColor))
				.SetTitle(dialogTitle)
				.SetNegativeButton(Android.Resource.String.Cancel, (sender, e)=>OnCancel())
				.SetOnCancelListener(new DialogOnCancelListener(this))
				.Create();
			// kill all padding from the dialog window
			dialog.SetView(view, 0, 0, 0, 0);

			// move cursor & target on first draw
			ViewTreeObserver vto = view.ViewTreeObserver;
			EventHandler onGlobalLayout = null;
			onGlobalLayout = (sender, e)=>
			{
				moveCursor();
				if (supportsAlpha)
					moveAlphaCursor();
				moveTarget();
				if (supportsAlpha)
					updateAlphaView();
				view.ViewTreeObserver.GlobalLayout -= onGlobalLayout;
			};
			vto.GlobalLayout += onGlobalLayout;
		}
		class DialogOnCancelListener : IDialogInterfaceOnCancelListener
		{
			public DialogOnCancelListener(ColorPickerDialog s) { this.s = s; }
			ColorPickerDialog s;
			public void OnCancel(IDialogInterface e) { s.OnCancel(); }
			public IntPtr Handle { get; }
			public void Dispose() {}
		}
		
		protected internal virtual void moveCursor()
		{
			float y = (1 - (Hue / 360)) * viewHue.MeasuredHeight;
			if (y == viewHue.MeasuredHeight)
				y = 0;
			RelativeLayout.LayoutParams layoutParams = (RelativeLayout.LayoutParams)viewCursor.LayoutParameters;
			layoutParams.LeftMargin = (int)(viewHue.Left - Math.Floor(viewCursor.MeasuredWidth / 2d) - viewContainer.PaddingLeft);
			layoutParams.TopMargin = (int)(viewHue.Top + y - Math.Floor(viewCursor.MeasuredHeight / 2d) - viewContainer.PaddingTop);
			viewCursor.LayoutParameters = layoutParams;
		}

		protected internal virtual void moveTarget()
		{
			float x = Sat * viewSatVal.MeasuredWidth;
			float y = (1.0f - Val) * viewSatVal.MeasuredHeight;
			RelativeLayout.LayoutParams layoutParams = (RelativeLayout.LayoutParams)viewTarget.LayoutParameters;
			layoutParams.LeftMargin = (int)(viewSatVal.Left + x - Math.Floor(viewTarget.MeasuredWidth / 2d) - viewContainer.PaddingLeft);
			layoutParams.TopMargin = (int)(viewSatVal.Top + y - Math.Floor(viewTarget.MeasuredHeight / 2d) - viewContainer.PaddingTop);
			viewTarget.LayoutParameters = layoutParams;
		}

		protected internal virtual void moveAlphaCursor()
		{
			//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
			//ORIGINAL LINE: final int measuredHeight = this.viewAlphaCheckered.getMeasuredHeight();
			int measuredHeight = this.viewAlphaCheckered.MeasuredHeight;
			float y = measuredHeight - ((this.getAlpha() * measuredHeight) / 255.0f);
			//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
			//ORIGINAL LINE: final android.widget.RelativeLayout.LayoutParams layoutParams = (android.widget.RelativeLayout.LayoutParams) this.viewAlphaCursor.getLayoutParams();
			RelativeLayout.LayoutParams layoutParams = (RelativeLayout.LayoutParams)viewAlphaCursor.LayoutParameters;
			layoutParams.LeftMargin = (int)(this.viewAlphaCheckered.Left - Math.Floor(viewAlphaCursor.MeasuredWidth / 2d) - viewContainer.PaddingLeft);
			layoutParams.TopMargin = (int)((this.viewAlphaCheckered.Top + y) - Math.Floor(viewAlphaCursor.MeasuredHeight / 2d) - viewContainer.PaddingTop);

			viewAlphaCursor.LayoutParameters = layoutParams;
		}

		Color CurrentColor
		{
			get
			{
				//int argb = Color.HSVToColor(currentColorHsv);
				//return alpha << 24 | (argb & 0x00ffffff);
				var color = Color.HSVToColor(currentColorHsv);
				color.A = (byte)alpha;
				return color;
			}
		}

		float Hue
		{
			get { return currentColorHsv[0]; }
			set { currentColorHsv[0] = value; }
		}

		float getAlpha() { return alpha; }

		float Sat
		{
			get { return currentColorHsv[1]; }
			set { currentColorHsv[1] = value; }
		}

		float Val
		{
			get { return currentColorHsv[2]; }
			set { currentColorHsv[2] = value; }
		}

		void setAlpha(int alpha) { this.alpha = alpha; }

		public virtual void show() { dialog.Show(); }

		public virtual AlertDialog Dialog
		{
			get { return dialog; }
		}

		void updateAlphaView()
		{
			//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
			//ORIGINAL LINE: final android.graphics.drawable.GradientDrawable gd = new android.graphics.drawable.GradientDrawable(android.graphics.drawable.GradientDrawable.Orientation.TOP_BOTTOM, new int[] { android.graphics.Color.HSVToColor(currentColorHsv), 0x0 });
			GradientDrawable gd = new GradientDrawable(GradientDrawable.Orientation.TopBottom, new[] {Color.HSVToColor(currentColorHsv), 0x0});
			viewAlphaOverlay.Background = gd;
		}
	}
}