namespace SharpBelot
{
	partial class AboutForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( AboutForm ) );
			this._picture = new System.Windows.Forms.PictureBox();
			this._labelAbout = new System.Windows.Forms.Label();
			this._buttonOK = new System.Windows.Forms.Button();
			this._linkCurrent = new System.Windows.Forms.LinkLabel();
			this._labelCurrent = new System.Windows.Forms.Label();
			this._linkOld = new System.Windows.Forms.LinkLabel();
			this._labelOld = new System.Windows.Forms.Label();
			this._linkRules = new System.Windows.Forms.LinkLabel();
			this._labelRules = new System.Windows.Forms.Label();
			this._timer = new System.Windows.Forms.Timer( this.components );
			this._linkLabelEmail = new System.Windows.Forms.LinkLabel();
			( ( System.ComponentModel.ISupportInitialize )( this._picture ) ).BeginInit();
			this.SuspendLayout();
			// 
			// _picture
			// 
			this._picture.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._picture, "_picture" );
			this._picture.Name = "_picture";
			this._picture.TabStop = false;
			// 
			// _labelAbout
			// 
			this._labelAbout.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelAbout, "_labelAbout" );
			this._labelAbout.Name = "_labelAbout";
			// 
			// _buttonOK
			// 
			this._buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources( this._buttonOK, "_buttonOK" );
			this._buttonOK.Name = "_buttonOK";
			// 
			// _linkCurrent
			// 
			this._linkCurrent.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._linkCurrent, "_linkCurrent" );
			this._linkCurrent.Name = "_linkCurrent";
			this._linkCurrent.TabStop = true;
			this._linkCurrent.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler( this.LinkClicked );
			// 
			// _labelCurrent
			// 
			this._labelCurrent.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelCurrent, "_labelCurrent" );
			this._labelCurrent.Name = "_labelCurrent";
			// 
			// _linkOld
			// 
			this._linkOld.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._linkOld, "_linkOld" );
			this._linkOld.Name = "_linkOld";
			this._linkOld.TabStop = true;
			this._linkOld.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler( this.LinkClicked );
			// 
			// _labelOld
			// 
			this._labelOld.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelOld, "_labelOld" );
			this._labelOld.Name = "_labelOld";
			// 
			// _linkRules
			// 
			this._linkRules.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._linkRules, "_linkRules" );
			this._linkRules.Name = "_linkRules";
			this._linkRules.TabStop = true;
			this._linkRules.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler( this.LinkClicked );
			// 
			// _labelRules
			// 
			this._labelRules.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelRules, "_labelRules" );
			this._labelRules.Name = "_labelRules";
			// 
			// _timer
			// 
			this._timer.Tick += new System.EventHandler( this.Timer_Tick );
			// 
			// _linkLabelEmail
			// 
			this._linkLabelEmail.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._linkLabelEmail, "_linkLabelEmail" );
			this._linkLabelEmail.Name = "_linkLabelEmail";
			this._linkLabelEmail.TabStop = true;
			this._linkLabelEmail.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler( this.MailClicked );
			// 
			// AboutForm
			// 
			this.AcceptButton = this._buttonOK;
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add( this._linkLabelEmail );
			this.Controls.Add( this._buttonOK );
			this.Controls.Add( this._linkCurrent );
			this.Controls.Add( this._labelCurrent );
			this.Controls.Add( this._linkOld );
			this.Controls.Add( this._labelOld );
			this.Controls.Add( this._linkRules );
			this.Controls.Add( this._labelRules );
			this.Controls.Add( this._labelAbout );
			this.Controls.Add( this._picture );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AboutForm";
			this.ShowInTaskbar = false;
			( ( System.ComponentModel.ISupportInitialize )( this._picture ) ).EndInit();
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.PictureBox _picture;
		private System.Windows.Forms.Label _labelAbout;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.LinkLabel _linkCurrent;
		private System.Windows.Forms.Label _labelCurrent;
		private System.Windows.Forms.LinkLabel _linkOld;
		private System.Windows.Forms.Label _labelOld;
		private System.Windows.Forms.LinkLabel _linkRules;
		private System.Windows.Forms.Label _labelRules;
		private System.Windows.Forms.Timer _timer;
		private System.Windows.Forms.LinkLabel _linkLabelEmail;
	}
}