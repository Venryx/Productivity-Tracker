using Android.App;
using Android.Content;
using Android.Widget;

public static class VUI
{
	/*public static void ShowSeekBarDialog()
	{
		AlertDialog.Builder alert = new AlertDialog.Builder(this);

		alert.setTitle("Alert Box");
		alert.setMessage("Edit Text");

		LinearLayout linear = new LinearLayout(this);

		linear.setOrientation(1);
		TextView text = new TextView(this);
		text.setText("Hello Android");
		text.setPadding(10, 10, 10, 10);

		SeekBar seek = new SeekBar(this);

		linear.addView(seek);
		linear.addView(text);

		alert.setView(linear);

		alert.setPositiveButton("Ok", new DialogInterface.OnClickListener()
		{
				public void onClick(DialogInterface dialog, int id)
			{
				Toast.makeText(getApplicationContext(), "OK Pressed", Toast.LENGTH_LONG).show();
				finish();
			}
		}); 

		alert.setNegativeButton("Cancel",new DialogInterface.OnClickListener()  
		{ 
			public void onClick(DialogInterface dialog, int id)
			{
				Toast.makeText(getApplicationContext(), "Cancel Pressed", Toast.LENGTH_LONG).show();
				finish();
			} 
		}); 

		alert.show(); 
	}*/
}