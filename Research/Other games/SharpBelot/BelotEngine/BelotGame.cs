/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System.Collections.Generic;

namespace Belot
{
	/// <summary>
	/// Belot Game.
	/// </summary>
	public class BelotGame 
	{
		#region Fields
		private IList< Deal > _deals;
		private Deal _currentDeal;
		private Player _southPlayer;
		private Player _eastPlayer;
		private Player _northPlayer;
		private Player _westPlayer;
		private Player _firstPlayer;

		private bool _capotRemovesDouble = true;
		private bool _extraPointsAreDoubled = true;

		private int _totalNorthSouthPoints = 0;
		private int _totalEastWestPoints = 0;
		private int _hangingPoints = 0;
		#endregion

		#region Constructor
		/// <summary>
		/// Creates new belot game
		/// </summary>
		public BelotGame( Player south, Player east, Player north, Player west )
		{
			_southPlayer = south; 
			_eastPlayer = east; 
			_northPlayer = north; 
			_westPlayer = west;

			_southPlayer.SetPosition( PlayerPosition.South );
			_eastPlayer.SetPosition( PlayerPosition.East );
			_northPlayer.SetPosition( PlayerPosition.North );
			_westPlayer.SetPosition( PlayerPosition.West );

			_firstPlayer = _southPlayer;

			_deals = new List< Deal >();
		}		

		#endregion

		#region Events
		
			#region BiddingCompleted
			/// <summary>
			/// Handler delegate for the BiddingCompleted event.
			/// </summary>
			public delegate void BiddingCompletedHandler ( Player winner, Announcement announce );
			
			/// <summary>
			/// Occurs when the bidding ( announcement ) phase of the game has finished.
			/// </summary>
			public event BiddingCompletedHandler BiddingCompleted;
		
			/// <summary>
			/// Raises the BiddingCompleted event
			/// </summary>
			protected void RaiseBiddingCompleted( Player winner, Announcement announce )
			{
				if( BiddingCompleted != null )
				{
					BiddingCompleted( winner, announce );
				}
			}
			#endregion

			#region GameCompleted
					
			/// <summary>
			/// Handler delegate for the GameCompleted events.
			/// </summary>
			public delegate void GameCompletedHandler( Player winner1, Player winner2 );

			/// <summary>
			/// One of the teams has passed 151 points. Game over,
			/// </summary>
			public event GameCompletedHandler GameCompleted;
				
			/// <summary>
			/// Raises the DealStarted event
			/// </summary>
			protected void RaiseGameCompleted( Player winner1, Player winner2 )
			{
				if( GameCompleted != null )
				{
					GameCompleted( winner1, winner2 );
				}
			}
			#endregion

			#region DealStarted
					
			/// <summary>
			/// Occurs when a new deal has started.
			/// </summary>
			public event DealEventHandler DealStarted;
			
			#endregion

			#region DealCompleted 
			/// <summary>
			/// Handler delegate for the Deal events.
			/// </summary>
			public delegate void DealEventHandler( );
				
			/// <summary>
			/// Occurs when the deal has finished.
			/// </summary>
			public event DealEventHandler DealCompleted;
			
			#endregion

		#region HandClosed
			/// <summary>
			/// Handler delegate for the HandClosed event.
			/// </summary>
			public delegate void HandClosedHandler( Hand hand );
				
			/// <summary>
			/// Occurs when four cards have passed.
			/// </summary>
			public event HandClosedHandler HandClosed;
			
			/// <summary>
			/// Raises the HandClosed event
			/// </summary>
			protected void RaiseHandClosed( Hand hand )
			{
				if( HandClosed != null )
				{
					HandClosed( hand );
				}
			}
			#endregion

		#endregion

		#region Properties
		/// <summary>
		/// The points that North-South team won in the current game.
		/// </summary>
		public int TotalNorthSouthPoints
		{
			get
			{
				return _totalNorthSouthPoints;
			}
		}

		/// <summary>
		/// The points that East-West team won in the current game.
		/// </summary>
		public int TotalEastWestPoints
		{
			get
			{
				return _totalEastWestPoints;
			}
		}

		/// <summary>
		/// Represents the current deal during the game.
		/// </summary>
		public Deal CurrentDeal
		{
			get { return _currentDeal; }
		}

		/// <summary>
		/// Gets or sets whether if a deal is capot the doubling is discarded
		/// </summary>
		public bool CapotRemovesDouble
		{
			get 
			{ 
				return _capotRemovesDouble; 
			}
			set 
			{ 
				_capotRemovesDouble = value; 
			}
		}

		/// <summary>
		/// Gets or sets whether extra points are doubled (from combinations, capot, etc)
		/// </summary>
		public bool ExtraPointsAreDoubled
		{
			get
			{ 
				return _extraPointsAreDoubled; 
			}
			set 
			{ 
				_extraPointsAreDoubled = value;
			}
		}

		/// <summary>
		/// Gets how many points are hanging for next deal
		/// </summary>
		public int HangingPoints
		{
			get
			{ 
				return _hangingPoints;
			}
		}
		
		#endregion	

		#region Internal and Private Methods
		private void NextDeal()
		{
			_southPlayer.Cards.Clear();
			_eastPlayer.Cards.Clear();
			_northPlayer.Cards.Clear();
			_westPlayer.Cards.Clear();

			_currentDeal = new Deal( this, _firstPlayer );

			_firstPlayer = GetNextPlayer( _firstPlayer );
			_deals.Add( _currentDeal );
			_currentDeal.DealFirstCards();
		
			Player winner = _currentDeal.EnterBiddingState();
			if( _currentDeal.CurrentAnnouncement.Type == AnnouncementTypeEnum.Pass )
			{
				_southPlayer.Cards.Clear();
				_eastPlayer.Cards.Clear();
				_northPlayer.Cards.Clear();
				_westPlayer.Cards.Clear();
				FinalizeDeal();
				return;
			}

			RaiseBiddingCompleted( winner, _currentDeal.CurrentAnnouncement );

			_currentDeal.DealRestCards();

			_southPlayer.CardPlayed += new Player.CardPlayedHandler( _currentDeal.PlayerPlayedCard );
			_northPlayer.CardPlayed += new Player.CardPlayedHandler( _currentDeal.PlayerPlayedCard );
			_eastPlayer.CardPlayed += new Player.CardPlayedHandler( _currentDeal.PlayerPlayedCard );
			_westPlayer.CardPlayed += new Player.CardPlayedHandler( _currentDeal.PlayerPlayedCard );

			_currentDeal.EnterPlayingState();
		}

		internal Player GetNextPlayer( Player currentPlayer )
		{
			if( currentPlayer == _southPlayer )
				return _eastPlayer;
			if( currentPlayer == _eastPlayer )
				return _northPlayer;
			if( currentPlayer == _northPlayer )
				return _westPlayer;
			if( currentPlayer == _westPlayer )
				return _southPlayer;

			return _southPlayer;
		}		
		
		internal Player GetTeamPlayer( Player currentPlayer )
		{
			if( currentPlayer == _southPlayer )
				return _northPlayer;
			if( currentPlayer == _eastPlayer )
				return _westPlayer;
			if( currentPlayer == _northPlayer )
				return _southPlayer;
			if( currentPlayer == _westPlayer )
				return _eastPlayer;

			return _southPlayer;
		}

		internal void HandIsClosed( Hand hand )
		{
			RaiseHandClosed( hand );
		}
		/// <summary>
		/// Raises the DealCompleted event
		/// </summary>
		internal void RaiseDealCompleted( )
		{
			_hangingPoints += _currentDeal.HangingPoints;

			if( _hangingPoints != 0 && _currentDeal.HangingPoints == 0 )
			{
				// a previous game was hanging and its time to add the points
				if( _currentDeal.RoundedNorthSouthPoints > _currentDeal.RoundedEastWestPoints )
				{
					_currentDeal.AddNorthSouthHangingPoints( _hangingPoints );
				}
				else
				{
					_currentDeal.AddEastWesthHangingPoints( _hangingPoints );
				}

				_hangingPoints = 0;
			}

			_totalNorthSouthPoints += _currentDeal.RoundedNorthSouthPoints;
			_totalEastWestPoints += _currentDeal.RoundedEastWestPoints;

			_southPlayer.CardPlayed -= new Player.CardPlayedHandler( _currentDeal.PlayerPlayedCard );
			_northPlayer.CardPlayed -= new Player.CardPlayedHandler( _currentDeal.PlayerPlayedCard );
			_eastPlayer.CardPlayed -= new Player.CardPlayedHandler( _currentDeal.PlayerPlayedCard );
			_westPlayer.CardPlayed -= new Player.CardPlayedHandler( _currentDeal.PlayerPlayedCard );

			DealCompleted( );

			FinalizeDeal();
		}
		/// <summary>
		/// Raises the DealStarted event
		/// </summary>
		internal void RaiseDealStarted( )
		{
			if( DealStarted != null )
			{
				DealStarted( );
			}
		}

		private void FinalizeDeal()
		{
			if(	(TotalNorthSouthPoints < 151 && TotalEastWestPoints < 151) ||
				(TotalNorthSouthPoints == TotalEastWestPoints ) ||
				_currentDeal.IsCapot )
			{
				NextDeal();
			}
			else
			{
				if( TotalNorthSouthPoints > TotalEastWestPoints )
				{
					RaiseGameCompleted( _northPlayer, _southPlayer );
				}
				else
				{
					RaiseGameCompleted( _eastPlayer, _westPlayer );
				}
			}
		}

		#endregion

		#region Public Methods
		/// <summary>
		/// Starts a new belot game.
		/// </summary>
		public void StartGame()
		{
			NextDeal();
		}

//		/// <summary>
//		/// Gets the position of a given player on the table.
//		/// </summary>
//		public PlayerPosition GetPosition( Player player )
//		{
//			if( player == _southPlayer )
//				return PlayerPosition.South;
//			if( player == _eastPlayer )
//				return PlayerPosition.East;
//			if( player == _northPlayer )
//				return PlayerPosition.North;
//			if( player == _westPlayer )
//				return PlayerPosition.West;
//
//			return PlayerPosition.South;
//		}

		/// <summary>
		/// Gets the player of a given position on the table.
		/// </summary>
		public Player GetPlayer( PlayerPosition position )
		{
			switch ( position )
			{
				case PlayerPosition.South:
					return _southPlayer;
				case PlayerPosition.East:
					return _eastPlayer;
				case PlayerPosition.North:
					return _northPlayer;
				case PlayerPosition.West:
					return _westPlayer;
				default:
					return _southPlayer;
			}
		}
		#endregion
	}
}
