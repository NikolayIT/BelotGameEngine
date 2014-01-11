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
	public partial class ResultForm : Form
	{
		private DataTable _resultsTable;
		private int _hangingPoints = 0;
		private string _caption = "";

		public ResultForm( DataTable results )
		{
			InitializeComponent();

			_resultsTable = results;
			_resultsTable.DefaultView.AllowNew = false;
			_resultsTable.DefaultView.AllowDelete = false;
			_resultsTable.DefaultView.AllowEdit = false;
			_dGridResults.DataSource = _resultsTable.DefaultView;

			_dGridResults.Font = Properties.Settings.Default.MistralFont;
			_boxTotalWe.Font = Properties.Settings.Default.MistralFont;
			_boxTotalYou.Font = Properties.Settings.Default.MistralFont;

			_caption = this.Text;
		}

		#region Properties

		public int HangingPoints
		{
			get
			{
				return _hangingPoints;
			}
			set
			{
				_hangingPoints = value;
			}
		}

		#endregion

		#region Private Methods

		private void ResultForm_VisibleChanged( object sender, EventArgs e )
		{
			ResizeColumns();

			CalculateTotals();

			this.Text = _caption;

			if ( _hangingPoints != 0 )
			{
				this.Text += " - ";
				this.Text += StringResources.hanging;
				this.Text += ": " + _hangingPoints.ToString();
			}

			if ( _resultsTable.Rows.Count > 0 )
			{
				_dGridResults.Select( _resultsTable.Rows.Count-1 );
			}
		}

		private void ResultForm_Resize( object sender, System.EventArgs e )
		{
			ResizeColumns();

			_boxTotalWe.Width = _dGridResults.Width / 2 - 2;
			_boxTotalYou.Width = _dGridResults.Width / 2 - 2;
			_boxTotalYou.Left = _boxTotalWe.Left + _boxTotalWe.Width;
		}

		private void ResizeColumns()
		{
			_dGridColumn1.Width = _dGridResults.Width / 2 - 2;
			_dGridColumn2.Width = _dGridResults.Width / 2 - 2;
		}

		private void CalculateTotals()
		{
			int totalWe = 0;
			int totalYou = 0;

			foreach ( DataRow row in _resultsTable.Rows )
			{
				totalWe += Convert.ToInt32( row[ "We" ] );
				totalYou += Convert.ToInt32( row[ "You" ] );
			}

			_boxTotalWe.Text = totalWe.ToString();
			_boxTotalYou.Text = totalYou.ToString();
		}
		#endregion

		public void ChangeResources()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( this.GetType() );
			resources.ApplyResources( this._buttonOK, "_buttonOK" );
			resources.ApplyResources( this._dGridResults, "_dGridResults" );
			resources.ApplyResources( this._dataGridTableStyle, "_dataGridTableStyle" );
			resources.ApplyResources( this._dGridColumn1, "_dGridColumn1" );
			resources.ApplyResources( this._dGridColumn2, "_dGridColumn2" );
			resources.ApplyResources( this._boxTotalWe, "_boxTotalWe" );
			resources.ApplyResources( this._boxTotalYou, "_boxTotalYou" );
			resources.ApplyResources( this, "$this" );

			_dGridResults.Font = Properties.Settings.Default.MistralFont;
			_boxTotalWe.Font = Properties.Settings.Default.MistralFont;
			_boxTotalYou.Font = Properties.Settings.Default.MistralFont;

			_caption = this.Text;
		}
	}
}