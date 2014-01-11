/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System.Drawing.Text;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpBelot.Properties
{


	// This class allows you to handle specific events on the settings class:
	//  The SettingChanging event is raised before a setting's value is changed.
	//  The PropertyChanged event is raised after a setting's value is changed.
	//  The SettingsLoaded event is raised after the setting values are loaded.
	//  The SettingsSaving event is raised before the setting values are saved.
	internal sealed partial class Settings
	{
		private PrivateFontCollection _fonts;
		private Font _mistralFont;
		private Font _curlzFont;

		public Settings()
		{
			// // To add event handlers for saving and changing settings, uncomment the lines below:
			//
			// this.SettingChanging += this.SettingChangingEventHandler;
			//
			// this.SettingsSaving += this.SettingsSavingEventHandler;
			//

			FileStream fsCurlz = new FileStream( this.CurlzFontFile, FileMode.Create );
			fsCurlz.Write( Resources.CURLZ, 0, Resources.CURLZ.Length );
			fsCurlz.Close();

			FileStream fsMist = new FileStream( this.MistralFontFile, FileMode.Create );
			fsMist.Write( Resources.MISTRAL, 0, Resources.MISTRAL.Length );
			fsMist.Close();

			_fonts = new PrivateFontCollection();
			_fonts.AddFontFile( this.CurlzFontFile );
			_fonts.AddFontFile( this.MistralFontFile );

			_curlzFont = new Font( _fonts.Families[ 0 ], 14 );
			_mistralFont = new Font( _fonts.Families[ 1 ], 14 );
		}

		private void SettingChangingEventHandler( object sender, System.Configuration.SettingChangingEventArgs e )
		{
			// Add code to handle the SettingChangingEvent event here.
		}

		private void SettingsSavingEventHandler( object sender, System.ComponentModel.CancelEventArgs e )
		{
			// Add code to handle the SettingsSaving event here.
		}

		public Font MistralFont
		{
			get
			{
				return _mistralFont;
			}
		}

		public Font CurlzFont
		{
			get
			{
				return _curlzFont;
			}
		}

	}
}
