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
using System.IO;
using System.Text;
using System.Windows.Forms;
using Belot;

namespace SharpBelot
{
	public partial class AnnounceForm : Form
	{
		private AnnouncementManager _manager;
		private Announcement _announce;
		private Player _player;
		private Font _curlzBold;

		public AnnounceForm()
		{
			InitializeComponent();

			_curlzBold = new Font( Properties.Settings.Default.CurlzFont, FontStyle.Bold );
			_labelAll.Font = _curlzBold;
			_labelNo.Font = _curlzBold;
			_labelDouble.Font = _curlzBold;
			_labelReDouble.Font = _curlzBold;
			_labelSpades.Font = Properties.Settings.Default.MistralFont;
			_labelHearts.Font = Properties.Settings.Default.MistralFont;
			_labelDiamonds.Font = Properties.Settings.Default.MistralFont;
			_labelClubs.Font = Properties.Settings.Default.MistralFont;
		}

		public Announcement Announce
		{
			get
			{
				return _announce;
			}
		}

		private void AnnounceForm_VisibleChanged( object sender, System.EventArgs e )
		{
			if ( this.Visible )
			{
				Announcement ann = _manager.GetLastValidAnnouncement();

				_radioDouble.Enabled = _manager.IsValid( _player, ann.Type, true, false );
				_radioReDouble.Enabled = _manager.IsValid( _player, ann.Type, false, true );


				_radioAll.Checked = ( ann.Type == AnnouncementTypeEnum.AllTrumps );
				_radioAll.Enabled = _manager.IsValid( _player, AnnouncementTypeEnum.AllTrumps, false, false );

				_radioNo.Checked = ( ann.Type == AnnouncementTypeEnum.NoTrumps );
				_radioNo.Enabled = _manager.IsValid( _player, AnnouncementTypeEnum.NoTrumps, false, false );

				_radioSpades.Checked = ( ann.Type == AnnouncementTypeEnum.Spades );
				_radioSpades.Enabled = _manager.IsValid( _player, AnnouncementTypeEnum.Spades, false, false );

				_radioHearts.Checked = ( ann.Type == AnnouncementTypeEnum.Hearts );
				_radioHearts.Enabled = _manager.IsValid( _player, AnnouncementTypeEnum.Hearts, false, false );

				_radioDiamonds.Checked = ( ann.Type == AnnouncementTypeEnum.Diamonds );
				_radioDiamonds.Enabled = _manager.IsValid( _player, AnnouncementTypeEnum.Diamonds, false, false );

				_radioClubs.Checked = ( ann.Type == AnnouncementTypeEnum.Clubs );
				_radioClubs.Enabled = _manager.IsValid( _player, AnnouncementTypeEnum.Clubs, false, false );

				_radioPass.Checked = true;
			}
		}

		private void buttonOK_Click( object sender, System.EventArgs e )
		{
			if ( _radioAll.Checked )
				this._announce = new Announcement( AnnouncementTypeEnum.AllTrumps, false, false );
			if ( _radioNo.Checked )
				this._announce = new Announcement( AnnouncementTypeEnum.NoTrumps, false, false );
			if ( _radioSpades.Checked )
				this._announce = new Announcement( AnnouncementTypeEnum.Spades, false, false );
			if ( _radioHearts.Checked )
				this._announce = new Announcement( AnnouncementTypeEnum.Hearts, false, false );
			if ( _radioDiamonds.Checked )
				this._announce = new Announcement( AnnouncementTypeEnum.Diamonds, false, false );
			if ( _radioClubs.Checked )
				this._announce = new Announcement( AnnouncementTypeEnum.Clubs, false, false );
			if ( _radioPass.Checked )
				this._announce = new Announcement( AnnouncementTypeEnum.Pass, false, false );
			if ( _radioDouble.Checked )
				this._announce = new Announcement( _manager.GetLastValidAnnouncement().Type, true, false );
			if ( _radioReDouble.Checked )
				this._announce = new Announcement( _manager.GetLastValidAnnouncement().Type, false, true );

		}

		public void Bid( Player player, AnnouncementManager manager )
		{
			this._manager = manager;
			this._player = player;
		}

		public void ChangeResources()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( this.GetType() );
			resources.ApplyResources( this._labelReDouble, "_labelReDouble" );
			resources.ApplyResources( this._radioReDouble, "_radioReDouble" );
			resources.ApplyResources( this._labelDouble, "_labelDouble" );
			resources.ApplyResources( this._labelClubs, "_labelClubs" );
			resources.ApplyResources( this._labelDiamonds, "_labelDiamonds" );
			resources.ApplyResources( this._labelHearts, "_labelHearts" );
			resources.ApplyResources( this._labelSpades, "_labelSpades" );
			resources.ApplyResources( this._labelNo, "_labelNo" );
			resources.ApplyResources( this._labelAll, "_labelAll" );
			resources.ApplyResources( this._radioDouble, "_radioDouble" );
			resources.ApplyResources( this._buttonOK, "_buttonOK" );
			resources.ApplyResources( this._radioPass, "_radioPass" );
			resources.ApplyResources( this._radioClubs, "_radioClubs" );
			resources.ApplyResources( this._radioDiamonds, "_radioDiamonds" );
			resources.ApplyResources( this._radioHearts, "_radioHearts" );
			resources.ApplyResources( this._radioSpades, "_radioSpades" );
			resources.ApplyResources( this._radioNo, "_radioNo" );
			resources.ApplyResources( this._radioAll, "_radioAll" );
			resources.ApplyResources( this, "$this" );
		}
	}
}