/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace SharpBelot
{
	class DisabledDataGridTexboxColumn : DataGridTextBoxColumn
	{
		public DisabledDataGridTexboxColumn()
		{
		}

		protected override void Edit( CurrencyManager source, int rowNum, System.Drawing.Rectangle bounds, bool readOnly, string instantText, bool cellIsVisible )
		{
			this.DataGridTableStyle.DataGrid.Focus();
		}
	}

	/// <summary>
	/// Summary description for DisabledTexbox.
	/// </summary>
	public class DisabledTexbox : TextBox
	{
		public DisabledTexbox()
		{
		}

		protected override void OnGotFocus( System.EventArgs e )
		{
			this.GetContainerControl().ActiveControl = null;
		}
	}
}
