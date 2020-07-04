namespace SharpBelot
{
	partial class PassedAnnouncesForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( PassedAnnouncesForm ) );
			this._panelMessages = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			// 
			// _panelMessages
			// 
			this._panelMessages.BackColor = System.Drawing.Color.Green;
			resources.ApplyResources( this._panelMessages, "_panelMessages" );
			this._panelMessages.Name = "_panelMessages";
			// 
			// PassedAnnouncesForm
			// 
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add( this._panelMessages );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "PassedAnnouncesForm";
			this.ShowInTaskbar = false;
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.Panel _panelMessages;
	}
}