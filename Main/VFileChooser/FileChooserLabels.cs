using System;

namespace VFileChooser
{
	/// <summary>Instances of this classes are used to re-define the value of the labels of a file chooser. If an attribute is set to null, then the default value is going to be used.</summary>
	[Serializable] public class FileChooserLabels
	{
		/// <summary>Static field required by the interface Serializable.</summary>
		const long serialVersionUID = 1L;

		/// <summary>Default's constructor.</summary>
		public FileChooserLabels()
		{
			labelAddButton = null;
			labelSelectButton = null;
			messageConfirmSelection = null;
			messageConfirmCreation = null;
			labelConfirmYesButton = null;
			labelConfirmNoButton = null;
			createFileDialogTitle = null;
			createFileDialogTitle = null;
			createFileDialogAcceptButton = null;
			createFileDialogCancelButton = null;
		}

		/// <summary>The label for the button used to create a file or a folder.</summary>
		public string labelAddButton;

		/// <summary>The label for the button for select the current folder (when using the file chooser for select folders).</summary>
		public string labelSelectButton;

		/// <summary>The message displayed by the confirmation dialog, when selecting a file. In this string, the character sequence '$file_name' is going to be replace by the file's name.</summary>
		public string messageConfirmSelection;

		/// <summary>The message displayed by the confirmation dialog, when creating a file. In this string, the character sequence '$file_name' is going to be replace by the file's name.</summary>
		public string messageConfirmCreation;

		/// <summary>The label for the 'yes' button when confirming the selection o creation of a file.</summary>
		public string labelConfirmYesButton;

		/// <summary>The label for the 'no' button when confirming the selection o creation of a file.</summary>
		public string labelConfirmNoButton;

		/// <summary>The title of the dialog for create a file.</summary>
		public string createFileDialogTitle;

		/// <summary>The message of the dialog for create a file.</summary>
		public string createFileDialogMessage;

		/// <summary>The label of the 'accept' button in the dialog for create a file.</summary>
		public string createFileDialogAcceptButton;

		/// <summary>The label of the 'cancel' button in the dialog for create a file.</summary>
		public string createFileDialogCancelButton;
	}
}