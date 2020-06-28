namespace SharpBelot
{
	partial class SettingsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( SettingsForm ) );
			this._tabControl = new System.Windows.Forms.TabControl();
			this._tabGeneral = new System.Windows.Forms.TabPage();
			this._groupBoxLanguage = new System.Windows.Forms.GroupBox();
			this._comboLanguage = new System.Windows.Forms.ComboBox();
			this._groupBoxSpeed = new System.Windows.Forms.GroupBox();
			this._trackSpeed = new System.Windows.Forms.TrackBar();
			this._groupBoxNames = new System.Windows.Forms.GroupBox();
			this._boxWestName = new System.Windows.Forms.TextBox();
			this._labelWestName = new System.Windows.Forms.Label();
			this._boxEastName = new System.Windows.Forms.TextBox();
			this._labelSouthName = new System.Windows.Forms.Label();
			this._boxSouthName = new System.Windows.Forms.TextBox();
			this._boxNorthName = new System.Windows.Forms.TextBox();
			this._labelNorthName = new System.Windows.Forms.Label();
			this._labelEastName = new System.Windows.Forms.Label();
			this._tabAdvanced = new System.Windows.Forms.TabPage();
			this._groupBoxCards = new System.Windows.Forms.GroupBox();
			this._comboCards = new System.Windows.Forms.ComboBox();
			this._groupBoxRules = new System.Windows.Forms.GroupBox();
			this._labelExtraDouble = new System.Windows.Forms.Label();
			this._labelCapotDouble = new System.Windows.Forms.Label();
			this._checkCapotDouble = new System.Windows.Forms.CheckBox();
			this._checkExtraDouble = new System.Windows.Forms.CheckBox();
			this._gbDlls = new System.Windows.Forms.GroupBox();
			this._comboAIWest = new System.Windows.Forms.ComboBox();
			this._comboAINorth = new System.Windows.Forms.ComboBox();
			this._comboAIEast = new System.Windows.Forms.ComboBox();
			this._labelWest = new System.Windows.Forms.Label();
			this._labelNorth = new System.Windows.Forms.Label();
			this._labelEast = new System.Windows.Forms.Label();
			this._buttonCancel = new System.Windows.Forms.Button();
			this._buttonOK = new System.Windows.Forms.Button();
			this._tabControl.SuspendLayout();
			this._tabGeneral.SuspendLayout();
			this._groupBoxLanguage.SuspendLayout();
			this._groupBoxSpeed.SuspendLayout();
			( ( System.ComponentModel.ISupportInitialize )( this._trackSpeed ) ).BeginInit();
			this._groupBoxNames.SuspendLayout();
			this._tabAdvanced.SuspendLayout();
			this._groupBoxCards.SuspendLayout();
			this._groupBoxRules.SuspendLayout();
			this._gbDlls.SuspendLayout();
			this.SuspendLayout();
			// 
			// _tabControl
			// 
			this._tabControl.Controls.Add( this._tabGeneral );
			this._tabControl.Controls.Add( this._tabAdvanced );
			resources.ApplyResources( this._tabControl, "_tabControl" );
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			// 
			// _tabGeneral
			// 
			this._tabGeneral.BackColor = System.Drawing.Color.Transparent;
			this._tabGeneral.Controls.Add( this._groupBoxLanguage );
			this._tabGeneral.Controls.Add( this._groupBoxSpeed );
			this._tabGeneral.Controls.Add( this._groupBoxNames );
			resources.ApplyResources( this._tabGeneral, "_tabGeneral" );
			this._tabGeneral.Name = "_tabGeneral";
			this._tabGeneral.UseVisualStyleBackColor = true;
			// 
			// _groupBoxLanguage
			// 
			this._groupBoxLanguage.BackColor = System.Drawing.Color.Transparent;
			this._groupBoxLanguage.Controls.Add( this._comboLanguage );
			resources.ApplyResources( this._groupBoxLanguage, "_groupBoxLanguage" );
			this._groupBoxLanguage.Name = "_groupBoxLanguage";
			this._groupBoxLanguage.TabStop = false;
			// 
			// _comboLanguage
			// 
			this._comboLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources( this._comboLanguage, "_comboLanguage" );
			this._comboLanguage.Name = "_comboLanguage";
			// 
			// _groupBoxSpeed
			// 
			this._groupBoxSpeed.Controls.Add( this._trackSpeed );
			resources.ApplyResources( this._groupBoxSpeed, "_groupBoxSpeed" );
			this._groupBoxSpeed.Name = "_groupBoxSpeed";
			this._groupBoxSpeed.TabStop = false;
			// 
			// _trackSpeed
			// 
			this._trackSpeed.BackColor = System.Drawing.SystemColors.Control;
			resources.ApplyResources( this._trackSpeed, "_trackSpeed" );
			this._trackSpeed.Maximum = 20;
			this._trackSpeed.Minimum = 1;
			this._trackSpeed.Name = "_trackSpeed";
			this._trackSpeed.Value = 10;
			// 
			// _groupBoxNames
			// 
			this._groupBoxNames.BackColor = System.Drawing.Color.Transparent;
			this._groupBoxNames.Controls.Add( this._boxWestName );
			this._groupBoxNames.Controls.Add( this._labelWestName );
			this._groupBoxNames.Controls.Add( this._boxEastName );
			this._groupBoxNames.Controls.Add( this._labelSouthName );
			this._groupBoxNames.Controls.Add( this._boxSouthName );
			this._groupBoxNames.Controls.Add( this._boxNorthName );
			this._groupBoxNames.Controls.Add( this._labelNorthName );
			this._groupBoxNames.Controls.Add( this._labelEastName );
			resources.ApplyResources( this._groupBoxNames, "_groupBoxNames" );
			this._groupBoxNames.Name = "_groupBoxNames";
			this._groupBoxNames.TabStop = false;
			// 
			// _boxWestName
			// 
			resources.ApplyResources( this._boxWestName, "_boxWestName" );
			this._boxWestName.Name = "_boxWestName";
			// 
			// _labelWestName
			// 
			resources.ApplyResources( this._labelWestName, "_labelWestName" );
			this._labelWestName.Name = "_labelWestName";
			// 
			// _boxEastName
			// 
			resources.ApplyResources( this._boxEastName, "_boxEastName" );
			this._boxEastName.Name = "_boxEastName";
			// 
			// _labelSouthName
			// 
			resources.ApplyResources( this._labelSouthName, "_labelSouthName" );
			this._labelSouthName.Name = "_labelSouthName";
			// 
			// _boxSouthName
			// 
			resources.ApplyResources( this._boxSouthName, "_boxSouthName" );
			this._boxSouthName.Name = "_boxSouthName";
			// 
			// _boxNorthName
			// 
			resources.ApplyResources( this._boxNorthName, "_boxNorthName" );
			this._boxNorthName.Name = "_boxNorthName";
			// 
			// _labelNorthName
			// 
			resources.ApplyResources( this._labelNorthName, "_labelNorthName" );
			this._labelNorthName.Name = "_labelNorthName";
			// 
			// _labelEastName
			// 
			resources.ApplyResources( this._labelEastName, "_labelEastName" );
			this._labelEastName.Name = "_labelEastName";
			// 
			// _tabAdvanced
			// 
			this._tabAdvanced.BackColor = System.Drawing.Color.Transparent;
			this._tabAdvanced.Controls.Add( this._groupBoxCards );
			this._tabAdvanced.Controls.Add( this._groupBoxRules );
			this._tabAdvanced.Controls.Add( this._gbDlls );
			resources.ApplyResources( this._tabAdvanced, "_tabAdvanced" );
			this._tabAdvanced.Name = "_tabAdvanced";
			this._tabAdvanced.UseVisualStyleBackColor = true;
			// 
			// _groupBoxCards
			// 
			this._groupBoxCards.Controls.Add( this._comboCards );
			resources.ApplyResources( this._groupBoxCards, "_groupBoxCards" );
			this._groupBoxCards.Name = "_groupBoxCards";
			this._groupBoxCards.TabStop = false;
			// 
			// _comboCards
			// 
			this._comboCards.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboCards.FormattingEnabled = true;
			resources.ApplyResources( this._comboCards, "_comboCards" );
			this._comboCards.Name = "_comboCards";
			// 
			// _groupBoxRules
			// 
			this._groupBoxRules.Controls.Add( this._labelExtraDouble );
			this._groupBoxRules.Controls.Add( this._labelCapotDouble );
			this._groupBoxRules.Controls.Add( this._checkCapotDouble );
			this._groupBoxRules.Controls.Add( this._checkExtraDouble );
			resources.ApplyResources( this._groupBoxRules, "_groupBoxRules" );
			this._groupBoxRules.Name = "_groupBoxRules";
			this._groupBoxRules.TabStop = false;
			// 
			// _labelExtraDouble
			// 
			this._labelExtraDouble.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelExtraDouble, "_labelExtraDouble" );
			this._labelExtraDouble.Name = "_labelExtraDouble";
			// 
			// _labelCapotDouble
			// 
			this._labelCapotDouble.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelCapotDouble, "_labelCapotDouble" );
			this._labelCapotDouble.Name = "_labelCapotDouble";
			// 
			// _checkCapotDouble
			// 
			this._checkCapotDouble.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._checkCapotDouble, "_checkCapotDouble" );
			this._checkCapotDouble.Name = "_checkCapotDouble";
			this._checkCapotDouble.UseVisualStyleBackColor = false;
			// 
			// _checkExtraDouble
			// 
			this._checkExtraDouble.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._checkExtraDouble, "_checkExtraDouble" );
			this._checkExtraDouble.Name = "_checkExtraDouble";
			this._checkExtraDouble.UseVisualStyleBackColor = false;
			// 
			// _gbDlls
			// 
			this._gbDlls.Controls.Add( this._comboAIWest );
			this._gbDlls.Controls.Add( this._comboAINorth );
			this._gbDlls.Controls.Add( this._comboAIEast );
			this._gbDlls.Controls.Add( this._labelWest );
			this._gbDlls.Controls.Add( this._labelNorth );
			this._gbDlls.Controls.Add( this._labelEast );
			resources.ApplyResources( this._gbDlls, "_gbDlls" );
			this._gbDlls.Name = "_gbDlls";
			this._gbDlls.TabStop = false;
			// 
			// _comboAIWest
			// 
			this._comboAIWest.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboAIWest.FormattingEnabled = true;
			resources.ApplyResources( this._comboAIWest, "_comboAIWest" );
			this._comboAIWest.Name = "_comboAIWest";
			// 
			// _comboAINorth
			// 
			this._comboAINorth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboAINorth.FormattingEnabled = true;
			resources.ApplyResources( this._comboAINorth, "_comboAINorth" );
			this._comboAINorth.Name = "_comboAINorth";
			// 
			// _comboAIEast
			// 
			this._comboAIEast.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboAIEast.FormattingEnabled = true;
			resources.ApplyResources( this._comboAIEast, "_comboAIEast" );
			this._comboAIEast.Name = "_comboAIEast";
			// 
			// _labelWest
			// 
			this._labelWest.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelWest, "_labelWest" );
			this._labelWest.Name = "_labelWest";
			// 
			// _labelNorth
			// 
			this._labelNorth.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelNorth, "_labelNorth" );
			this._labelNorth.Name = "_labelNorth";
			// 
			// _labelEast
			// 
			this._labelEast.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources( this._labelEast, "_labelEast" );
			this._labelEast.Name = "_labelEast";
			// 
			// _buttonCancel
			// 
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources( this._buttonCancel, "_buttonCancel" );
			this._buttonCancel.Name = "_buttonCancel";
			// 
			// _buttonOK
			// 
			resources.ApplyResources( this._buttonOK, "_buttonOK" );
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Click += new System.EventHandler( this.ButtonOKClick );
			// 
			// SettingsForm
			// 
			resources.ApplyResources( this, "$this" );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add( this._tabControl );
			this.Controls.Add( this._buttonCancel );
			this.Controls.Add( this._buttonOK );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "SettingsForm";
			this.ShowInTaskbar = false;
			this._tabControl.ResumeLayout( false );
			this._tabGeneral.ResumeLayout( false );
			this._groupBoxLanguage.ResumeLayout( false );
			this._groupBoxSpeed.ResumeLayout( false );
			this._groupBoxSpeed.PerformLayout();
			( ( System.ComponentModel.ISupportInitialize )( this._trackSpeed ) ).EndInit();
			this._groupBoxNames.ResumeLayout( false );
			this._groupBoxNames.PerformLayout();
			this._tabAdvanced.ResumeLayout( false );
			this._groupBoxCards.ResumeLayout( false );
			this._groupBoxRules.ResumeLayout( false );
			this._gbDlls.ResumeLayout( false );
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.TabControl _tabControl;
		private System.Windows.Forms.TabPage _tabGeneral;
		private System.Windows.Forms.GroupBox _groupBoxLanguage;
		private System.Windows.Forms.ComboBox _comboLanguage;
		private System.Windows.Forms.GroupBox _groupBoxSpeed;
		private System.Windows.Forms.TrackBar _trackSpeed;
		private System.Windows.Forms.GroupBox _groupBoxNames;
		private System.Windows.Forms.TextBox _boxWestName;
		private System.Windows.Forms.Label _labelWestName;
		private System.Windows.Forms.TextBox _boxEastName;
		private System.Windows.Forms.Label _labelSouthName;
		private System.Windows.Forms.TextBox _boxSouthName;
		private System.Windows.Forms.TextBox _boxNorthName;
		private System.Windows.Forms.Label _labelNorthName;
		private System.Windows.Forms.Label _labelEastName;
		private System.Windows.Forms.TabPage _tabAdvanced;
		private System.Windows.Forms.GroupBox _groupBoxCards;
		private System.Windows.Forms.GroupBox _groupBoxRules;
		private System.Windows.Forms.Label _labelExtraDouble;
		private System.Windows.Forms.Label _labelCapotDouble;
		private System.Windows.Forms.CheckBox _checkCapotDouble;
		private System.Windows.Forms.CheckBox _checkExtraDouble;
		private System.Windows.Forms.GroupBox _gbDlls;
		private System.Windows.Forms.Label _labelWest;
		private System.Windows.Forms.Label _labelNorth;
		private System.Windows.Forms.Label _labelEast;
		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.ComboBox _comboAIEast;
		private System.Windows.Forms.ComboBox _comboAIWest;
		private System.Windows.Forms.ComboBox _comboAINorth;
		private System.Windows.Forms.ComboBox _comboCards;
	}
}