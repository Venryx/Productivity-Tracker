using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using Java.IO;
using Main;

namespace VFileChooser
{
	/// <summary>This class is used to represents the files that can be selected by the user.</summary>
	public class FileItem : LinearLayout
	{
		// attributes
		// ==========

		/// <summary>The file which is represented by this item.</summary>
		File file;

		/// <summary>The image in which show the file's icon.</summary>
		ImageView icon;

		/// <summary>The label in which show the file's name.</summary>
		TextView label;

		/// <summary>A boolean indicating if the item can be selected.</summary>
		bool selectable;

		/// <summary>The listeners for the click event.</summary>
		IList<OnFileClickListener> listeners;

		// constructor
		// ==========

		/// <summary>The class main constructor.</summary>
		/// <param name = "context"> The application's context. </param>
		public FileItem(Context context) : base(context)
		{
			// define the layout
			LayoutInflater inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
			inflater.Inflate(Resource.Layout.daidalos_file_item, this, true);

			// initialize attributes
			file = null;
			selectable = true;
			icon = FindViewById< ImageView>(Resource.Id.imageViewIcon);
			label = FindViewById< TextView>(Resource.Id.textViewLabel);
			listeners = new List<OnFileClickListener>();

			var fileItem = this;

			// add a listener for the click event
			Click += (sender, e)=>
			{
				//var v = (View)sender;

				// verify if the item can be selected
				if (selectable)
					foreach(var listener in listeners) // call the listeners
						listener(fileItem);
			};
		}

		/// <summary>A class constructor.</summary>
		/// <param name = "context"> The application's context. </param>
		/// <param name = "file"> The file represented by this item </param>
		public FileItem(Context context, File file) : this(context)
		{
			File = file; // set the file
		}

		/// <summary>A class constructor.</summary>
		/// <param name = "context"> The application's context. </param>
		/// <param name = "file"> The file represented by this item. </param>
		/// <param name = "label"> The label of this item. </param>
		public FileItem(Context context, File file, string label) : this(context, file)
		{
			Label = label; // set the label.
		}

		// Get() and Set() methods
		// ==========

		/// <summary>Defines the file represented by this item.</summary>
		/// <param name = "file">A file.</param>
		public virtual File File
		{
			set
			{
				if (value != null)
				{
					file = value;

					// Replace the label by the value's name.
					Label = value.Name;

					// Change the icon, depending if the value is a folder or not.
					updateIcon();
				}
			}
			get { return file; }
		}

		/// <summary> Changes the label of this item, which by default is the file's name. This method must be called after invoking the method setFile(), otherwise the label is going to be overwritten with the file's name.</summary>
		/// <param name="label">A string value.</param>
		public virtual string Label
		{
			set
			{
				// Verify if 'value' is not null.
				if (value == null)
					value = "";

				// Change the value.
				label.Text = value;
			}
		}

		/// <summary>Verifies if the item can be selected.</summary>
		/// <returns> 'true' if the item can be selected, 'false' if not. </returns>
		public virtual bool Selectable
		{
			get { return selectable; }
			set
			{
				// Save the value.
				selectable = value;

				// Update the icon.
				updateIcon();
			}
		}

		// miscellaneous methods
		// ==========

		/// <summary>Updates the icon according to if the file is a folder and if it can be selected.</summary>
		void updateIcon()
		{
			// Define the icon.
			int icon = Resource.Drawable.document_gray;
			if (selectable)
				icon = (file != null && file.IsDirectory) ? Resource.Drawable.folder : Resource.Drawable.document;

			// Set the icon.
			this.icon.SetImageDrawable(Resources.GetDrawable(icon, null));

			// Change the color of the text.
			if (icon != Resource.Drawable.document_gray)
				//label.SetTextColor(Resources.GetColor(Resource.Color.daidalos_active_file, null));
				label.SetTextColor(Resources.GetColor(Resource.Color.daidalos_active_file));
			else
				//label.SetTextColor(Resources.GetColor(Resource.Color.daidalos_inactive_file, null));
				label.SetTextColor(Resources.GetColor(Resource.Color.daidalos_inactive_file));
		}

		// events
		// ==========

		/// <summary>Add a listener for the click event.</summary>
		/// <param name="listener">The listener to add.</param>
		public virtual void addListener(OnFileClickListener listener) { listeners.Add(listener); }

		/// <summary>Removes a listener for the click event.</summary>
		/// <param name="listener">The listener to remove.</param>
		public virtual void removeListener(OnFileClickListener listener) { listeners.Remove(listener); }

		/// <summary>Removes all the listeners for the click event.</summary>
		public virtual void removeAllListeners() { listeners.Clear(); }

		/// <summary>Signature for callback when a FileItem is clicked.</summary>
		/// <param name="source">The source of the event.</param>
		public delegate void OnFileClickListener(FileItem source);
	}
}