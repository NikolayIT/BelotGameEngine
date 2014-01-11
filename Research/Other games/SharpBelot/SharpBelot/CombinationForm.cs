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
using Belot;

namespace SharpBelot
{
	public partial class CombinationForm : Form
	{
		public CombinationForm()
		{
			InitializeComponent();
		}

		public CardCombination Combination
		{
			set
			{
				_labelCombination.Text = GetCombinationStringFull( value );
			}
		}

		#region Private Methods

		private string GetCombinationStringFull( CardCombination combination )
		{
			StringBuilder sb = new StringBuilder();

			foreach ( Card card in combination.Cards )
			{
				sb.Append( card.ToString() + " " );
			}

			return sb.ToString();
		}

		private void OnYesClick( object sender, EventArgs e )
		{
			this.Close();
		}

		#endregion

		public void ChangeResources()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( this.GetType() );
			resources.ApplyResources( this._labelCombination, "_labelCombination" );
			resources.ApplyResources( this._buttonNo, "_buttonNo" );
			resources.ApplyResources( this._buttonYes, "_buttonYes" );
			resources.ApplyResources( this._labelQuestion, "_labelQuestion" );
			resources.ApplyResources( this, "$this" );
		}
	}
}