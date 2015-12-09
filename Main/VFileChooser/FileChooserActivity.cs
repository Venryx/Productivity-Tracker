using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.IO;
using Main;

namespace VFileChooser
{
	/// <summary>A file chooser implemented in an Activity.</summary>
	public class FileChooserActivity : Activity, FileChooser
	{
		// fields
		// ==========

		/// <summary>The folder that the class opened by default.</summary>
		private File startFolder;
		/// <summary>The core of the file chooser.</summary>
		private FileChooserCore core;
		/// <summary>A boolean indicating if the 'back' button must be used to navigate to parent folders.</summary>
		private bool useBackButton;

		// constants
		// ==========

		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains the path of the folder which files are going to be listed.</summary>
		public const string INPUT_START_FOLDER = "input_start_folder";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains a boolean that indicates if the user is going to select folders instead of select files.</summary>
		public const string INPUT_FOLDER_MODE = "input_folder_mode";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains a boolean that indicates if the user can create files.</summary>
		public const string INPUT_CAN_CREATE_FILES = "input_can_create_files";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains a regular expression which is going to be used as a filter to determine which files can be selected.</summary>
		public const string INPUT_REGEX_FILTER = "input_regex_filter";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains a boolean that indicates if only the files that can be selected must be displayed.</summary>
		public const string INPUT_SHOW_ONLY_SELECTABLE = "input_show_only_selectable";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains an instance of the class FileChooserLabels that allows to override the default value of the labels.</summary>
		public const string INPUT_LABELS = "input_labels";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains a boolean that indicates if a confirmation dialog must be displayed when creating a file.</summary>
		public const string INPUT_SHOW_CONFIRMATION_ON_CREATE = "input_show_confirmation_on_create";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains a boolean that indicates if a confirmation dialog must be displayed when selecting a file.</summary>
		public const string INPUT_SHOW_CONFIRMATION_ON_SELECT = "input_show_confirmation_on_select";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains a boolean that indicates if the title must show the full path of the current's folder (true) or only the folder's name (false).</summary>		/// </summary>
		public const string INPUT_SHOW_FULL_PATH_IN_TITLE = "input_show_full_path_in_title";
		/// <summary>Constant used for represent the key of the bundle object (inside the start's intent) which contains a boolean that indicates if the 'Back' button must be used to navigate to the parents folder (true) or if must follow the default behavior (and close the activity when the button is pressed).</summary>		/// </summary>
		public const string INPUT_USE_BACK_BUTTON_TO_NAVIGATE = "input_use_back_button_to_navigate";
		/// <summary>Constant used for represent the key of the bundle object (inside the result's intent) which contains the File object, that represents the file selected by the user or the folder in which the user wants to create a file.</summary>		/// </summary>
		public const string OUTPUT_FILE_OBJECT = "output_file_object";
		/// <summary>Constant used for represent the key of the bundle object (inside the result's intent) which contains the name of the file that the user wants to create.</summary>
		public const string OUTPUT_NEW_FILE_NAME = "output_new_file_name";

		// activity methods
		// ==========

		/// <summary>Called when the activity is first created.</summary>
		protected override void OnCreate(Bundle savedInstanceState)
		{
			// Call superclass creator.
			base.OnCreate(savedInstanceState);

			// Set layout.
			//this.ContentView = R.layout.daidalos_file_chooser;
			SetContentView(Resource.Layout.daidalos_file_chooser);

			// Set the background color.
			LinearLayout layout = FindViewById<LinearLayout>(Resource.Id.rootLayout);
			//layout.BackgroundColor = Resources.getColor(R.color.daidalos_backgroud);
			layout.SetBackgroundColor(Resources.GetColor(Resource.Color.daidalos_backgroud, null));

			// Initialize fields.
			useBackButton = false;

			// Create the core of the file chooser.
			core = new FileChooserCore(this);

			// Verify if the optional parameters has been defined.
			string folderPath = null;
			Bundle extras = Intent.Extras;
			if (extras != null)
			{
				if (extras.ContainsKey(INPUT_START_FOLDER))
					folderPath = extras.GetString(INPUT_START_FOLDER);
				if (extras.ContainsKey(INPUT_REGEX_FILTER))
					core.Filter = extras.GetString(INPUT_REGEX_FILTER);
				if (extras.ContainsKey(INPUT_SHOW_ONLY_SELECTABLE))
					core.ShowOnlySelectable = extras.GetBoolean(INPUT_SHOW_ONLY_SELECTABLE);
				if (extras.ContainsKey(INPUT_FOLDER_MODE))
					core.FolderMode = extras.GetBoolean(INPUT_FOLDER_MODE);
				if (extras.ContainsKey(INPUT_CAN_CREATE_FILES))
					core.CanCreateFiles = extras.GetBoolean(INPUT_CAN_CREATE_FILES);
				if (extras.ContainsKey(INPUT_LABELS))
					//core.Labels = (FileChooserLabels)extras.Get(INPUT_LABELS);
					core.Labels = (FileChooserLabels)(object)extras.Get(INPUT_LABELS);
				if (extras.ContainsKey(INPUT_SHOW_CONFIRMATION_ON_CREATE))
					core.ShowConfirmationOnCreate = extras.GetBoolean(INPUT_SHOW_CONFIRMATION_ON_CREATE);
				if (extras.ContainsKey(INPUT_SHOW_CONFIRMATION_ON_SELECT))
					core.ShowConfirmationOnSelect = extras.GetBoolean(INPUT_SHOW_CONFIRMATION_ON_SELECT);
				if (extras.ContainsKey(INPUT_SHOW_FULL_PATH_IN_TITLE))
					core.ShowFullPathInTitle = extras.GetBoolean(INPUT_SHOW_FULL_PATH_IN_TITLE);
				if (extras.ContainsKey(INPUT_USE_BACK_BUTTON_TO_NAVIGATE))
					useBackButton = extras.GetBoolean(INPUT_USE_BACK_BUTTON_TO_NAVIGATE);
			}

			// Load the files of a folder.
			core.loadFolder(folderPath);
			startFolder = core.CurrentFolder;

			// Add a listener for when a file is selected.
			core.addListener(new OnFileSelectedListenerAnonymousInnerClassHelper(this));
		}

        private class OnFileSelectedListenerAnonymousInnerClassHelper : FileChooserCore.OnFileSelectedListener
		{
			public OnFileSelectedListenerAnonymousInnerClassHelper(FileChooserActivity outerInstance) { this.outerInstance = outerInstance; }

			private readonly FileChooserActivity outerInstance;
			public virtual void OnFileSelected(File folder, string name)
			{
				// Pass the data through an intent.
				Intent intent = new Intent();
				Bundle bundle = new Bundle();
				bundle.PutSerializable(OUTPUT_FILE_OBJECT, folder);
				bundle.PutString(OUTPUT_NEW_FILE_NAME, name);
				intent.PutExtras(bundle);

				outerInstance.SetResult(Result.Ok, intent);
				outerInstance.Finish();
			}
			public virtual void OnFileSelected(File file)
			{
				// Pass the data through an intent.
				Intent intent = new Intent();
				Bundle bundle = new Bundle();
				bundle.PutSerializable(OUTPUT_FILE_OBJECT, file);
				intent.PutExtras(bundle);

				outerInstance.SetResult(Result.Ok, intent);
				outerInstance.Finish();
			}
		}

		/// <summary>Called when the user push the 'back' button.</summary>
		public override void OnBackPressed()
		{
			// verify if the activity must be finished or if the parent folder must be opened
			File current = core.CurrentFolder;
			if (!useBackButton || current == null || current.Parent == null || current.Path.CompareTo(startFolder.Path) == 0)
				base.OnBackPressed(); // close activity
			else
				core.loadFolder(current.Parent); // open parent
		}
		
		// FileChooser methods
		// ==========

		public virtual LinearLayout RootLayout { get { return FindViewById(Resource.Id.rootLayout) as LinearLayout; } }
		//public virtual Context Context { get { return this.getBaseContext(); } }
		public virtual Context Context { get { return this; } }
		public virtual string CurrentFolderName { set { Title = value; } }
	}
}