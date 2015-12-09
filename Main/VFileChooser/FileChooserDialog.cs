using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Java.IO;
using Main;

namespace VFileChooser
{
	/// <summary>A file chooser implemented in a Dialog.</summary>
	public class FileChooserDialog : Dialog, FileChooser
	{
		// attributes
		// ==========

		/// <summary>The core of this file chooser.</summary>
		FileChooserCore core;

		/// <summary>The listeners for the event of select a file.</summary>
		IList<OnFileSelectedListener> listeners;

		// constructors
		// ==========

		/// <summary>Creates a file chooser dialog which, by default, lists all the files in the SD card.</summary>
		/// <param name = "context"> The current context. </param>
		public FileChooserDialog(Context context) : this(context, null) { }

		/// <summary>Creates a file chooser dialog which lists all the file of a particular folder.</summary>
		/// <param name = "context">The current context.</param>
		/// <param name = "folderPath">The folder which files are going to be listed.</param>
		public FileChooserDialog(Context context, string folderPath) : base(context)
		{
			// Call superclass constructor.

			// Set layout.
			SetContentView(Resource.Layout.daidalos_file_chooser);

			// Maximize the dialog.
			WindowManagerLayoutParams lp = new WindowManagerLayoutParams();
			lp.CopyFrom(Window.Attributes);
			lp.Width = ViewGroup.LayoutParams.MatchParent;
			lp.Height = ViewGroup.LayoutParams.MatchParent;
			Window.Attributes = lp;

			// By default, load the SD card files.
			core = new FileChooserCore(this);
			core.loadFolder(folderPath);

			// Initialize attributes.
			listeners = new List<OnFileSelectedListener>();

			// Set the background color.
			LinearLayout layout = FindViewById<LinearLayout>(Resource.Id.rootLayout);
			layout.SetBackgroundColor(context.Resources.GetColor(Resource.Color.daidalos_backgroud, null));

			// Add a listener for when a file is selected.
			core.addListener(new OnFileSelectedListenerAnonymousInnerClassHelper(this));
		}

		class OnFileSelectedListenerAnonymousInnerClassHelper : FileChooserCore.OnFileSelectedListener
		{
			readonly FileChooserDialog outerInstance;

			public OnFileSelectedListenerAnonymousInnerClassHelper(FileChooserDialog outerInstance) { this.outerInstance = outerInstance; }

			public virtual void OnFileSelected(File folder, string name)
			{
				// call to the listeners
				for (int i = 0; i < outerInstance.listeners.Count; i++)
					outerInstance.listeners[i].onFileSelected(outerInstance, folder, name);
			}
			public virtual void OnFileSelected(File file)
			{
				// call to the listeners
				for (int i = 0; i < outerInstance.listeners.Count; i++)
					outerInstance.listeners[i].onFileSelected(outerInstance, file);
			}
		}

		// events methods
		// ==========

		/// <summary>Add a listener for the event of a file selected.</summary>
		/// <param name = "listener"> The listener to add. </param>
		public virtual void addListener(OnFileSelectedListener listener) { listeners.Add(listener); }

		/// <summary>Removes a listener for the event of a file selected.</summary>
		/// <param name = "listener"> The listener to remove. </param>
		public virtual void removeListener(OnFileSelectedListener listener) { listeners.Remove(listener); }

		/// <summary>Removes all the listeners for the event of a file selected.</summary>
		public virtual void removeAllListeners() { listeners.Clear(); }

		/// <summary>Interface definition for a callback to be invoked when a file is selected.</summary>
		public interface OnFileSelectedListener
		{
			/// <summary>Called when a file has been selected.</summary>
			/// <param name = "file"> The file selected. </param>
			void onFileSelected(Dialog source, File file);

			/// <summary>Called when an user wants to be create a file.</summary>
			/// <param name = "folder"> The file's parent folder. </param>
			/// <param name = "name"> The file's name. </param>
			void onFileSelected(Dialog source, File folder, string name);
		}

		// miscellaneous methods
		// ==========

		/// <summary>Set a regular expression to filter the files that can be selected.</summary>
		/// <param name = "filter">A regular expression.</param>
		public virtual string Filter { set { core.Filter = value; } }

		/// <summary>Defines if only the files that can be selected (they pass the filter) must be show.</summary>
		/// <param name = "show">'true' if only the files that can be selected must be show or 'false' if all the files must be show.</param>
		public virtual bool ShowOnlySelectable { set { core.ShowOnlySelectable = value; } }

		/// <summary>Loads all the files of the SD card root.</summary>
		public virtual void loadFolder() { core.loadFolder(); }

		/// <summary>Loads all the files of a folder in the file chooser. If no path is specified ('folderPath' is null) the root folder of the SD card is going to be used.</summary>
		/// <param name = "folderPath">The folder's path.</param>
		public virtual void loadFolder(string folderPath) { core.loadFolder(folderPath); }

		/// <summary>Defines if the chooser is going to be used to select folders, instead of files.</summary>
		/// <param name = "folderMode"> 'true' for select folders or 'false' for select files. </param>
		public virtual bool FolderMode { set { core.FolderMode = value; } }

		/// <summary>Defines if the user can create files, instead of only select files.</summary>
		/// <param name = "canCreate"> 'true' if the user can create files or 'false' if it can only select them.</param>
		public virtual bool CanCreateFiles { set { core.CanCreateFiles = value; } }

		/// <summary>Defines the value of the labels.</summary>
		/// <param name = "label">The labels.</param>
		public virtual FileChooserLabels Labels { set { core.Labels = value; } }

		/// <summary>Allows to define if a confirmation dialog must be show when selecting o creating a file.</summary>
		/// <param name = "onSelect">'true' for show a confirmation dialog when selecting a file, 'false' if not.</param>
		/// <param name = "onCreate">'true' for show a confirmation dialog when creating a file, 'false' if not.</param>
		public virtual void setShowConfirmation(bool onSelect, bool onCreate)
		{
			core.ShowConfirmationOnCreate = onCreate;
			core.ShowConfirmationOnSelect = onSelect;
		}

		/// <summary>Allows to define if, in the title, must be show only the current folder's name or the full file's path.</summary>
		/// <param name = "show"> 'true' for show the full path, 'false' for show only the name. </param>
		public virtual bool ShowFullPath { set { core.ShowFullPathInTitle = value; } }

		// FileChooser methods
		// ==========

		public virtual LinearLayout RootLayout { get { return FindViewById(Resource.Id.rootLayout) as LinearLayout; } }

		public virtual string CurrentFolderName { set { SetTitle(value); } }
	}
}