/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Threading;
using System.Reflection;
using Belot;

namespace SharpBelot
{
	public partial class SettingsForm : Form
	{
		public SettingsForm()
		{
			InitializeComponent();

			_boxSouthName.Text = Properties.Settings.Default.SouthName;
			_boxNorthName.Text = Properties.Settings.Default.NorthName;
			_boxEastName.Text = Properties.Settings.Default.EastName;
			_boxWestName.Text = Properties.Settings.Default.WestName;

			IList<string> aiDll = FindPlayerDlls();
			_comboAINorth.DataSource = aiDll;
			_comboAIEast.DataSource = aiDll;
			_comboAIWest.DataSource = aiDll;

			_comboCards.DataSource = FindCardDlls();

			_comboAINorth.SelectedItem = Properties.Settings.Default.NorthDll;
			_comboAIEast.SelectedItem = Properties.Settings.Default.EastDll;
			_comboAIWest.SelectedItem = Properties.Settings.Default.WestDll;

			_comboCards.SelectedItem = Properties.Settings.Default.CardsDll;

			_trackSpeed.Value = Properties.Settings.Default.Speed;

			_checkCapotDouble.Checked = Properties.Settings.Default.CapotRemovesDouble;
			_checkExtraDouble.Checked = Properties.Settings.Default.ExtraPointsAreDoubled;

			_comboLanguage.DisplayMember = "DisplayName";
			_comboLanguage.Items.Add( new CultureInfo( "en-US" ) );
			_comboLanguage.Items.Add( new CultureInfo( "bg-BG" ) );
			_comboLanguage.SelectedItem = CultureInfo.CurrentCulture;
		}

		#region Event Handlers

		private void ButtonOKClick( object sender, EventArgs e )
		{
			Properties.Settings.Default.SouthName = _boxSouthName.Text;
			Properties.Settings.Default.NorthName = _boxNorthName.Text;
			Properties.Settings.Default.EastName = _boxEastName.Text;
			Properties.Settings.Default.WestName = _boxWestName.Text;

			Properties.Settings.Default.NorthDll = _comboAINorth.SelectedItem.ToString();
			Properties.Settings.Default.EastDll = _comboAIEast.SelectedItem.ToString();
			Properties.Settings.Default.WestDll = _comboAIWest.SelectedItem.ToString();

			if ( _comboCards.SelectedItem != null )
			{
				if ( Properties.Settings.Default.CardsDll != _comboCards.SelectedItem.ToString() )
				{
					Properties.Settings.Default.CardsDll = _comboCards.SelectedItem.ToString();
					CardGUIProvider.Reload();
				}
				else
				{
					Properties.Settings.Default.CardsDll = _comboCards.SelectedItem.ToString();
				}
			}

			Properties.Settings.Default.Speed = _trackSpeed.Value;

			Properties.Settings.Default.CapotRemovesDouble = _checkCapotDouble.Checked;
			Properties.Settings.Default.ExtraPointsAreDoubled = _checkExtraDouble.Checked;

			Thread.CurrentThread.CurrentUICulture = _comboLanguage.SelectedItem as CultureInfo;
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture;

			Properties.Settings.Default.Culture = Thread.CurrentThread.CurrentUICulture.ToString();
			Properties.Settings.Default.Save();

			this.DialogResult = DialogResult.OK;
		}
		
		#endregion

		public void ChangeResources()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( this.GetType() );
			resources.ApplyResources( this._tabControl, "_tabControl" );
			resources.ApplyResources( this._tabGeneral, "_tabGeneral" );
			resources.ApplyResources( this._groupBoxLanguage, "_groupBoxLanguage" );
			resources.ApplyResources( this._comboLanguage, "_comboLanguage" );
			resources.ApplyResources( this._groupBoxSpeed, "_groupBoxSpeed" );
			resources.ApplyResources( this._trackSpeed, "_trackSpeed" );
			resources.ApplyResources( this._groupBoxNames, "_groupBoxNames" );
			resources.ApplyResources( this._boxWestName, "_boxWestName" );
			resources.ApplyResources( this._labelWestName, "_labelWestName" );
			resources.ApplyResources( this._boxEastName, "_boxEastName" );
			resources.ApplyResources( this._labelSouthName, "_labelSouthName" );
			resources.ApplyResources( this._boxSouthName, "_boxSouthName" );
			resources.ApplyResources( this._boxNorthName, "_boxNorthName" );
			resources.ApplyResources( this._labelNorthName, "_labelNorthName" );
			resources.ApplyResources( this._labelEastName, "_labelEastName" );
			resources.ApplyResources( this._tabAdvanced, "_tabAdvanced" );
			resources.ApplyResources( this._groupBoxCards, "_groupBoxCards" );
			resources.ApplyResources( this._groupBoxRules, "_groupBoxRules" );
			resources.ApplyResources( this._labelExtraDouble, "_labelExtraDouble" );
			resources.ApplyResources( this._labelCapotDouble, "_labelCapotDouble" );
			resources.ApplyResources( this._checkCapotDouble, "_checkCapotDouble" );
			resources.ApplyResources( this._checkExtraDouble, "_checkExtraDouble" );
			resources.ApplyResources( this._gbDlls, "_gbDlls" );
			resources.ApplyResources( this._labelWest, "_labelWest" );
			resources.ApplyResources( this._labelNorth, "_labelNorth" );
			resources.ApplyResources( this._labelEast, "_labelEast" );
			resources.ApplyResources( this._buttonCancel, "_buttonCancel" );
			resources.ApplyResources( this._buttonOK, "_buttonOK" );
			resources.ApplyResources( this, "$this" );
		}

		private IList<string> FindPlayerDlls()
		{
			string[] allDlls = Directory.GetFiles( ".", "*.dll" );
			List<string> aiDlls = new List<string>();

			foreach ( string dll in allDlls )
			{
				Assembly asm = Assembly.LoadFrom( dll );

				if ( asm.GetType( "AIPlayers.AIPlayer" ) != null )
				{
					aiDlls.Add( dll );
				}
			}

			return aiDlls;
		}

		private IList<string> FindCardDlls()
		{
			string[] allDlls = Directory.GetFiles( ".", "*.dll" );
			List<string> cardDlls = new List<string>();

			foreach ( string dll in allDlls )
			{
				Assembly asm = Assembly.LoadFrom( dll );

				if ( asm.GetType( "CardRes.Properties.Resources" ) != null )
				{
					cardDlls.Add( dll );
				}
			}

			return cardDlls;
		}
	}
}