using Android.Content;
using Android.Widget;

namespace VFileChooser
{
	/// <summary>This interface defines all the methods that a file chooser must implement, in order to being able to make use of the class FileChooserUtils.</summary>
	internal interface FileChooser
	{
		/// <summary>Gets the root of the layout 'file_chooser.xml'.</summary>
		/// <returns>A linear layout.</returns>
		LinearLayout RootLayout { get; }

		/// <summary>Set the name of the current folder.</summary>
		/// <param name="name">The current folder's name.</param>
		string CurrentFolderName { set; }

		/// <summary>Returns the current context of the file chooser.</summary>
		/// <returns>The current context.</returns>
		Context Context { get; }
	}
}