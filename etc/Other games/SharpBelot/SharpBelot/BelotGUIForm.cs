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
using System.Reflection;
using System.Threading;

namespace SharpBelot
{
	public partial class BelotGUIForm : Form
	{
		#region Fields

		private HumanPlayer _currentHuman = null;
		private BelotGame _game = null;

		private DataTable _resultsTable = null;

		private AnnounceForm _announceForm = null;
		private SettingsForm _settingsForm = null;
		private ResultForm _resultForm = null;
		private DealResultForm _dealResultForm = null;
		private CombinationForm _combinationForm = null;
		private PassedAnnouncesForm _passedAnnouncesForm = null;

		private CardAnimator _animator = null;

		#endregion

		public BelotGUIForm()
		{
			System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo( Properties.Settings.Default.Culture );
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo( Properties.Settings.Default.Culture );
			StringResources.Culture = new System.Globalization.CultureInfo( Properties.Settings.Default.Culture );

			InitializeComponent();


#if DEBUG			
			_panelEast.AreBacks = false;
			_panelNorth.AreBacks = false;
			_panelWest.AreBacks = false;
#else
			_panelEast.AreBacks = true;
			_panelNorth.AreBacks = true;
			_panelWest.AreBacks = true;
#endif

			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.DoubleBuffer, true );
			this.SetStyle( ControlStyles.Opaque, false );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.Selectable, false );


			Font mistralBold = new Font( Properties.Settings.Default.MistralFont, FontStyle.Bold );
			_labelSouth.Font = mistralBold;
			_labelNorth.Font = mistralBold;
			_labelEast.Font = mistralBold;
			_labelWest.Font = mistralBold;
			_labelSouth.Text = Properties.Settings.Default.SouthName;
			_labelNorth.Text = Properties.Settings.Default.NorthName;
			_labelEast.Text = Properties.Settings.Default.EastName;
			_labelWest.Text = Properties.Settings.Default.WestName;

			_announceForm = new AnnounceForm();
			this.AddOwnedForm( _announceForm );

			_settingsForm = new SettingsForm();
			this.AddOwnedForm( _settingsForm );

			_resultsTable = new DataTable( "ResultsTable" );
			_resultsTable.Columns.Add( "We", typeof( int ) );
			_resultsTable.Columns.Add( "You", typeof( int ) );

			_resultForm = new ResultForm( _resultsTable );
			this.AddOwnedForm( _resultForm );

			_dealResultForm = new DealResultForm();
			this.AddOwnedForm( _dealResultForm );

			_combinationForm = new CombinationForm();
			this.AddOwnedForm( _combinationForm );

			_passedAnnouncesForm = new PassedAnnouncesForm( this );
			this.AddOwnedForm( _passedAnnouncesForm );
			_passedAnnouncesForm.Show();

			_panelSouth.CardClick += new CardClicked( Card_Click );
			_panelEast.CardClick += new CardClicked( Card_Click );
			_panelNorth.CardClick += new CardClicked( Card_Click );
			_panelWest.CardClick += new CardClicked( Card_Click );

			_animator = new CardAnimator( _animationCard );
			_animator.AnimationFinished += new AnimationFinishHandler( AnimationFinished );

			this.Paint += new PaintEventHandler( BelotGUIForm_Paint );
		}

		#region Private Methods

		private void BelotGUIForm_Load( object sender, EventArgs e )
		{
			CardGUIProvider.Reload();
		}

		private void MenuNew_Click( object sender, EventArgs e )
		{
			this.Text = "";

			HumanPlayer pl1 = new HumanPlayer( Properties.Settings.Default.SouthName );
			ComputerPlayer pl2 = CreatePlayer( Properties.Settings.Default.NorthDll, Properties.Settings.Default.NorthName );
			ComputerPlayer pl3 = CreatePlayer( Properties.Settings.Default.EastDll, Properties.Settings.Default.EastName );
			ComputerPlayer pl4 = CreatePlayer( Properties.Settings.Default.WestDll, Properties.Settings.Default.WestName );

			if ( pl2 == null || pl3 == null || pl4 == null )
			{
				MessageBox.Show( "Player not properly configured. See 'Settings->Players' for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
				return;
			}

			pl1.AnnounceMaking += new HumanPlayer.AnnounceMakingHandler( HumanPlayerIsBidding );
			pl2.AnnounceMade += new ComputerPlayer.AnnounceMadeHandler( CompPlayerBidded );
			pl3.AnnounceMade += new ComputerPlayer.AnnounceMadeHandler( CompPlayerBidded );
			pl4.AnnounceMade += new ComputerPlayer.AnnounceMadeHandler( CompPlayerBidded );

			pl1.CardPlaying += new Player.CardPlayingHandler( HumanPlayerIsPlaying );
			pl2.CardPlayed += new Player.CardPlayedHandler( ComputerPlayerPlayed );
			pl3.CardPlayed += new Player.CardPlayedHandler( ComputerPlayerPlayed );
			pl4.CardPlayed += new Player.CardPlayedHandler( ComputerPlayerPlayed );

			pl1.CardsChanged += new Player.PlayerCardsChangedHandler( DrawCards );
			pl2.CardsChanged += new Player.PlayerCardsChangedHandler( DrawCards );
			pl3.CardsChanged += new Player.PlayerCardsChangedHandler( DrawCards );
			pl4.CardsChanged += new Player.PlayerCardsChangedHandler( DrawCards );

			pl1.CardCombinationAnnouncing += new HumanPlayer.CardCombinationAnnouncingHandler( HumanCardCombinationAnnouncing );
			pl2.CardCombinationAnnounced += new ComputerPlayer.CardCombinationAnnouncedHandler( CombinationAnnounced );
			pl3.CardCombinationAnnounced += new ComputerPlayer.CardCombinationAnnouncedHandler( CombinationAnnounced );
			pl4.CardCombinationAnnounced += new ComputerPlayer.CardCombinationAnnouncedHandler( CombinationAnnounced );

			this._game = new BelotGame( pl1, pl3, pl2, pl4 );
			this._game.BiddingCompleted += new BelotGame.BiddingCompletedHandler( BiddingCompleted );
			this._game.DealStarted += new BelotGame.DealEventHandler( DealStarted );
			this._game.DealCompleted += new BelotGame.DealEventHandler( DealCompleted );
			this._game.HandClosed += new BelotGame.HandClosedHandler( HandClosed );
			this._game.GameCompleted += new BelotGame.GameCompletedHandler( GameOver );

			this._game.CapotRemovesDouble = Properties.Settings.Default.CapotRemovesDouble;
			this._game.ExtraPointsAreDoubled = Properties.Settings.Default.ExtraPointsAreDoubled;

			this._game.StartGame();
		}

		private void MenuSettings_Click( object sender, EventArgs e )
		{
			if ( _settingsForm.ShowDialog() == DialogResult.OK )
			{
				_labelSouth.Text = Properties.Settings.Default.SouthName;
				_labelNorth.Text = Properties.Settings.Default.NorthName;
				_labelEast.Text = Properties.Settings.Default.EastName;
				_labelWest.Text = Properties.Settings.Default.WestName;

				ChangeResources();
			}
		}

		private void MenuResults_Click( object sender, EventArgs e )
		{
			if ( _game != null )
			{
				_resultForm.HangingPoints = _game.HangingPoints;
			}
			_resultForm.ShowDialog();
		}

		private void MenuAbout_Click( object sender, EventArgs e )
		{
			using ( AboutForm about = new AboutForm() )
			{
				about.ShowDialog();
			}
		}

		private void MenuExit_Click( object sender, EventArgs e )
		{
			this.Close();
		}

		private void Card_Click( CardsPanel panel, CardClickedEventArgs e )
		{
			MoveCardToTable( _currentHuman.Position, e.Card );
			_currentHuman.PlayCard( e.Card );
		}

		public void ChangeResources()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( this.GetType() );
			resources.ApplyResources( this._picPos, "_picPos" );
			resources.ApplyResources( this._menuMain, "_menuMain" );
			resources.ApplyResources( this._menuGame, "_menuGame" );
			resources.ApplyResources( this._menuNew, "_menuNew" );
			resources.ApplyResources( this._menuResults, "_menuResults" );
			resources.ApplyResources( this._menuSettings, "_menuSettings" );
			resources.ApplyResources( this._menuExit, "_menuExit" );
			resources.ApplyResources( this._menuAbout, "_menuAbout" );
			resources.ApplyResources( this._panelPlayingTable, "_panelPlayingTable" );
			resources.ApplyResources( this._panelSouth, "_panelSouth" );
			resources.ApplyResources( this._panelNorth, "_panelNorth" );
			resources.ApplyResources( this._panelEast, "_panelEast" );
			resources.ApplyResources( this._panelWest, "_panelWest" );
			resources.ApplyResources( this._cardTableWest, "_cardTableWest" );
			resources.ApplyResources( this._cardTableEast, "_cardTableEast" );
			resources.ApplyResources( this._cardTableSouth, "_cardTableSouth" );
			resources.ApplyResources( this._cardTableNorth, "_cardTableNorth" );
			resources.ApplyResources( this, "$this" );

			_resultForm.ChangeResources();
			_announceForm.ChangeResources();
			_combinationForm.ChangeResources();
			_dealResultForm.ChangeResources();
			_passedAnnouncesForm.ChangeResources();
			_resultForm.ChangeResources();
			_settingsForm.ChangeResources();

			StringResources.Culture = Thread.CurrentThread.CurrentCulture;
		}

		/// <summary>
		/// Gets a string representation of an Announcement
		/// </summary>
		private string GetAnnouncementString( Announcement announce )
		{
			string str = "";
			switch ( announce.Type )
			{
				case AnnouncementTypeEnum.AllTrumps:
					str = StringResources.allTrumps;
					break;
				case AnnouncementTypeEnum.NoTrumps:
					str = StringResources.noTrumps;
					break;
				case AnnouncementTypeEnum.Spades:
					str = StringResources.spades;
					break;
				case AnnouncementTypeEnum.Hearts:
					str = StringResources.hearts;
					break;
				case AnnouncementTypeEnum.Diamonds:
					str = StringResources.diamonds;
					break;
				case AnnouncementTypeEnum.Clubs:
					str = StringResources.clubs;
					break;
				case AnnouncementTypeEnum.Pass:
					str = StringResources.pass;
					break;
			}

			if ( announce.IsDoubled )
			{
				str = StringResources.doubled;
			}
			if ( announce.IsReDoubled )
			{
				str = StringResources.redoubled;
			}

			return str;
		}

		/// <summary>
		/// Gets a string representation of a Combination
		/// </summary>
		private string GetCombinationString( CardCombination combination )
		{
			string str = "";

			if ( combination is FourEqualsCombination )
			{
				str = StringResources.fourEqual;
			}

			if ( combination is BelotCombination )
			{
				str = StringResources.belot;
			}

			if ( combination is SequentialCombination )
			{
				if ( combination.Points == 20 )
				{
					str = StringResources.threeSeq;
				}

				if ( combination.Points == 50 )
				{
					str = StringResources.fourSeq;
				}

				if ( combination.Points == 100 )
				{
					str = StringResources.fiveSeq;
				}
			}

			return str;
		}

		#endregion

		#region Human player methods

		private Announcement HumanPlayerIsBidding( Player player, AnnouncementManager manager )
		{
			SetPlayerActive( player );
			_announceForm.Bid( player, manager );
			_announceForm.ShowDialog();
			this.Update();

			Announcement currentAnnounce = _announceForm.Announce;

			this.Text = StringResources.lastBid;
			this.Text += GetAnnouncementString( currentAnnounce );
			this.Text += StringResources.saidBy;
			this.Text += player.Name;

			bool isActive = currentAnnounce.CompareTo( _game.CurrentDeal.CurrentAnnouncement ) > 0;
			_passedAnnouncesForm.AddMessage( player.Name, GetAnnouncementString( currentAnnounce ), isActive );

			return currentAnnounce;
		}

		private void HumanPlayerIsPlaying( Player player, PlayingManager manager )
		{
			SetPlayerActive( player );
			_currentHuman = ( HumanPlayer )player;
		}

		private bool HumanCardCombinationAnnouncing( Player player, CardCombination combination )
		{
			bool isAnnounced = false;

			_combinationForm.Combination = combination;
			isAnnounced = ( _combinationForm.ShowDialog() == DialogResult.OK );

			if ( isAnnounced )
			{
				CombinationAnnounced( player, combination );
			}

			return isAnnounced;
		}

		#endregion

		#region Computer player methods

		private ComputerPlayer CreatePlayer( string assemblyName, string playerName )
		{
			ComputerPlayer player = null;

			Assembly a = Assembly.LoadFrom( assemblyName );
			Type[] types = a.GetExportedTypes();
			foreach ( Type t in types )
			{
				if ( t.IsSubclassOf( typeof( ComputerPlayer ) ) )
				{
					object[] o = new object[ 1 ];
					o[ 0 ] = playerName;
					player = Activator.CreateInstance( t, o ) as ComputerPlayer;
				}
			}

			return player;
		}

		private void CompPlayerBidded( Player player, Announcement currentAnnounce )
		{
			SetPlayerActive( null );

			bool isActive = currentAnnounce.CompareTo( _game.CurrentDeal.CurrentAnnouncement ) > 0;
			_passedAnnouncesForm.AddMessage( player.Name, GetAnnouncementString( currentAnnounce ), isActive );

			Thread.Sleep( 1000 - Properties.Settings.Default.Speed*50 );

			this.Text = StringResources.lastBid;
			this.Text += GetAnnouncementString( currentAnnounce );
			this.Text += StringResources.saidBy;
			this.Text += player.Name;
		}

		private void ComputerPlayerPlayed( Player player, Card playedCard )
		{
			SetPlayerActive( null );
			MoveCardToTable( player.Position, playedCard );
		}

		#endregion

		#region Drawing methods

		private void BelotGUIForm_Paint( object sender, PaintEventArgs e )
		{
			e.Graphics.DrawImage( Properties.Resources.roundtable, 0, _menuMain.Height, Width - 15, Height - 55 );
		}

		private void DrawCards( Player player, CardsCollection cards )
		{
			switch ( player.Position )
			{
				case PlayerPosition.South:
					_panelSouth.Cards = cards;
					break;
				case PlayerPosition.East:
					_panelEast.Cards = cards;
					break;
				case PlayerPosition.North:
					_panelNorth.Cards = cards;
					break;
				case PlayerPosition.West:
					_panelWest.Cards = cards;
					break;
				default:
					_panelSouth.Cards = cards;
					break;
			}
		}

		private void SetPlayerActive( Player player )
		{
			if ( player == null )
			{
				_picPos.Image = Properties.Resources.AllPos;
			}
			else
			{
				switch ( player.Position )
				{
					case PlayerPosition.South:
						_picPos.Image = Properties.Resources.SouthPos;
						break;
					case PlayerPosition.East:
						_picPos.Image = Properties.Resources.EastPos;
						break;
					case PlayerPosition.North:
						_picPos.Image = Properties.Resources.NorthPos;
						break;
					case PlayerPosition.West:
						_picPos.Image = Properties.Resources.WestPos;
						break;
				}
			}
		}

		private void MoveCardToTable( PlayerPosition position, Card playedCard )
		{
			switch ( position )
			{
				case PlayerPosition.South:
					_cardTableSouth.Visible = true;
					_cardTableSouth.Card = playedCard;
					break;
				case PlayerPosition.East:
					_cardTableEast.Visible = true;
					_cardTableEast.Card = playedCard;
					break;
				case PlayerPosition.North:
					_cardTableNorth.Visible = true;
					_cardTableNorth.Card = playedCard;
					break;
				case PlayerPosition.West:
					_cardTableWest.Visible = true;
					_cardTableWest.Card = playedCard;
					break;
			}
		}

		private void AnimateHand( PlayerPosition position )
		{
			Point destination;

			_panelSouth.Enabled = false;
			_panelEast.Enabled = false;
			_panelNorth.Enabled = false;
			_panelWest.Enabled = false;

			switch ( position )
			{
				case PlayerPosition.South:
					destination = new Point( _cardTableSouth.Left + _panelPlayingTable.Left, this.Height );

					_animator.EnqueueControl( _cardTableSouth );
					_animator.EnqueueControl( _cardTableEast );
					_animator.EnqueueControl( _cardTableNorth );
					_animator.EnqueueControl( _cardTableWest );
					_animator.Animate( destination );
					break;
				case PlayerPosition.East:
					destination = new Point( this.Width, _cardTableWest.Top + _panelPlayingTable.Top );

					_animator.EnqueueControl( _cardTableEast );
					_animator.EnqueueControl( _cardTableNorth );
					_animator.EnqueueControl( _cardTableWest );
					_animator.EnqueueControl( _cardTableSouth );
					_animator.Animate( destination );
					break;
				case PlayerPosition.North:
					destination = new Point( _cardTableNorth.Left + _panelPlayingTable.Left, -_cardTableNorth.Height );

					_animator.EnqueueControl( _cardTableNorth );
					_animator.EnqueueControl( _cardTableWest );
					_animator.EnqueueControl( _cardTableSouth );
					_animator.EnqueueControl( _cardTableEast );
					_animator.Animate( destination );
					break;
				case PlayerPosition.West:
					destination = new Point( -_cardTableEast.Width, _cardTableEast.Top + _panelPlayingTable.Top );

					_animator.EnqueueControl( _cardTableWest );
					_animator.EnqueueControl( _cardTableSouth );
					_animator.EnqueueControl( _cardTableEast );
					_animator.EnqueueControl( _cardTableNorth );
					_animator.Animate( destination );
					break;
			}
		}

		private void AnimationFinished()
		{
			_panelSouth.Enabled = true;
			_panelEast.Enabled = true;
			_panelNorth.Enabled = true;
			_panelWest.Enabled = true;

			_game.CurrentDeal.PauseAfterHand = false;
		}

		#endregion

		#region Game methods

		private void BiddingCompleted( Player winner, Announcement finalAnnounce )
		{
			this.Text = StringResources.gameOf;
			this.Text = GetAnnouncementString( finalAnnounce );
			this.Text = StringResources.saidBy;
			this.Text = winner.Name;

			_passedAnnouncesForm.AddSpaces();
		}

		private void DealStarted()
		{
			_menuNew.Enabled = false;
			_menuSettings.Enabled = false;

			_passedAnnouncesForm.ClearMessages();
		}

		private void DealCompleted()
		{
			_menuNew.Enabled = true;
			_menuSettings.Enabled = true;

			_dealResultForm.Deal = _game.CurrentDeal;
			_dealResultForm.ShowDialog();
			this.Update();

			DataRow newRow = _resultsTable.NewRow();
			newRow[ "We" ] = _game.CurrentDeal.RoundedNorthSouthPoints;
			newRow[ "You" ] = _game.CurrentDeal.RoundedEastWestPoints;

			_resultsTable.Rows.Add( newRow );

			_resultForm.HangingPoints = _game.HangingPoints;

			_resultForm.ShowDialog();
			this.Update();
		}

		private void HandClosed( Hand hand )
		{
			Thread.Sleep( 1000 - Properties.Settings.Default.Speed * 20 );

			_game.CurrentDeal.PauseAfterHand = true;

			AnimateHand( hand.Winner.Position );

			_dealResultForm.AddHand( hand );
		}

		private void CombinationAnnounced( Player player, CardCombination combination )
		{
			_passedAnnouncesForm.AddMessage( player.Name, GetCombinationString( combination ), false );

			_dealResultForm.AddCombination( player, combination );
		}

		private void GameOver( Player winner1, Player winner2 )
		{
			MessageBox.Show( winner1.Name + StringResources.and + winner2.Name + StringResources.win, StringResources.congratulations, MessageBoxButtons.OK );
			_resultsTable.Rows.Clear();
		}

		#endregion

	}
}