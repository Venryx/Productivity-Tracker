using Android.Content;
using Android.Preferences;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Util;
using Android.OS;
using Android.Runtime;
using Java.Lang;
using Java.Interop;
using Main;

namespace AndroidColorPicker
{
	public class ColorPickerPreference : Preference
	{
		readonly bool supportsAlpha;
		int colorValueAsInt;

		public ColorPickerPreference(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			TypedArray ta = context.ObtainStyledAttributes(attrs, Resource.Styleable.AmbilWarnaPreference);
			supportsAlpha = ta.GetBoolean(Resource.Styleable.AmbilWarnaPreference_supportsAlpha, false);

			WidgetLayoutResource = Resource.Layout.ambilwarna_pref_widget;
		}

		protected override void OnBindView(View view)
		{
			base.OnBindView(view);

			// Set our custom views inside the layout
			var box = view.FindViewById(Resource.Id.ambilwarna_pref_widget_box);
			box?.SetBackgroundColor(Color.ParseColor(colorValueAsInt.ToHexStr()));
		}

		protected override void OnClick()
		{
			var dialog = new ColorPickerDialog(Context, Color.ParseColor(colorValueAsInt.ToHexStr()), supportsAlpha);
			dialog.OnOK += color=>
			{
				if (!CallChangeListener((int)color)) // They don't want the value to be set
					return;
				colorValueAsInt = color;
				PersistInt(colorValueAsInt);
				NotifyChanged();
			};
			dialog.show();
		}
		

		public virtual void forceSetValue(int value)
		{
			colorValueAsInt = value;
			PersistInt(value);
			NotifyChanged();
		}

		protected override Object OnGetDefaultValue(TypedArray a, int index)
		{
			// This preference type's value type is Integer, so we read the default value from the attributes as an Integer.
			return a.GetInteger(index, 0);
		}
		
		protected override void OnSetInitialValue(bool restoreValue, Object defaultValue)
		{
			colorValueAsInt = restoreValue ? GetPersistedInt(colorValueAsInt) : DefaultOrder;
			if (!restoreValue) // if just-set value was not loaded (i.e. was just the default), save the just-set value
				PersistInt(colorValueAsInt);
		}

		// Suppose a client uses this preference type without persisting. We must save the instance state so it is able to, for example, survive orientation changes.
		protected override IParcelable OnSaveInstanceState()
		{
			IParcelable superState = base.OnSaveInstanceState();
			if (Persistent) // No need to save instance state since it's persistent
				return superState;

			SavedState myState = new SavedState(superState);
			myState.value = colorValueAsInt;
			return myState;
		}
		
		protected override void OnRestoreInstanceState(IParcelable state)
		{
			if (state.GetType() != typeof(SavedState))
			{
				// Didn't save state for us in onSaveInstanceState
				base.OnRestoreInstanceState(state);
				return;
			}

			// Restore the instance state
			SavedState myState = (SavedState)state;
			base.OnRestoreInstanceState(myState.SuperState);
			colorValueAsInt = myState.value;
			NotifyChanged();
		}
		
		/// <summary>SavedState, a subclass of <seealso cref = "android.preference.Preference.BaseSavedState" />, will store the
		///		state of MyPreference, a subclass of Preference. <para>It is important to always call through to super methods.</para></summary>
		class SavedState : BaseSavedState
		{
			public SavedState(IParcelable superState) : base(superState) {}
			public SavedState(Parcel source) : base(source) { value = source.ReadInt(); }
			internal int value;
			public override void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
			{
				base.WriteToParcel(dest, flags);
				dest.WriteInt(value);
			}

			//@SuppressWarnings("unused") public static readonly Creator<SavedState> CREATOR = new SavedStateCreator();
			//[ExportField("CREATOR")] public static SavedStateCreator InitializeCreator() { return new SavedStateCreator(); }
			[Register("CREATOR")] public new static IParcelableCreator Creator=>new SavedStateCreator();
			public class SavedStateCreator : Object, IParcelableCreator
			{
				public virtual Object CreateFromParcel(Parcel @in) { return new SavedState(@in); }
				public virtual Object[] NewArray(int size) { return new SavedState[size]; }
			}
		}
	}
}