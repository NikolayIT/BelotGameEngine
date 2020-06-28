namespace SharpBelot
{
	partial class CombinationForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( CombinationForm ) );
			this._labelCombination = new System.Windows.Forms.Label();
			this._buttonNo = new System.Windows.Forms.Button();
			this._buttonYes = new System.Windows.Forms.Button();
			this._labelQuestion = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _labelCombination
			// 
			this._labelCombination.AccessibleDescription = null;
			this._labelCombination.AccessibleName = null;
			resources.ApplyResources( this._labelCombination, "_labelCombination" );
			this._labelCombination.BackColor = System.Drawing.Color.Transparent;
			this._labelCombination.Name = "_labelCombination";
			// 
			// _buttonNo
			// 
			this._buttonNo.AccessibleDescription = null;
			this._buttonNo.AccessibleName = null;
			resources.ApplyResources( this._buttonNo, "_buttonNo" );
			this._buttonNo.BackgroundImage = null;
			this._buttonNo.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonNo.Font = null;
			this._buttonNo.Name = "_buttonNo";
			// 
			// _buttonYes
			// 
			this._buttonYes.AccessibleDescription = null;
			this._buttonYes.AccessibleName = null;
			resources.ApplyResources( this._buttonYes, "_buttonYes" );
			this._buttonYes.BackgroundImage = null;
			this._buttonYes.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._buttonYes.Font = null;
			this._buttonYes.Name = "_buttonYes";
			// 
			// _labelQuestion
			// 
			this._labelQuestion.AccessibleDescription = null;
			this._labelQuestion.AccessibleName = null;
			resources.ApplyResources( this._labelQuestion, "_labelQuestion" );
			this._labelQuestion.BackColor = System.Drawing.Color.Transparent;
			this._labelQuestion.Font = null;
			this._labelQuestion.Name = "_labelQuestion";
			// 
			// CombinationForm
			// 
			this.AcceptButton = this._buttonYes;
			this.AccessibleDescription = null;
			this.AccessibleName = null;
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImage = null;
			this.CancelButton = this._buttonNo;
			this.ControlBox = false;
			this.Controls.Add( this._labelCombination );
			this.Controls.Add( this._buttonNo );
			this.Controls.Add( this._buttonYes );
			this.Controls.Add( this._labelQuestion );
			this.Font = null;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = null;
			this.Name = "CombinationForm";
			this.ShowInTaskbar = false;
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.Label _labelCombination;
		private System.Windows.Forms.Button _buttonNo;
		private System.Windows.Forms.Button _buttonYes;
		private System.Windows.Forms.Label _labelQuestion;
	}
}