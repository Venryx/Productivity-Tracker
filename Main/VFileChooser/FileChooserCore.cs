using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Util;
using Main;

namespace VFileChooser
{
	/// <summary>This class implements the common features of a file chooser.</summary>
	class FileChooserCore
	{
		// attributes
		// ==========

		/// <summary>The file chooser in which all the operations are performed.</summary>
		FileChooser chooser;

		/// <summary>The listeners for the event of select a file.</summary>
		List<Action<File, bool>> listeners;

		/// <summary>A regular expression for filter the files.</summary>
		string filter;

		/// <summary>A boolean indicating if only the files that can be selected (they pass the filter) will be shown.</summary>
		bool showOnlySelectable;

		/// <summary>A boolean indicating if the user can create files.</summary>
		bool canCreateFiles;

		/// <summary>A boolean indicating if the chooser is going to be used to select folders.</summary>
		bool folderMode;

		/// <summary>A file that indicates the folder that is currently being displayed.</summary>
		File currentFolder;

		/// <summary>This attribut allows to override the default value of the labels.</summary>
		FileChooserLabels labels;

		/// <summary>A boolean that indicates if a confirmation dialog must be displaying when selecting a file.</summary>
		bool showConfirmationOnSelect;

		/// <summary>A boolean that indicates if a confirmation dialog must be displaying when creating a file.</summary>
		bool showConfirmationOnCreate;

		/// <summary>A boolean indicating if the folder's full path must be show in the title.</summary>
		bool showFullPathInTitle;

		// ---- Static attributes ----- //

		/// <summary>Static attribute for save the folder displayed by default.</summary>
		static File defaultFolder;

		/// <summary>Static constructor.</summary>
		static FileChooserCore() { defaultFolder = null; }

		// ----- Constructor ----- //

		/// <summary>Creates an instance of this class.</summary>
		/// <param name = "fileChooser"> The graphical file chooser. </param>
		public FileChooserCore(FileChooser fileChooser)
		{
			// Initialize attributes.
			chooser = fileChooser;
			//listeners = new LinkedList<OnFileSelectedListener>();
			listeners = new List<Action<File, bool>>();
			filter = null;
			showOnlySelectable = false;
			CanCreateFiles = false;
			FolderMode = false;
			currentFolder = null;
			labels = null;
			showConfirmationOnCreate = false;
			showConfirmationOnSelect = false;
			showFullPathInTitle = false;

			// Add listener for the  buttons.
			LinearLayout root = chooser.RootLayout;
			Button addButton = root.FindViewById<Button>(Resource.Id.buttonAdd);
			var core = this;
			addButton.Click += (sender, e)=>
			{
				View v = (View)sender;

				// Get the current context.
				Context context = v.Context;

				// Create an alert dialog.
				AlertDialog.Builder alert = new AlertDialog.Builder(context);

				// Define the dialog's labels.
				string title = context.GetString(core.folderMode ? Resource.String.daidalos_create_folder : Resource.String.daidalos_create_file);
				if (core.labels != null && core.labels.createFileDialogTitle != null)
					title = core.labels.createFileDialogTitle;
				string message = context.GetString(core.folderMode ? Resource.String.daidalos_enter_folder_name : Resource.String.daidalos_enter_file_name);
				if (core.labels != null && core.labels.createFileDialogMessage != null)
					message = core.labels.createFileDialogMessage;
				string posButton = (core.labels != null && core.labels.createFileDialogAcceptButton != null) ? core.labels.createFileDialogAcceptButton : context.GetString(Resource.String.daidalos_accept);
				string negButton = (core.labels != null && core.labels.createFileDialogCancelButton != null) ? core.labels.createFileDialogCancelButton : context.GetString(Resource.String.daidalos_cancel);

				// Set the title and the message.
				alert.SetTitle(title);
				alert.SetMessage(message);

				// Set an EditText view to get the file's name.
				//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
				//ORIGINAL LINE: final android.widget.EditText input = new android.widget.EditText(context);
				EditText input = new EditText(context);
				input.SetSingleLine();
				alert.SetView(input);

				// Set the 'ok' and 'cancel' buttons.
				alert.SetPositiveButton(posButton, (sender2, e2)=>
				{
					//int whichButton = e2.Which;

					string fileName = input.Text.ToString();
					// Verify if a value has been entered.
					if (fileName != null && fileName.Length > 0)
						// Notify the listeners.
						core.notifyListeners(core.currentFolder, fileName);
				});
				alert.SetNegativeButton(negButton, (sender2, e2)=>
				{
					//int whichButton = e2.Which;
					// do nothing; the dialog will close automatically
				});

				alert.Show();
			};
			var okButton = root.FindViewById<Button>(Resource.Id.buttonOk);
			okButton.Click += (sender, e)=> { notifyListeners(currentFolder, null); }; // notify the listeners
		}

		// events methods
		// ==========

		/// <summary>Add a listener for the event of a file selected.</summary>
		/// <param name = "listener"> The listener to add. </param>
		public virtual void addListener(Action<File, bool> listener) { listeners.Add(listener); }

		/// <summary>Removes a listener for the event of a file selected.</summary>
		/// <param name = "listener"> The listener to remove. </param>
		public virtual void removeListener(Action<File, bool> listener) { listeners.Remove(listener); }

		/// <summary>Removes all the listeners for the event of a file selected.</summary>
		public virtual void removeAllListeners() { listeners.Clear(); }

		/// <summary>Notify to all listeners that a file has been selected or created.</summary>
		/// <param name = "file"> The file or folder selected or the folder in which the file must be created. </param>
		/// <param name = "name"> The name of the file that must be created or 'null' if a file was selected (instead of being
		///     created). </param>
		//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		//ORIGINAL LINE: private void notifyListeners(final File file, final String name)
		void notifyListeners(File fileOrFolder, string name)
		{
			var file = fileOrFolder.IsDirectory ? fileOrFolder.GetFile(name) : fileOrFolder;
			// Determine if a file has been selected or created.
			//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
			//ORIGINAL LINE: final boolean creation = name != null && name.length() > 0;
			bool creation = name != null && name.Length > 0;

			// Verify if a confirmation dialog must be show.
			if ((creation && showConfirmationOnCreate || !creation && showConfirmationOnSelect))
			{
				// Create an alert dialog.
				Context context = chooser.Context;
				AlertDialog.Builder alert = new AlertDialog.Builder(context);

				// Define the dialog's labels.
				string message = null;
				if (labels != null && ((creation && labels.messageConfirmCreation != null) || (!creation && labels.messageConfirmSelection != null)))
					message = creation ? labels.messageConfirmCreation : labels.messageConfirmSelection;
				else if (folderMode)
					message = context.GetString(creation ? Resource.String.daidalos_confirm_create_folder : Resource.String.daidalos_confirm_select_folder);
				else
					message = context.GetString(creation ? Resource.String.daidalos_confirm_create_file : Resource.String.daidalos_confirm_select_file);
				if (message != null)
					message = message.Replace("$file_name", name != null ? name : file.Name);
				string posButton = (labels != null && labels.labelConfirmYesButton != null) ? labels.labelConfirmYesButton : context.GetString(Resource.String.daidalos_yes);
				string negButton = (labels != null && labels.labelConfirmNoButton != null) ? labels.labelConfirmNoButton : context.GetString(Resource.String.daidalos_no);

				// Set the message and the 'yes' and 'no' buttons.
				alert.SetMessage(message);
				alert.SetPositiveButton(posButton, (sender, e)=>
				{
					foreach (var listener in listeners) // notify to listeners
						listener(file, creation);
				});
				alert.SetNegativeButton(negButton, (sender, e)=>
				{
					//int whichButton = e.Which;
					// do nothing; the dialog will close automatically
				});

				alert.Show();
			}
			else
				foreach (var listener in listeners) // notify to listeners
					listener(file, creation);
		}

		// get and set methods
		// ==========

		/// <summary>Allows to define if a confirmation dialog must be show when selecting a file.</summary>
		/// <param name = "show"> 'true' for show the confirmation dialog, 'false' for not show the dialog. </param>
		public virtual bool ShowConfirmationOnSelect { set { showConfirmationOnSelect = value; } }

		/// <summary>Allows to define if a confirmation dialog must be show when creating a file.</summary>
		/// <param name = "show"> 'true' for show the confirmation dialog, 'false' for not show the dialog. </param>
		public virtual bool ShowConfirmationOnCreate { set { showConfirmationOnCreate = value; } }

		/// <summary>Allows to define if, in the title, must be show only the current folder's name or the full file's path..</summary>
		/// <param name = "show"> 'true' for show the full path, 'false' for show only the name. </param>
		public virtual bool ShowFullPathInTitle { set { showFullPathInTitle = value; } }

		/// <summary>Defines the value of the labels.</summary>
		/// <param name = "label"> The labels. </param>
		public virtual FileChooserLabels Labels
		{
			set
			{
				labels = value;

				// Verify if the buttons for add a file or select a folder has been modified.
				if (value != null)
				{
					LinearLayout root = chooser.RootLayout;

					if (value.labelAddButton != null)
					{
						Button addButton = root.FindViewById< Button>(Resource.Id.buttonAdd);
						addButton.Text = value.labelAddButton;
					}

					if (value.labelSelectButton != null)
					{
						Button okButton = root.FindViewById<Button>(Resource.Id.buttonOk);
						okButton.Text = value.labelSelectButton;
					}
				}
			}
		}

		/// <summary>Set a regular expression to filter the files that can be selected.</summary>
		/// <param name = "filter"> A regular expression. </param>
		public virtual string Filter
		{
			set
			{
				if (value == null || value.Length == 0)
					filter = null;
				else
					filter = value;

				// Reload the list of files.
				loadFolder(currentFolder);
			}
		}

		/// <summary>Defines if the chooser is going to be used to select folders, instead of files.</summary>
		/// <param name = "folderMode"> 'true' for select folders or 'false' for select files. </param>
		public virtual bool FolderMode
		{
			set
			{
				folderMode = value;

				// Show or hide the 'Ok' button.
				updateButtonsLayout();

				// Reload the list of files.
				loadFolder(currentFolder);
			}
		}

		/// <summary>Defines if the user can create files, instead of only select files.</summary>
		/// <param name = "canCreate"> 'true' if the user can create files or 'false' if it can only select them. </param>
		public virtual bool CanCreateFiles
		{
			set
			{
				canCreateFiles = value;

				// Show or hide the 'Add' button.
				updateButtonsLayout();
			}
		}

		/// <summary>Defines if only the files that can be selected (they pass the filter) must be show.</summary>
		/// <param name = "show"> 'true' if only the files that can be selected must be show or 'false' if all the files must be
		///     show. </param>
		public virtual bool ShowOnlySelectable
		{
			set
			{
				showOnlySelectable = value;

				// Reload the list of files.
				loadFolder(currentFolder);
			}
		}

		/// <summary>Returns the current folder.</summary>
		/// <returns> The current folder. </returns>
		public virtual File CurrentFolder { get { return currentFolder; } }

		// miscellaneous methods
		// ==========

		/// <summary>Changes the height of the layout for the buttons, according if the buttons are visible or not.</summary>
		void updateButtonsLayout()
		{
			// Get the buttons layout.
			LinearLayout root = chooser.RootLayout;
			LinearLayout buttonsLayout = root.FindViewById<LinearLayout>(Resource.Id.linearLayoutButtons);

			// Verify if the 'Add' button is visible or not.
			View addButton = root.FindViewById(Resource.Id.buttonAdd);
			addButton.Visibility = canCreateFiles ? ViewStates.Visible : ViewStates.Invisible;
			addButton.LayoutParameters.Width = canCreateFiles ? ViewGroup.LayoutParams.MatchParent : 0;

			// Verify if the 'Ok' button is visible or not.
			View okButton = root.FindViewById(Resource.Id.buttonOk);
			okButton.Visibility = folderMode ? ViewStates.Visible : ViewStates.Invisible;
			okButton.LayoutParameters.Width = folderMode ? ViewGroup.LayoutParams.MatchParent : 0;

			// If both buttons are invisible, hide the layout.
			ViewGroup.LayoutParams @params = buttonsLayout.LayoutParameters;
			if (canCreateFiles || folderMode)
			{
				// Show the layout.
				@params.Height = ViewGroup.LayoutParams.WrapContent;

				// If only the 'Ok' button is visible, put him first. Otherwise, put 'Add' first.
				buttonsLayout.RemoveAllViews();
				if (folderMode && !canCreateFiles)
				{
					buttonsLayout.AddView(okButton);
					buttonsLayout.AddView(addButton);
				}
				else
				{
					buttonsLayout.AddView(addButton);
					buttonsLayout.AddView(okButton);
				}
			}
			else
				@params.Height = 0; // hide the layout
		}

		/// <summary>Loads all the files of the SD card root.</summary>
		public virtual void loadFolder() { loadFolder(defaultFolder); }

		/// <summary>Loads all the files of a folder in the file chooser. If no path is specified ('folderPath' is null), the root folder of the SD card is used.</summary>
		/// <param name = "folderPath"> The folder's path. </param>
		public virtual void loadFolder(string folderPath)
		{
			// Get the file path.
			File path = null;
			if (folderPath != null && folderPath.Length > 0)
				path = new File(folderPath);

			loadFolder(path);
		}

		/// <summary> Loads all the files of a folder in the file chooser.  If no path is specified ('folder' is null) the root
		///     folder of the SD card is going to be used. </summary>
		/// <param name = "folder"> The folder. </param>
		public virtual void loadFolder(File folder)
		{
			// Remove previous files.
			LinearLayout root = chooser.RootLayout;
			LinearLayout layout = root.FindViewById< LinearLayout>(Resource.Id.linearLayoutFiles);
			layout.RemoveAllViews();

			// Get the file path.
			if (folder == null || !folder.Exists())
				if (defaultFolder != null)
					currentFolder = defaultFolder;
				else
					currentFolder = Android.OS.Environment.ExternalStorageDirectory;
			else
				currentFolder = folder;

			// Verify if the path exists.
			if (currentFolder.Exists() && layout != null)
			{
				IList<FileItem> fileItems = new List<FileItem>();

				// Add the parent folder.
				if (currentFolder.Parent != null)
				{
					File parent = new File(currentFolder.Parent);
					if (parent.Exists())
						fileItems.Add(new FileItem(chooser.Context, parent, ".."));
				}

				// Verify if the file is a directory.
				if (currentFolder.IsDirectory)
				{
					// Get the folder's files.
					var fileList = currentFolder.ListFiles().ToList();
					if (fileList != null)
					{
						// Order the files alphabetically and separating folders from files.
						fileList.Sort((File file1, File file2)=>
						{
							if (file1 != null && file2 != null)
							{
								if (file1.IsDirectory && (!file2.IsDirectory))
									return -1;
								if (file2.IsDirectory && (!file1.IsDirectory))
									return 1;
								return file1.Name.CompareTo(file2.Name);
							}
							return 0;
						});

						// Iterate all the files in the folder.
						foreach (File file in fileList)
						{
							// Verify if file can be selected (is a directory or folder mode is not activated and the file pass the filter, if defined).
							bool selectable = true;
							if (!file.IsDirectory)
								selectable = !folderMode && (filter == null || new Regex(filter).IsMatch(file.Name));

							// Verify if the file must be show.
							if (selectable || !showOnlySelectable)
							{
								// Create the file item and add it to the list.
								FileItem fileItem = new FileItem(chooser.Context, file);
								fileItem.Selectable = selectable;
								fileItems.Add(fileItem);
							}
						}
					}

					// Set the name of the current folder.
					string currentFolderName = showFullPathInTitle ? currentFolder.Path : currentFolder.Name;
					chooser.CurrentFolderName = currentFolderName;
				}
				else
				// The file is not a folder, add only this file.
					fileItems.Add(new FileItem(chooser.Context, currentFolder));

				// Add click listener and add the FileItem objects to the layout.
				for (int i = 0; i < fileItems.Count; i++)
				{
					fileItems[i].addListener(source=>
					{
						// Verify if the item is a folder.
						File file = source.File;
						if (file.IsDirectory)
							loadFolder(file); // open the folder
						else
							notifyListeners(file, null); // notify the listeners
					});
					layout.AddView(fileItems[i]);
				}

				// Refresh default folder.
				defaultFolder = currentFolder;
			}
		}
	}
}