namespace SharpBelot
{
	partial class ResultForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( ResultForm ) );
			this._buttonOK = new System.Windows.Forms.Button();
			this._dGridResults = new System.Windows.Forms.DataGrid();
			this._dataGridTableStyle = new System.Windows.Forms.DataGridTableStyle();
			this._dGridColumn1 = new SharpBelot.DisabledDataGridTexboxColumn();
			this._dGridColumn2 = new SharpBelot.DisabledDataGridTexboxColumn();
			this._boxTotalWe = new SharpBelot.DisabledTexbox();
			this._boxTotalYou = new SharpBelot.DisabledTexbox();
			( ( System.ComponentModel.ISupportInitialize )( this._dGridResults ) ).BeginInit();
			this.SuspendLayout();
			// 
			// _buttonOK
			// 
			resources.ApplyResources( this._buttonOK, "_buttonOK" );
			this._buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._buttonOK.Name = "_buttonOK";
			// 
			// _dGridResults
			// 
			this._dGridResults.AllowNavigation = false;
			this._dGridResults.AllowSorting = false;
			resources.ApplyResources( this._dGridResults, "_dGridResults" );
			this._dGridResults.BackgroundColor = System.Drawing.SystemColors.Window;
			this._dGridResults.CaptionVisible = false;
			this._dGridResults.DataMember = "";
			this._dGridResults.GridLineColor = System.Drawing.Color.Blue;
			this._dGridResults.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this._dGridResults.Name = "_dGridResults";
			this._dGridResults.PreferredColumnWidth = 100;
			this._dGridResults.ReadOnly = true;
			this._dGridResults.RowHeadersVisible = false;
			this._dGridResults.TableStyles.AddRange( new System.Windows.Forms.DataGridTableStyle[] {
            this._dataGridTableStyle} );
			// 
			// _dataGridTableStyle
			// 
			this._dataGridTableStyle.AllowSorting = false;
			this._dataGridTableStyle.DataGrid = this._dGridResults;
			this._dataGridTableStyle.GridColumnStyles.AddRange( new System.Windows.Forms.DataGridColumnStyle[] {
            this._dGridColumn1,
            this._dGridColumn2} );
			this._dataGridTableStyle.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this._dataGridTableStyle.MappingName = "ResultsTable";
			resources.ApplyResources( this._dataGridTableStyle, "_dataGridTableStyle" );
			this._dataGridTableStyle.ReadOnly = true;
			this._dataGridTableStyle.RowHeadersVisible = false;
			// 
			// _dGridColumn1
			// 
			resources.ApplyResources( this._dGridColumn1, "_dGridColumn1" );
			this._dGridColumn1.FormatInfo = null;
			// 
			// _dGridColumn2
			// 
			resources.ApplyResources( this._dGridColumn2, "_dGridColumn2" );
			this._dGridColumn2.FormatInfo = null;
			// 
			// _boxTotalWe
			// 
			resources.ApplyResources( this._boxTotalWe, "_boxTotalWe" );
			this._boxTotalWe.Name = "_boxTotalWe";
			// 
			// _boxTotalYou
			// 
			resources.ApplyResources( this._boxTotalYou, "_boxTotalYou" );
			this._boxTotalYou.Name = "_boxTotalYou";
			// 
			// ResultForm
			// 
			this.AcceptButton = this._buttonOK;
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add( this._boxTotalYou );
			this.Controls.Add( this._boxTotalWe );
			this.Controls.Add( this._buttonOK );
			this.Controls.Add( this._dGridResults );
			this.Name = "ResultForm";
			this.ShowInTaskbar = false;
			this.Resize += new System.EventHandler( this.ResultForm_Resize );
			this.VisibleChanged += new System.EventHandler( this.ResultForm_VisibleChanged );
			( ( System.ComponentModel.ISupportInitialize )( this._dGridResults ) ).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.DataGrid _dGridResults;
		private DisabledTexbox _boxTotalWe;
		private DisabledTexbox _boxTotalYou;
		private System.Windows.Forms.DataGridTableStyle _dataGridTableStyle;
		private DisabledDataGridTexboxColumn _dGridColumn1;
		private DisabledDataGridTexboxColumn _dGridColumn2;
	}
}