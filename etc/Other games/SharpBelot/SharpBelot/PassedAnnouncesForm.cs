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

namespace SharpBelot
{
	public partial class PassedAnnouncesForm : Form
	{
		private int _labelsCount = 0;
		private const int LABEL_HEIGHT = 20;
		private Font _labelFont = null;
		private Font _labelFontBold = null;
		private Form _mainForm;

		public PassedAnnouncesForm( Form mainForm )
		{
			InitializeComponent();

			_mainForm = mainForm;
			_mainForm.Move += new System.EventHandler( OnMainFormMove );
			this.Height = _mainForm.Height/2;

			_labelFont = new Font( Properties.Settings.Default.MistralFont, FontStyle.Regular );
			_labelFontBold = new Font( Properties.Settings.Default.MistralFont, FontStyle.Bold );

			AdjustPosition();
		}

		public void AddMessage( string playerName, string message, bool isActive )
		{
			Label labelName = new Label();
			labelName.ForeColor = ( isActive ? Color.OrangeRed : Color.Yellow );
			labelName.Font = _labelFontBold;
			labelName.Left = 0;
			labelName.Top = _labelsCount * LABEL_HEIGHT;
			labelName.Text = playerName;
			labelName.Width = _panelMessages.Width*2/5;
			_panelMessages.Controls.Add( labelName );

			Label labelMessage = new Label();
			labelMessage.ForeColor = ( isActive ? Color.OrangeRed : Color.Yellow );
			labelMessage.Font = _labelFont;
			labelMessage.Left = _panelMessages.Width*2/5;
			labelMessage.Top = _labelsCount * LABEL_HEIGHT;
			labelMessage.Text = message;
			labelMessage.Width = _panelMessages.Width*3/5;
			_panelMessages.Controls.Add( labelMessage );

			_panelMessages.Update();
			_labelsCount++;
		}

		public void ClearMessages()
		{
			_panelMessages.Controls.Clear();
			_panelMessages.Invalidate();
			_labelsCount = 0;
		}

		public void AddSpaces()
		{
			_labelsCount += 2;
		}

		private void OnMainFormMove( object sender, System.EventArgs e )
		{
			AdjustPosition();
		}

		private void AdjustPosition()
		{
			this.Left = _mainForm.Location.X + _mainForm.Width;
			this.Top = _mainForm.Location.Y;
		}

		public void ChangeResources()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( this.GetType() );
			resources.ApplyResources( this._panelMessages, "_panelMessages" );
			resources.ApplyResources( this, "$this" );
		}
	}
}