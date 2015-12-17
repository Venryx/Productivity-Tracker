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
using Android.Widget;
using Android.Util;
using Android.Views;
using Android.Content.Res;

namespace android.support.percent
{
	/// <summary>
	/// Subclass of <seealso cref="android.widget.FrameLayout"/> that supports percentage based dimensions and
	/// margins.
	/// 
	/// You can specify dimension or a margin of child by using attributes with "Percent" suffix. Follow
	/// this example:
	/// 
	/// <pre class="prettyprint">
	/// &lt;android.support.percent.PercentFrameLayout
	///         xmlns:android="http://schemas.android.com/apk/res/android"
	///         xmlns:app="http://schemas.android.com/apk/res-auto"
	///         android:layout_width="match_parent"
	///         android:layout_height="match_parent"&gt
	///     &lt;ImageView
	///         app:layout_widthPercent="50%"
	///         app:layout_heightPercent="50%"
	///         app:layout_marginTopPercent="25%"
	///         app:layout_marginLeftPercent="25%"/&gt
	/// &lt;/android.support.percent.PercentFrameLayout/&gt
	/// </pre>
	/// 
	/// The attributes that you can use are:
	/// <ul>
	///     <li>{@code layout_widthPercent}
	///     <li>{@code layout_heightPercent}
	///     <li>{@code layout_marginPercent}
	///     <li>{@code layout_marginLeftPercent}
	///     <li>{@code layout_marginTopPercent}
	///     <li>{@code layout_marginRightPercent}
	///     <li>{@code layout_marginBottomPercent}
	///     <li>{@code layout_marginStartPercent}
	///     <li>{@code layout_marginEndPercent}
	///     <li>{@code layout_aspectRatio}
	/// </ul>
	/// 
	/// It is not necessary to specify {@code layout_width/height} if you specify {@code
	/// layout_widthPercent.} However, if you want the view to be able to take up more space than what
	/// percentage value permits, you can add {@code layout_width/height="wrap_content"}. In that case
	/// if the percentage size is too small for the View's content, it will be resized using
	/// {@code wrap_content} rule.
	/// 
	/// <para>
	/// You can also make one dimension be a fraction of the other by setting only width or height and
	/// using {@code layout_aspectRatio} for the second one to be calculated automatically. For
	/// example, if you would like to achieve 16:9 aspect ratio, you can write:
	/// <pre class="prettyprint">
	///     android:layout_width="300dp"
	///     app:layout_aspectRatio="178%"
	/// </pre>
	/// This will make the aspect ratio 16:9 (1.78:1) with the width fixed at 300dp and height adjusted
	/// accordingly.
	/// </para>
	/// </summary>
	public class PercentFrameLayout : FrameLayout
	{
		bool InstanceFieldsInitialized;

		void InitializeInstanceFields() { mHelper = new PercentLayoutHelper(this); }

		PercentLayoutHelper mHelper;
		public PercentFrameLayout(Context context) : base(context)
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}
		public PercentFrameLayout(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}
		public PercentFrameLayout(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		}
		// custom removed (had to, for it to compile)
		/*protected override ViewGroup.LayoutParams GenerateDefaultLayoutParams() { return new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent); }
		public override ViewGroup.LayoutParams GenerateLayoutParams(IAttributeSet attrs) { return new LayoutParams(Context, attrs); }*/
		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			mHelper.adjustChildren(widthMeasureSpec, heightMeasureSpec);
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			if (mHelper.handleMeasuredStateTooSmall())
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}
		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayout(changed, left, top, right, bottom);
			mHelper.restoreOriginalParams();
		}
		public class LayoutParams : FrameLayout.LayoutParams, PercentLayoutHelper.PercentLayoutParams
		{
			internal PercentLayoutHelper.PercentLayoutInfo mPercentLayoutInfo;
			public LayoutParams(Context c, IAttributeSet attrs) : base(c, attrs) { mPercentLayoutInfo = PercentLayoutHelper.getPercentLayoutInfo(c, attrs); }
			public LayoutParams(int width, int height) : base(width, height) {}
			public LayoutParams(int width, int height, GravityFlags gravity) : base(width, height, gravity) {}
			public LayoutParams(ViewGroup.LayoutParams source) : base(source) {}
			public LayoutParams(MarginLayoutParams source) : base(source) {}
			public LayoutParams(FrameLayout.LayoutParams source) : base((MarginLayoutParams)source) { Gravity = source.Gravity; }
			public LayoutParams(LayoutParams source) : this((FrameLayout.LayoutParams)source) { mPercentLayoutInfo = source.mPercentLayoutInfo; }
			public virtual PercentLayoutHelper.PercentLayoutInfo PercentLayoutInfo
			{
				get
				{
					if (mPercentLayoutInfo == null)
						mPercentLayoutInfo = new PercentLayoutHelper.PercentLayoutInfo();
					return mPercentLayoutInfo;
				}
			}
			protected override void SetBaseAttributes(TypedArray a, int widthAttr, int heightAttr) { PercentLayoutHelper.fetchWidthAndHeight(this, a, widthAttr, heightAttr); }
		}
	}
}