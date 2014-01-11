namespace SharpBelot
{
	partial class BelotGUIForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( BelotGUIForm ) );
			this._labelEast = new System.Windows.Forms.Label();
			this._labelWest = new System.Windows.Forms.Label();
			this._labelNorth = new System.Windows.Forms.Label();
			this._labelSouth = new System.Windows.Forms.Label();
			this._picPos = new System.Windows.Forms.PictureBox();
			this._menuMain = new System.Windows.Forms.MenuStrip();
			this._menuGame = new System.Windows.Forms.ToolStripMenuItem();
			this._menuNew = new System.Windows.Forms.ToolStripMenuItem();
			this._menuResults = new System.Windows.Forms.ToolStripMenuItem();
			this._menuSettings = new System.Windows.Forms.ToolStripMenuItem();
			this._menuExit = new System.Windows.Forms.ToolStripMenuItem();
			this._menuAbout = new System.Windows.Forms.ToolStripMenuItem();
			this._animationCard = new SharpBelot.CardPictureBox();
			this._panelPlayingTable = new SharpBelot.TransparentPanel();
			this._cardTableWest = new SharpBelot.CardPictureBox();
			this._cardTableEast = new SharpBelot.CardPictureBox();
			this._cardTableSouth = new SharpBelot.CardPictureBox();
			this._cardTableNorth = new SharpBelot.CardPictureBox();
			this._panelSouth = new SharpBelot.CardsPanel();
			this._panelNorth = new SharpBelot.CardsPanel();
			this._panelEast = new SharpBelot.CardsPanel();
			this._panelWest = new SharpBelot.CardsPanel();
			( ( System.ComponentModel.ISupportInitialize )( this._picPos ) ).BeginInit();
			this._menuMain.SuspendLayout();
			this._panelPlayingTable.SuspendLayout();
			this.SuspendLayout();
			// 
			// _labelEast
			// 
			this._labelEast.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelEast, "_labelEast" );
			this._labelEast.ForeColor = System.Drawing.Color.Yellow;
			this._labelEast.Name = "_labelEast";
			// 
			// _labelWest
			// 
			this._labelWest.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelWest, "_labelWest" );
			this._labelWest.ForeColor = System.Drawing.Color.Yellow;
			this._labelWest.Name = "_labelWest";
			// 
			// _labelNorth
			// 
			this._labelNorth.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelNorth, "_labelNorth" );
			this._labelNorth.ForeColor = System.Drawing.Color.Yellow;
			this._labelNorth.Name = "_labelNorth";
			// 
			// _labelSouth
			// 
			this._labelSouth.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelSouth, "_labelSouth" );
			this._labelSouth.ForeColor = System.Drawing.Color.Yellow;
			this._labelSouth.Name = "_labelSouth";
			// 
			// _picPos
			// 
			resources.ApplyResources( this._picPos, "_picPos" );
			this._picPos.Name = "_picPos";
			this._picPos.TabStop = false;
			// 
			// _menuMain
			// 
			this._menuMain.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this._menuGame,
            this._menuAbout} );
			resources.ApplyResources( this._menuMain, "_menuMain" );
			this._menuMain.Name = "_menuMain";
			// 
			// _menuGame
			// 
			this._menuGame.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._menuGame.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this._menuNew,
            this._menuResults,
            this._menuSettings,
            this._menuExit} );
			this._menuGame.Name = "_menuGame";
			resources.ApplyResources( this._menuGame, "_menuGame" );
			// 
			// _menuNew
			// 
			this._menuNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._menuNew.Name = "_menuNew";
			resources.ApplyResources( this._menuNew, "_menuNew" );
			this._menuNew.Click += new System.EventHandler( this.MenuNew_Click );
			// 
			// _menuResults
			// 
			this._menuResults.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._menuResults.Name = "_menuResults";
			resources.ApplyResources( this._menuResults, "_menuResults" );
			this._menuResults.Click += new System.EventHandler( this.MenuResults_Click );
			// 
			// _menuSettings
			// 
			this._menuSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._menuSettings.Name = "_menuSettings";
			resources.ApplyResources( this._menuSettings, "_menuSettings" );
			this._menuSettings.Click += new System.EventHandler( this.MenuSettings_Click );
			// 
			// _menuExit
			// 
			this._menuExit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._menuExit.Name = "_menuExit";
			resources.ApplyResources( this._menuExit, "_menuExit" );
			this._menuExit.Click += new System.EventHandler( this.MenuExit_Click );
			// 
			// _menuAbout
			// 
			this._menuAbout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._menuAbout.Name = "_menuAbout";
			resources.ApplyResources( this._menuAbout, "_menuAbout" );
			this._menuAbout.Click += new System.EventHandler( this.MenuAbout_Click );
			// 
			// _animationCard
			// 
			resources.ApplyResources( this._animationCard, "_animationCard" );
			this._animationCard.Name = "_animationCard";
			// 
			// _panelPlayingTable
			// 
			this._panelPlayingTable.BackColor = System.Drawing.Color.Transparent;
			this._panelPlayingTable.Controls.Add( this._cardTableWest );
			this._panelPlayingTable.Controls.Add( this._cardTableEast );
			this._panelPlayingTable.Controls.Add( this._cardTableSouth );
			this._panelPlayingTable.Controls.Add( this._cardTableNorth );
			resources.ApplyResources( this._panelPlayingTable, "_panelPlayingTable" );
			this._panelPlayingTable.Name = "_panelPlayingTable";
			// 
			// _cardTableWest
			// 
			resources.ApplyResources( this._cardTableWest, "_cardTableWest" );
			this._cardTableWest.Name = "_cardTableWest";
			// 
			// _cardTableEast
			// 
			resources.ApplyResources( this._cardTableEast, "_cardTableEast" );
			this._cardTableEast.Name = "_cardTableEast";
			// 
			// _cardTableSouth
			// 
			resources.ApplyResources( this._cardTableSouth, "_cardTableSouth" );
			this._cardTableSouth.Name = "_cardTableSouth";
			// 
			// _cardTableNorth
			// 
			resources.ApplyResources( this._cardTableNorth, "_cardTableNorth" );
			this._cardTableNorth.Name = "_cardTableNorth";
			// 
			// _panelSouth
			// 
			this._panelSouth.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._panelSouth, "_panelSouth" );
			this._panelSouth.Name = "_panelSouth";
			this._panelSouth.TablePosition = Belot.PlayerPosition.South;
			// 
			// _panelNorth
			// 
			this._panelNorth.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._panelNorth, "_panelNorth" );
			this._panelNorth.Name = "_panelNorth";
			this._panelNorth.TablePosition = Belot.PlayerPosition.North;
			// 
			// _panelEast
			// 
			this._panelEast.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._panelEast, "_panelEast" );
			this._panelEast.Name = "_panelEast";
			this._panelEast.TablePosition = Belot.PlayerPosition.East;
			// 
			// _panelWest
			// 
			this._panelWest.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._panelWest, "_panelWest" );
			this._panelWest.Name = "_panelWest";
			this._panelWest.TablePosition = Belot.PlayerPosition.West;
			// 
			// BelotGUIForm
			// 
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Green;
			this.Controls.Add( this._animationCard );
			this.Controls.Add( this._panelPlayingTable );
			this.Controls.Add( this._panelSouth );
			this.Controls.Add( this._panelNorth );
			this.Controls.Add( this._panelEast );
			this.Controls.Add( this._panelWest );
			this.Controls.Add( this._picPos );
			this.Controls.Add( this._labelEast );
			this.Controls.Add( this._labelWest );
			this.Controls.Add( this._labelNorth );
			this.Controls.Add( this._labelSouth );
			this.Controls.Add( this._menuMain );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MainMenuStrip = this._menuMain;
			this.MaximizeBox = false;
			this.Name = "BelotGUIForm";
			this.Load += new System.EventHandler( this.BelotGUIForm_Load );
			( ( System.ComponentModel.ISupportInitialize )( this._picPos ) ).EndInit();
			this._menuMain.ResumeLayout( false );
			this._menuMain.PerformLayout();
			this._panelPlayingTable.ResumeLayout( false );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label _labelEast;
		private System.Windows.Forms.Label _labelWest;
		private System.Windows.Forms.Label _labelNorth;
		private System.Windows.Forms.Label _labelSouth;
		private System.Windows.Forms.PictureBox _picPos;
		private CardsPanel _panelWest;
		private CardsPanel _panelEast;
		private CardsPanel _panelNorth;
		private CardsPanel _panelSouth;
		private TransparentPanel _panelPlayingTable;
		private System.Windows.Forms.MenuStrip _menuMain;
		private System.Windows.Forms.ToolStripMenuItem _menuGame;
		private System.Windows.Forms.ToolStripMenuItem _menuNew;
		private System.Windows.Forms.ToolStripMenuItem _menuResults;
		private System.Windows.Forms.ToolStripMenuItem _menuSettings;
		private System.Windows.Forms.ToolStripMenuItem _menuExit;
		private System.Windows.Forms.ToolStripMenuItem _menuAbout;
		private CardPictureBox _cardTableWest;
		private CardPictureBox _cardTableEast;
		private CardPictureBox _cardTableSouth;
		private CardPictureBox _cardTableNorth;
		private CardPictureBox _animationCard;
	}
}