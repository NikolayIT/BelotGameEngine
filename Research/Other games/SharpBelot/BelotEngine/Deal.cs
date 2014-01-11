/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections;
using System.Collections.Specialized;

namespace Belot
{
	/// <summary>
	/// A deal during the game.
	/// </summary>
	public class Deal
	{
		#region Fields
		private BelotGame _game;
		private Player _firstPlayer;	
		private Player _bidWinner;
		private CardsCollection _allCards;
		private CardsCollection _cards;
		private Announcement _currentAnnouncement;
		private int _northSouthPoints = 0;
		private int _eastWestPoints = 0;
		private int _rawNorthSouthPoints = 0;
		private int _rawEastWestPoints = 0;
		private int _hangingPoints = 0;
		private ListDictionary _mapEqualCombinationToPlayer;
		private ListDictionary _mapSequentialCombinationToPlayer;
		private ListDictionary _mapBelotCombinationToPlayer;
		private bool _isCapot = false;
		private PlayingManager _playingManager;
		private bool _isPaused = false;
		#endregion
		
		#region Properties
		/// <summary>
		/// The current announcement ( bid ) for the current deal.
		/// </summary>
		public Announcement CurrentAnnouncement
		{
			get { return _currentAnnouncement; }			
		}
	
		
		/// <summary>
		/// The points that North-South team won in the current deal, including 
		/// combinations and excluding doubling and capot.
		/// </summary>
		public int RawNorthSouthPoints
		{
			get { return _rawNorthSouthPoints;	}
		}

		/// <summary>
		/// The points that East-West team won in the current deal, including 
		/// combinations and excluding doubling and capot.
		/// </summary>
		public int RawEastWestPoints
		{
			get { return _rawEastWestPoints; }
		}

		/// <summary>
		/// The rounded points that North-South team won in the current deal.
		/// </summary>
		public int RoundedNorthSouthPoints
		{
			get
			{ 
				return RoundPoints( _northSouthPoints );
			}
		}

		/// <summary>
		/// The rounded points that East-West team won in the current deal.
		/// </summary>
		public int RoundedEastWestPoints
		{
			get
			{ 
				return RoundPoints( _eastWestPoints );
			}
		}

		/// <summary>
		/// Pauses deal after current hand. Next hand will not be played until set to true. 
		/// </summary>
		public bool PauseAfterHand
		{
			get
			{
				return _isPaused;
			}
			set
			{
				_isPaused = value;
				NextHand();
			}
		}

		#endregion

		#region Internal Properties
		/// <summary>
		/// The points that North-South team won in the current deal.
		/// </summary>
		internal int NorthSouthPoints
		{
			get { return _northSouthPoints;	}
		}

		/// <summary>
		/// The points that East-West team won in the current deal.
		/// </summary>
		internal int EastWestPoints
		{
			get { return _eastWestPoints; }
		}

		internal int HangingPoints
		{
			get
			{
				return _hangingPoints;
			}
		}

		internal bool IsCapot
		{
			get
			{
				return _isCapot;
			}
		}

		#endregion

		#region Constructors
		
		/// <summary>
		/// Constructor for the class. Creates a new deal during the current game.
		/// </summary>
		/// <param name="game"></param>
		/// <param name="firstPlayer"></param>
		internal Deal( BelotGame game, Player firstPlayer )
		{
			this._game = game;
			this._firstPlayer = firstPlayer;

			_mapEqualCombinationToPlayer = new ListDictionary();
			_mapSequentialCombinationToPlayer = new ListDictionary();
			_mapBelotCombinationToPlayer = new ListDictionary();

			_allCards = InitCards();
			_cards = new CardsCollection();

			foreach( Card card in _allCards )
			{
				_cards.Add( card );
			}

			_currentAnnouncement = new Announcement( AnnouncementTypeEnum.Pass, false, false );

			_game.RaiseDealStarted();
		}
		#endregion

		#region Private Methods
		private CardsCollection InitCards()
		{
			CardsCollection cards = new CardsCollection();

			// add spades
			cards.Add( new Card( CardType.Ace, CardColor.Spades ) );
			cards.Add( new Card( CardType.King, CardColor.Spades ) );
			cards.Add( new Card( CardType.Queen, CardColor.Spades ) );
			cards.Add( new Card( CardType.Jack, CardColor.Spades ) );
			cards.Add( new Card( CardType.Ten, CardColor.Spades ) );
			cards.Add( new Card( CardType.Nine, CardColor.Spades ) );
			cards.Add( new Card( CardType.Eight, CardColor.Spades ) );
			cards.Add( new Card( CardType.Seven, CardColor.Spades ) );

			// add hearts
			cards.Add( new Card( CardType.Ace, CardColor.Hearts ) );
			cards.Add( new Card( CardType.King, CardColor.Hearts ) );
			cards.Add( new Card( CardType.Queen, CardColor.Hearts ) );
			cards.Add( new Card( CardType.Jack, CardColor.Hearts ) );
			cards.Add( new Card( CardType.Ten, CardColor.Hearts ) );
			cards.Add( new Card( CardType.Nine, CardColor.Hearts ) );
			cards.Add( new Card( CardType.Eight, CardColor.Hearts ) );
			cards.Add( new Card( CardType.Seven, CardColor.Hearts ) );

			// add diamonds
			cards.Add( new Card( CardType.Ace, CardColor.Diamonds ) );
			cards.Add( new Card( CardType.King, CardColor.Diamonds ) );
			cards.Add( new Card( CardType.Queen, CardColor.Diamonds ) );
			cards.Add( new Card( CardType.Jack, CardColor.Diamonds ) );
			cards.Add( new Card( CardType.Ten, CardColor.Diamonds ) );
			cards.Add( new Card( CardType.Nine, CardColor.Diamonds ) );
			cards.Add( new Card( CardType.Eight, CardColor.Diamonds ) );
			cards.Add( new Card( CardType.Seven, CardColor.Diamonds ) );

			// add clubs
			cards.Add( new Card( CardType.Ace, CardColor.Clubs ) );
			cards.Add( new Card( CardType.King, CardColor.Clubs ) );
			cards.Add( new Card( CardType.Queen, CardColor.Clubs ) );
			cards.Add( new Card( CardType.Jack, CardColor.Clubs ) );
			cards.Add( new Card( CardType.Ten, CardColor.Clubs ) );
			cards.Add( new Card( CardType.Nine, CardColor.Clubs ) );
			cards.Add( new Card( CardType.Eight, CardColor.Clubs ) );
			cards.Add( new Card( CardType.Seven, CardColor.Clubs ) );

			return cards;			
		}

		private void DealCards( int count )
		{
			System.Random rand = new Random( DateTime.Now.Millisecond * DateTime.Now.Second );
	
			Player current = this._firstPlayer;

			for( int i = count; i > 0; i-- )
			{
				int index = rand.Next( i );
				
				current.Cards.Add( _cards[index] );
				current = this._game.GetNextPlayer( current );	

				_cards.RemoveAt( index );
			}
			
			CardComparer comparer = new CardComparer( this._currentAnnouncement.Type );

			this._game.GetPlayer( PlayerPosition.South ).Cards.Sort( comparer );
			this._game.GetPlayer( PlayerPosition.East ).Cards.Sort( comparer );
			this._game.GetPlayer( PlayerPosition.North ).Cards.Sort( comparer );
			this._game.GetPlayer( PlayerPosition.West ).Cards.Sort( comparer );

		}

		private void CalculatePoints( Hand lastHand )
		{
			PlayerPosition pos = lastHand.Winner.Position;
			if( pos == PlayerPosition.East || pos == PlayerPosition.West )
			{
				foreach( Card card in lastHand )
				{
					_eastWestPoints += CardPointEvaluator.EvaluateCard( this._currentAnnouncement.Type, card );
				}
			}
			else
			{
				foreach( Card card in lastHand )
				{
					_northSouthPoints += CardPointEvaluator.EvaluateCard( this._currentAnnouncement.Type, card );
				}
			}
		}

		private void AddLastTenPoints( Player winner )
		{
			PlayerPosition pos = winner.Position;
			if( pos == PlayerPosition.East || pos == PlayerPosition.West )
			{
				_eastWestPoints += 10;
			}
			else
			{
				_northSouthPoints += 10;
			}
		}

		private int RoundPoints( int points )
		{
			double ret = points / 10D;
			int halfPoints = (_eastWestPoints + _northSouthPoints) /2;
			int lastDigit = points%10;

			switch( _currentAnnouncement.Type )
			{
				case AnnouncementTypeEnum.NoTrumps:					
					ret = Math.Round( ret );
					break;
				case AnnouncementTypeEnum.AllTrumps:
					// round on 4 for the less points
					ret = Math.Round( ret );
					if( (lastDigit == 4) && ( points < halfPoints ) )
						ret++;
					break;
				case AnnouncementTypeEnum.Spades:
				case AnnouncementTypeEnum.Hearts:
				case AnnouncementTypeEnum.Diamonds:
				case AnnouncementTypeEnum.Clubs:
					// round on 6 for the less points
					ret = Math.Floor( ret );
					if( lastDigit > 6)
						ret++;
					else if( (lastDigit == 6)  && ( points < halfPoints ) )
						ret++;
					break;
			}

			return (int)ret;
		}

		private Player FindBiggestEqualCombinationHolder( )
		{
			CardCombination biggest = null;
			CardCombination second = null;

			Player biggestPlayer = null;
			Player secondPlayer = null;

			foreach( DictionaryEntry de in _mapEqualCombinationToPlayer )
			{
				CardCombination current = de.Key as CardCombination;

				if( biggest != null )
				{
					if( current.CompareTo( biggest ) > 1 )
					{
						biggest = current;
						biggestPlayer = de.Value as Player;
					}
					else if( current.CompareTo( biggest ) == 0 )
					{
						second = current;
						secondPlayer = de.Value as Player;
					}
				}
				else
				{
					biggest = current;
					biggestPlayer = de.Value as Player;
				}
			}

			if( biggest != null && second != null )
			{
				if( biggestPlayer == secondPlayer || biggestPlayer == _game.GetTeamPlayer( secondPlayer ))
				{
					return biggestPlayer;
				}
			}
			else if( biggest != null )
			{
				return biggestPlayer;
			}

			return null;
		}

		private Player FindBiggestSequentialCombinationHolder( )
		{
			CardCombination biggest = null;
			CardCombination second = null;

			Player biggestPlayer = null;
			Player secondPlayer = null;

			foreach( DictionaryEntry de in _mapSequentialCombinationToPlayer )
			{
				CardCombination current = de.Key as CardCombination;

				if( biggest != null )
				{
					if( current.CompareTo( biggest ) > 0 )
					{
						biggest = current;
						biggestPlayer = de.Value as Player;
					}
					else if( current.CompareTo( biggest ) == 0 )
					{
						second = current;
						secondPlayer = de.Value as Player;
					}
				}
				else
				{
					biggest = current;
					biggestPlayer = de.Value as Player;
				}
			}

			if( biggest != null && second != null )
			{
				if( biggestPlayer == secondPlayer || biggestPlayer == _game.GetTeamPlayer( secondPlayer ))
				{
					return biggestPlayer;
				}
			}
			else if( biggest != null )
			{
				return biggestPlayer;
			}

			return null;
		}

		private int GetCombinationPoints( Player winner, ListDictionary map )
		{
			int points = 0;
			foreach( DictionaryEntry de in map )
			{
				if( winner == de.Value || _game.GetTeamPlayer( winner ) == de.Value )
				{
					points += (de.Key as CardCombination).Points;
					(de.Key as CardCombination).IsCounted = true;
				}
				else
				{
					(de.Key as CardCombination).IsCounted = false;
				}
			}
			return points;
		}

		private void AddDoublingPoints( ref int winnersPoints, ref int losersPoints, int winnersExtraPoints, int losersExtraPoints )
		{
			winnersPoints += losersPoints;
		
			if( _currentAnnouncement.IsDoubled )
			{
				winnersPoints *= 2;

				if( _game.ExtraPointsAreDoubled )
				{
					winnersExtraPoints *= 2;
					losersExtraPoints *= 2;
				}
			}
			else if( _currentAnnouncement.IsReDoubled )
			{
				winnersPoints *= 4;

				if( _game.ExtraPointsAreDoubled )
				{
					winnersExtraPoints *= 4;
					losersExtraPoints *= 4;
				}
			}

			winnersPoints += winnersExtraPoints + losersExtraPoints;
			losersPoints = 0;
		}

		private void CountDealPoints()
		{
			int northSouthExtraPoints = 0;
			int eastWestExtraPoints = 0;

			_isCapot = ( _northSouthPoints == 0 || _eastWestPoints == 0 );

			if( _currentAnnouncement.Type == AnnouncementTypeEnum.NoTrumps )
			{
				_northSouthPoints *= 2;
				_eastWestPoints *= 2;
			}

			#region Add combination points

			Player equalHolder = FindBiggestEqualCombinationHolder();
			Player sequentialHolder = FindBiggestSequentialCombinationHolder();

			if( equalHolder != null )
			{
				PlayerPosition posEq = equalHolder.Position;
				if( posEq == PlayerPosition.East || posEq == PlayerPosition.West )
				{
					eastWestExtraPoints += GetCombinationPoints( equalHolder, _mapEqualCombinationToPlayer );
				}
				else
				{
					northSouthExtraPoints += GetCombinationPoints( equalHolder, _mapEqualCombinationToPlayer );
				}
			}

			if( sequentialHolder != null )
			{
				PlayerPosition posSeq = sequentialHolder.Position;
				if( posSeq == PlayerPosition.East || posSeq == PlayerPosition.West )
				{
					eastWestExtraPoints += GetCombinationPoints( sequentialHolder, _mapSequentialCombinationToPlayer );
				}
				else
				{
					northSouthExtraPoints += GetCombinationPoints( sequentialHolder, _mapSequentialCombinationToPlayer );
				}
			}

			foreach( DictionaryEntry de in _mapBelotCombinationToPlayer )
			{
				PlayerPosition pos = ( de.Value as Player ).Position;
				if( pos == PlayerPosition.North || pos == PlayerPosition.South )
				{
					northSouthExtraPoints += (de.Key as BelotCombination).Points;
				}
				else
				{
					eastWestExtraPoints += (de.Key as BelotCombination).Points;
				}
			}

			#endregion

			_rawNorthSouthPoints = _northSouthPoints + northSouthExtraPoints;
			_rawEastWestPoints = _eastWestPoints + eastWestExtraPoints;

			PlayerPosition posBid = _bidWinner.Position;
			if( (_northSouthPoints + northSouthExtraPoints) > (_eastWestPoints + eastWestExtraPoints) )
			{
				if( _isCapot )
				{
					northSouthExtraPoints += 90;
				}

				if( ( !_game.CapotRemovesDouble || !_isCapot ) && ( _currentAnnouncement.IsDoubled || _currentAnnouncement.IsReDoubled ) )
				{
					AddDoublingPoints( ref _northSouthPoints, ref _eastWestPoints, northSouthExtraPoints, eastWestExtraPoints );
				}
				else
				{
					if( posBid == PlayerPosition.East || posBid == PlayerPosition.West )
					{
						// bad game for east-west. All points go to north-south
						_northSouthPoints += _eastWestPoints + northSouthExtraPoints + eastWestExtraPoints;
						_eastWestPoints = 0;
					}
					else
					{	
						//normal game
						_northSouthPoints += northSouthExtraPoints;
						_eastWestPoints += eastWestExtraPoints;
					}
				}
			}
			else if( (_northSouthPoints + northSouthExtraPoints) < (_eastWestPoints + eastWestExtraPoints) )
			{
				if( _isCapot )
				{
					eastWestExtraPoints += 90;
				}

				if( ( !_game.CapotRemovesDouble || !_isCapot ) && ( _currentAnnouncement.IsDoubled || _currentAnnouncement.IsReDoubled ) )
				{
					AddDoublingPoints( ref _eastWestPoints, ref _northSouthPoints, eastWestExtraPoints, northSouthExtraPoints );
				}
				else
				{
					if( posBid == PlayerPosition.North || posBid == PlayerPosition.South )
					{
						// bad game for north-south. All points go to east-west
						_eastWestPoints += _northSouthPoints + northSouthExtraPoints + eastWestExtraPoints;
						_northSouthPoints = 0;
					}
					else
					{	
						//normal game
						_northSouthPoints += northSouthExtraPoints;
						_eastWestPoints += eastWestExtraPoints;
					}
				}
			}
			else
			{
				//Hanging game
				if( posBid == PlayerPosition.North || posBid == PlayerPosition.South )
				{
					_northSouthPoints = 0;
					_hangingPoints = RoundPoints( _eastWestPoints );
				}
				else
				{	
					_eastWestPoints = 0;
					_hangingPoints = RoundPoints( _northSouthPoints );
				}
			}
		}
	
		private void AddBelotCombination( Player player, BelotCombination combination )
		{
			if( player.AnnounceCombination( combination ) )
			{
				_mapBelotCombinationToPlayer.Add( combination, player );
			}
		}
		private void PlayerHasTurn( Player player )
		{
			#region Announce combinations on first hand

			if( _playingManager.IsFirstHand && _currentAnnouncement.Type != AnnouncementTypeEnum.NoTrumps )
			{
				CombinationFinder finder = new CombinationFinder( player.Cards );
				foreach( CardCombination combination in finder.Combinations )
				{ 
					bool isValidCombination = false;

					if( combination is SequentialCombination )
					{
						Player biggest = FindBiggestSequentialCombinationHolder();
						CardCombination biggestCombination = null;

						foreach( DictionaryEntry de in _mapSequentialCombinationToPlayer )
						{
							if( biggest == de.Value as Player )
							{
								biggestCombination = de.Key as CardCombination;
								break;
							}
						}

						if( (biggest == null) || 
							(combination.Points >= biggestCombination.Points) ||
							(player.Position == biggest.Position || player.Position == _game.GetTeamPlayer( biggest ).Position) )
						{
							isValidCombination = true;
						}
					}
					else
					{
						isValidCombination = true;
					}

					if( isValidCombination && player.AnnounceCombination( combination ) )
					{
						if( combination is SequentialCombination )
						{
							_mapSequentialCombinationToPlayer.Add( combination, player );
						}
						if( combination is FourEqualsCombination )
						{
							_mapEqualCombinationToPlayer.Add( combination, player );
						}
					}
				}
			}

			#endregion

			foreach( Card card in player.Cards )
			{
				card.IsSelectable = _playingManager.IsValid( player, card );
			}

			player.PlayCard( _playingManager );
		}

		private void NextHand()
		{
			if( !_isPaused )
			{
				if( !_playingManager.AreCardsFinished )
				{
					PlayerHasTurn( _playingManager.GetLastHand().Winner );
				}
				else
				{
					AddLastTenPoints( _playingManager.GetLastHand().Winner );

					CountDealPoints();

					_playingManager.BelotFound -= new Belot.PlayingManager.BelotFoundHandler( AddBelotCombination );
					_game.RaiseDealCompleted();
				}
			}
		}

		#endregion

		#region Internal Methods
		internal void DealFirstCards()
		{
			DealCards( 20 );
		}

		internal void DealRestCards()
		{
			DealCards( 12 );
		}		

		internal Player EnterBiddingState()
		{			
			AnnouncementManager announcementManager = new AnnouncementManager( );			

			Player current = this._firstPlayer;
			Announcement announce;

			while( !announcementManager.IsBiddingFinished	)
			{				
				announce = current.MakeAnnouncement( announcementManager );								
														
				announcementManager.Add( current, announce );								

				_currentAnnouncement = announcementManager.GetLastValidAnnouncement();

				current = _game.GetNextPlayer( current );
			}

			_bidWinner = announcementManager.GetLastBidder();

			return announcementManager.GetLastBidder();
		}

		internal void EnterPlayingState()
		{
			_playingManager = new PlayingManager( this.CurrentAnnouncement, _game, _allCards );
			_playingManager.BelotFound += new Belot.PlayingManager.BelotFoundHandler( AddBelotCombination );

			PlayerHasTurn( this._firstPlayer );
		}

		internal void PlayerPlayedCard( Player player, Card playedCard )
		{		
			_playingManager.Add( player, playedCard );

			player.Cards.Remove( playedCard );

			foreach( Card card in player.Cards )
			{
				card.IsSelectable = false;
			}

			if( _playingManager.IsHandClosed )
			{
				Hand lastHand = _playingManager.GetLastHand();
				CalculatePoints( lastHand );
				_game.HandIsClosed( lastHand );
				NextHand();
			}
			else
			{
				PlayerHasTurn( _game.GetNextPlayer( player ) );
			}
		}
		
		internal void AddNorthSouthHangingPoints( int points )
		{
			_northSouthPoints += points * 10;
		}

		internal void AddEastWesthHangingPoints( int points )
		{
			_eastWestPoints += points * 10;
		}

		#endregion

	}
}
