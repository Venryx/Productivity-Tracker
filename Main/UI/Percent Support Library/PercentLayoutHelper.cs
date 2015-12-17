/*
 * Copyright (C) 2015 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Android.Content;
using Android.Content.Res;
using Android.Util;
using Android.Views;
using Main;

namespace android.support.percent
{
	/// <summary>
	/// Helper for layouts that want to support percentage based dimensions.
	/// 
	/// <para>This class collects utility methods that are involved in extracting percentage based dimension
	/// attributes and applying them to ViewGroup's children. If you would like to implement a layout
	/// that supports percentage based dimensions, you need to take several steps:
	/// 
	/// <ol>
	/// <li> You need a <seealso cref="ViewGroup.LayoutParams"/> subclass in your ViewGroup that implements
	/// <seealso cref="android.support.percent.PercentLayoutHelper.PercentLayoutParams"/>.
	/// <li> In your {@code LayoutParams(Context c, AttributeSet attrs)} constructor create an instance
	/// of <seealso cref="PercentLayoutHelper.PercentLayoutInfo"/> by calling
	/// <seealso cref="PercentLayoutHelper#getPercentLayoutInfo(Context, AttributeSet)"/>. Return this
	/// object from {@code public PercentLayoutHelper.PercentLayoutInfo getPercentLayoutInfo()}
	/// method that you implemented for <seealso cref="android.support.percent.PercentLayoutHelper.PercentLayoutParams"/> interface.
	/// <li> Override
	/// <seealso cref="ViewGroup.LayoutParams#setBaseAttributes(TypedArray, int, int)"/>
	/// with a single line implementation {@code PercentLayoutHelper.fetchWidthAndHeight(this, a,
	/// widthAttr, heightAttr);}
	/// <li> In your ViewGroup override <seealso cref="ViewGroup#generateLayoutParams(AttributeSet)"/> to return
	/// your LayoutParams.
	/// <li> In your <seealso cref="ViewGroup#onMeasure(int, int)"/> override, you need to implement following
	/// pattern:
	/// <pre class="prettyprint">
	/// protected void onMeasure(int widthMeasureSpec, int heightMeasureSpec) {
	///     mHelper.adjustChildren(widthMeasureSpec, heightMeasureSpec);
	///     super.onMeasure(widthMeasureSpec, heightMeasureSpec);
	///     if (mHelper.handleMeasuredStateTooSmall()) {
	///         super.onMeasure(widthMeasureSpec, heightMeasureSpec);
	///     }
	/// }
	/// </pre>
	/// <li>In your <seealso cref="ViewGroup#onLayout(boolean, int, int, int, int)"/> override, you need to
	/// implement following pattern:
	/// <pre class="prettyprint">
	/// protected void onLayout(boolean changed, int left, int top, int right, int bottom) {
	///     super.onLayout(changed, left, top, right, bottom);
	///     mHelper.restoreOriginalParams();
	/// }
	/// </pre>
	/// </ol>
	/// </para>
	/// </summary>
	public class PercentLayoutHelper
	{
		private const string TAG = "PercentLayout";
		private readonly ViewGroup mHost;
		public PercentLayoutHelper(ViewGroup host)
		{
			mHost = host;
		}
		/// <summary>
		/// Helper method to be called from <seealso cref="ViewGroup.LayoutParams#setBaseAttributes"/> override
		/// that reads layout_width and layout_height attribute values without throwing an exception if
		/// they aren't present.
		/// </summary>
		public static void fetchWidthAndHeight(ViewGroup.LayoutParams @params, TypedArray array, int widthAttr, int heightAttr)
		{
			@params.Width = array.GetLayoutDimension(widthAttr, 0);
			@params.Height = array.GetLayoutDimension(heightAttr, 0);
		}
		/// <summary>
		/// Iterates over children and changes their width and height to one calculated from percentage
		/// values. </summary>
		/// <param name="widthMeasureSpec"> Width MeasureSpec of the parent ViewGroup. </param>
		/// <param name="heightMeasureSpec"> Height MeasureSpec of the parent ViewGroup. </param>
		public virtual void adjustChildren(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (Log.IsLoggable(TAG, LogPriority.Debug))
			{
				Log.Debug(TAG, "adjustChildren: " + mHost + " widthMeasureSpec: " + View.MeasureSpec.ToString(widthMeasureSpec) + " heightMeasureSpec: " + View.MeasureSpec.ToString(heightMeasureSpec));
			}
			int widthHint = View.MeasureSpec.GetSize(widthMeasureSpec);
			int heightHint = View.MeasureSpec.GetSize(heightMeasureSpec);
			for (int i = 0, N = mHost.ChildCount; i < N; i++)
			{
				View view = mHost.GetChildAt(i);
				ViewGroup.LayoutParams @params = view.LayoutParameters;
				if (Log.IsLoggable(TAG, LogPriority.Debug))
				{
					Log.Debug(TAG, "should adjust " + view + " " + @params);
				}
				if (@params is PercentLayoutParams)
				{
					PercentLayoutInfo info = ((PercentLayoutParams) @params).PercentLayoutInfo;
					if (Log.IsLoggable(TAG, LogPriority.Debug))
					{
						Log.Debug(TAG, "using " + info);
					}
					if (info != null)
					{
						if (@params is ViewGroup.MarginLayoutParams)
						{
							info.fillMarginLayoutParams((ViewGroup.MarginLayoutParams) @params, widthHint, heightHint);
						}
						else
						{
							info.fillLayoutParams(@params, widthHint, heightHint);
						}
					}
				}
			}
		}
		/// <summary>
		/// Constructs a PercentLayoutInfo from attributes associated with a View. Call this method from
		/// {@code LayoutParams(Context c, AttributeSet attrs)} constructor.
		/// </summary>
		public static PercentLayoutInfo getPercentLayoutInfo(Context context, IAttributeSet attrs)
		{
			PercentLayoutInfo info = null;
			TypedArray array = context.ObtainStyledAttributes(attrs, Resource.Styleable.PercentLayout_Layout);
			float value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_widthPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent width: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.widthPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_heightPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent height: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.heightPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_marginPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent margin: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.leftMarginPercent = value;
				info.topMarginPercent = value;
				info.rightMarginPercent = value;
				info.bottomMarginPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_marginLeftPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent left margin: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.leftMarginPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_marginTopPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent top margin: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.topMarginPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_marginRightPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent right margin: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.rightMarginPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_marginBottomPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent bottom margin: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.bottomMarginPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_marginStartPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent start margin: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.startMarginPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_marginEndPercent, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "percent end margin: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.endMarginPercent = value;
			}
			value = array.GetFraction(Resource.Styleable.PercentLayout_Layout_layout_aspectRatio, 1, 1, -1f);
			if (value != -1f)
			{
				if (Log.IsLoggable(TAG, LogPriority.Verbose))
				{
					Log.Verbose(TAG, "aspect ratio: " + value);
				}
				info = info != null ? info : new PercentLayoutInfo();
				info.aspectRatio = value;
			}
			array.Recycle();
			if (Log.IsLoggable(TAG, LogPriority.Debug))
			{
				Log.Debug(TAG, "constructed: " + info);
			}
			return info;
		}
		/// <summary>
		/// Iterates over children and restores their original dimensions that were changed for
		/// percentage values. Calling this method only makes sense if you previously called
		/// <seealso cref="PercentLayoutHelper#adjustChildren(int, int)"/>.
		/// </summary>
		public virtual void restoreOriginalParams()
		{
			for (int i = 0, N = mHost.ChildCount; i < N; i++)
			{
				View view = mHost.GetChildAt(i);
				ViewGroup.LayoutParams @params = view.LayoutParameters;
				if (Log.IsLoggable(TAG, LogPriority.Debug))
				{
					Log.Debug(TAG, "should restore " + view + " " + @params);
				}
				if (@params is PercentLayoutParams)
				{
					PercentLayoutInfo info = ((PercentLayoutParams) @params).PercentLayoutInfo;
					if (Log.IsLoggable(TAG, LogPriority.Debug))
					{
						Log.Debug(TAG, "using " + info);
					}
					if (info != null)
					{
						if (@params is ViewGroup.MarginLayoutParams)
						{
							info.restoreMarginLayoutParams((ViewGroup.MarginLayoutParams) @params);
						}
						else
						{
							info.restoreLayoutParams(@params);
						}
					}
				}
			}
		}
		/// <summary>
		/// Iterates over children and checks if any of them would like to get more space than it
		/// received through the percentage dimension.
		/// 
		/// If you are building a layout that supports percentage dimensions you are encouraged to take
		/// advantage of this method. The developer should be able to specify that a child should be
		/// remeasured by adding normal dimension attribute with {@code wrap_content} value. For example
		/// he might specify child's attributes as {@code app:layout_widthPercent="60%p"} and
		/// {@code android:layout_width="wrap_content"}. In this case if the child receives too little
		/// space, it will be remeasured with width set to {@code WRAP_CONTENT}.
		/// </summary>
		/// <returns> True if the measure phase needs to be rerun because one of the children would like
		/// to receive more space. </returns>
		public virtual bool handleMeasuredStateTooSmall()
		{
			bool needsSecondMeasure = false;
			for (int i = 0, N = mHost.ChildCount; i < N; i++)
			{
				View view = mHost.GetChildAt(i);
				ViewGroup.LayoutParams @params = view.LayoutParameters;
				if (Log.IsLoggable(TAG, LogPriority.Debug))
				{
					Log.Debug(TAG, "should handle measured state too small " + view + " " + @params);
				}
				if (@params is PercentLayoutParams)
				{
					PercentLayoutInfo info = ((PercentLayoutParams) @params).PercentLayoutInfo;
					if (info != null)
					{
						if (shouldHandleMeasuredWidthTooSmall(view, info))
						{
							needsSecondMeasure = true;
							@params.Width = ViewGroup.LayoutParams.WrapContent;
						}
						if (shouldHandleMeasuredHeightTooSmall(view, info))
						{
							needsSecondMeasure = true;
							@params.Height = ViewGroup.LayoutParams.WrapContent;
						}
					}
				}
			}
			if (Log.IsLoggable(TAG, LogPriority.Debug))
			{
				Log.Debug(TAG, "should trigger second measure pass: " + needsSecondMeasure);
			}
			return needsSecondMeasure;
		}
		private static bool shouldHandleMeasuredWidthTooSmall(View view, PercentLayoutInfo info)
		{
			// custom changed
			/*int state = ViewCompat.getMeasuredWidthAndState(view) & ViewCompat.MEASURED_STATE_MASK;
			return state == ViewCompat.MEASURED_STATE_TOO_SMALL && info.widthPercent >= 0 && info.mPreservedParams.Width == ViewGroup.LayoutParams.WrapContent;*/
			return false;
		}
		private static bool shouldHandleMeasuredHeightTooSmall(View view, PercentLayoutInfo info)
		{
			// custom changed
			/*int state = ViewCompat.getMeasuredHeightAndState(view) & ViewCompat.MEASURED_STATE_MASK;
			return state == ViewCompat.MEASURED_STATE_TOO_SMALL && info.heightPercent >= 0 && info.mPreservedParams.Height == ViewGroup.LayoutParams.WrapContent;*/
			return false;
		}
		/// <summary>
		/// Container for information about percentage dimensions and margins. It acts as an extension
		/// for {@code LayoutParams}.
		/// </summary>
		public class PercentLayoutInfo
		{
			public float widthPercent;
			public float heightPercent;
			public float leftMarginPercent;
			public float topMarginPercent;
			public float rightMarginPercent;
			public float bottomMarginPercent;
			public float startMarginPercent;
			public float endMarginPercent;
			public float aspectRatio;
			/* package */	 internal readonly ViewGroup.MarginLayoutParams mPreservedParams;
			public PercentLayoutInfo()
			{
				widthPercent = -1f;
				heightPercent = -1f;
				leftMarginPercent = -1f;
				topMarginPercent = -1f;
				rightMarginPercent = -1f;
				bottomMarginPercent = -1f;
				startMarginPercent = -1f;
				endMarginPercent = -1f;
				mPreservedParams = new ViewGroup.MarginLayoutParams(0, 0);
			}
			/// <summary>
			/// Fills {@code ViewGroup.LayoutParams} dimensions based on percentage values.
			/// </summary>
			public virtual void fillLayoutParams(ViewGroup.LayoutParams @params, int widthHint, int heightHint)
			{
				// Preserve the original layout params, so we can restore them after the measure step.
				mPreservedParams.Width = @params.Width;
				mPreservedParams.Height = @params.Height;
				// We assume that width/height set to 0 means that value was unset. This might not
				// necessarily be true, as the user might explicitly set it to 0. However, we use this
				// information only for the aspect ratio. If the user set the aspect ratio attribute,
				// it means they accept or soon discover that it will be disregarded.
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean widthNotSet = params.Width == 0 && widthPercent < 0;
				bool widthNotSet = @params.Width == 0 && widthPercent < 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean heightNotSet = params.Height == 0 && heightPercent < 0;
				bool heightNotSet = @params.Height == 0 && heightPercent < 0;
				if (widthPercent >= 0)
				{
					@params.Width = (int)(widthHint * widthPercent);
				}
				if (heightPercent >= 0)
				{
					@params.Height = (int)(heightHint * heightPercent);
				}
				if (aspectRatio >= 0)
				{
					if (widthNotSet)
					{
						@params.Width = (int)(@params.Height * aspectRatio);
					}
					if (heightNotSet)
					{
						@params.Height = (int)(@params.Width / aspectRatio);
					}
				}
				if (Log.IsLoggable(TAG, LogPriority.Debug))
				{
					Log.Debug(TAG, "after fillLayoutParams: (" + @params.Width + ", " + @params.Height + ")");
				}
			}
			/// <summary>
			/// Fills {@code ViewGroup.MarginLayoutParams} dimensions and margins based on percentage
			/// values.
			/// </summary>
			public virtual void fillMarginLayoutParams(ViewGroup.MarginLayoutParams @params, int widthHint, int heightHint)
			{
				fillLayoutParams(@params, widthHint, heightHint);
				// Preserver the original margins, so we can restore them after the measure step.
				mPreservedParams.LeftMargin = @params.LeftMargin;
				mPreservedParams.TopMargin = @params.TopMargin;
				mPreservedParams.RightMargin = @params.RightMargin;
				mPreservedParams.BottomMargin = @params.BottomMargin;

				// custom removed
				/*MarginLayoutParams.setMarginStart(mPreservedParams, MarginLayoutParamsCompat.getMarginStart(@params));
				MarginLayoutParamsCompat.setMarginEnd(mPreservedParams, MarginLayoutParamsCompat.getMarginEnd(@params));*/

				if (leftMarginPercent >= 0)
				{
					@params.LeftMargin = (int)(widthHint * leftMarginPercent);
				}
				if (topMarginPercent >= 0)
				{
					@params.TopMargin = (int)(heightHint * topMarginPercent);
				}
				if (rightMarginPercent >= 0)
				{
					@params.RightMargin = (int)(widthHint * rightMarginPercent);
				}
				if (bottomMarginPercent >= 0)
				{
					@params.BottomMargin = (int)(heightHint * bottomMarginPercent);
				}

				// custom removed
				/*if (startMarginPercent >= 0)
					MarginLayoutParamsCompat.setMarginStart(@params, (int)(widthHint * startMarginPercent));
				if (endMarginPercent >= 0)
					MarginLayoutParamsCompat.setMarginEnd(@params, (int)(widthHint * endMarginPercent));*/

				if (Log.IsLoggable(TAG, LogPriority.Debug))
				{
					Log.Debug(TAG, "after fillMarginLayoutParams: (" + @params.Width + ", " + @params.Height + ")");
				}
			}
			public override string ToString()
			{
				return string.Format("PercentLayoutInformation width: {0:F} height {1:F}, margins ({2:F}, {3:F}, " + " {4:F}, {5:F}, {6:F}, {7:F})", widthPercent, heightPercent, leftMarginPercent, topMarginPercent, rightMarginPercent, bottomMarginPercent, startMarginPercent, endMarginPercent);
			}
			/// <summary>
			/// Restores original dimensions and margins after they were changed for percentage based
			/// values. Calling this method only makes sense if you previously called
			/// <seealso cref="PercentLayoutHelper.PercentLayoutInfo#fillMarginLayoutParams"/>.
			/// </summary>
			public virtual void restoreMarginLayoutParams(ViewGroup.MarginLayoutParams @params)
			{
				restoreLayoutParams(@params);
				@params.LeftMargin = mPreservedParams.LeftMargin;
				@params.TopMargin = mPreservedParams.TopMargin;
				@params.RightMargin = mPreservedParams.RightMargin;
				@params.BottomMargin = mPreservedParams.BottomMargin;
				// custom removed
				/*MarginLayoutParamsCompat.setMarginStart(@params, MarginLayoutParamsCompat.getMarginStart(mPreservedParams));
				MarginLayoutParamsCompat.setMarginEnd(@params, MarginLayoutParamsCompat.getMarginEnd(mPreservedParams));*/
			}
			/// <summary>
			/// Restores original dimensions after they were changed for percentage based values. Calling
			/// this method only makes sense if you previously called
			/// <seealso cref="PercentLayoutHelper.PercentLayoutInfo#fillLayoutParams"/>.
			/// </summary>
			public virtual void restoreLayoutParams(ViewGroup.LayoutParams @params)
			{
				@params.Width = mPreservedParams.Width;
				@params.Height = mPreservedParams.Height;
			}
		}
		/// <summary>
		/// If a layout wants to support percentage based dimensions and use this helper class, its
		/// {@code LayoutParams} subclass must implement this interface.
		/// 
		/// Your {@code LayoutParams} subclass should contain an instance of {@code PercentLayoutInfo}
		/// and the implementation of this interface should be a simple accessor.
		/// </summary>
		public interface PercentLayoutParams
		{
			PercentLayoutInfo PercentLayoutInfo {get;}
		}
	}
}