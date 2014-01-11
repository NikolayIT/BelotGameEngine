/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;

namespace Belot
{
	/// <summary>
	/// A manager that observes playing rules.
	/// </summary>
	public class PlayingManager
	{
		#region Fields

		private IList< Hand > _playedHands;
		private IDictionary< Card, Player > _cardToPlayerMap;
		private Announcement _currentAnnouncement;
		private Hand _currentHand;
		private bool _isHandClosed;
		private CardComparer _cardComparer;
		private BelotGame _game;
		private CardsCollection _playedCards;
		private CardsCollection _remainingCards;
	
		#endregion

		#region Constructor
		/// <summary>
		/// Constructor for the class
		/// </summary>
		internal PlayingManager( Announcement current, BelotGame game, CardsCollection _allCards )
		{		
			_game = game;
			_playedHands = new List< Hand >();
			_cardToPlayerMap = new Dictionary<Card, Player>();
			_currentAnnouncement = current;			
			_currentHand = new Hand();
			_isHandClosed = false;
			_cardComparer = new CardComparer( current.Type );

			_playedCards = new CardsCollection();
			_remainingCards = new CardsCollection();

			foreach( Card card in _allCards )
			{
				_remainingCards.Add( card );
			}
		}
		#endregion

		#region Internal Properties
		/// <summary>
		/// Gets a value whether deal is finished ( all cards are played )
		/// </summary>
		internal bool AreCardsFinished
		{
			get
			{ 
				return (_playedHands.Count >= 8);
			}
		}
		
		/// <summary>
		/// Gets whether a hand is closed (each player's turn passed)
		/// </summary>
		internal bool IsHandClosed
		{
			get
			{ 
				return _isHandClosed;
			}
		}

		/// <summary>
		/// Gets whether current hand is first in deal
		/// </summary>
		internal bool IsFirstHand
		{
			get
			{ 
				return _playedHands.Count == 0;
			}
		}

		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the current announcement in the deal
		/// </summary>
		public Announcement CurrentAnnouncement
		{
			get
			{
				return _currentAnnouncement;
			}
		}

		/// <summary>
		/// Gets the current hand in the deal
		/// </summary>
		public Hand CurrentHand
		{
			get
			{
				return _currentHand;
			}
		}
		
		/// <summary>
		/// Gets all cards that already have passed in this deal
		/// </summary>
		public CardsCollection PlayedCards
		{
			get
			{
				return _playedCards;
			}
		}

		/// <summary>
		/// Gets all cards that have not passed in this deal
		/// </summary>
		public CardsCollection RemainingCards
		{
			get
			{
				return _remainingCards;
			}
		}
		#endregion

		#region Internal Methods
		internal void Add( Player player, Card card )  
		{			
			_cardToPlayerMap.Add( card, player );
			_currentHand.Add( card );

			_playedCards.Add( card );
			_remainingCards.Remove( card );
				
			CheckForBelotCombination ( player, card );

			if( _currentHand.Count < 4 ) // current hand is open
			{
				_isHandClosed = false;
			}
			else // current hand is closed
			{
				_currentHand.SetWinner( DetermineCurrentHandWinner() );
				_playedHands.Add( _currentHand );
				_currentHand = new Hand();
				_isHandClosed = true;
			}
		}

		internal Hand GetLastHand()
		{
			return _playedHands[_playedHands.Count-1] as Hand;
		}

		#endregion

		#region Public Methods
		
		/// <summary>
		/// If card played by this player is valid according to game rules
		/// </summary>
		public bool IsValid( Player player, Card card )
		{			
			if( player == null)
				throw new ArgumentNullException( "Player", "Player cannot be null");
				
			if(	card == null)
				throw new ArgumentNullException( "Card", "Played card cannot be null");
			
			CardColor wantedColor;
			Card biggestCard;

			// if player plays first card in hand he can play any card
			if( _currentHand.IsEmpty )
			{
				return true;
			}
			else
			{
				wantedColor = GetPlayingColor( _currentHand );
				biggestCard = GetBiggestCard( _currentHand );
			}			
			

			if( _currentAnnouncement.Type == AnnouncementTypeEnum.AllTrumps )
			{
				#region Game of all trumps
				if( card.CardColor == wantedColor )
				{
					if( IsBigger( card, biggestCard ) )				
					{
						// card is biggest for now in wanted color
						return true;
					}
					else 
					{
						// card is not biggest but player has no bigger one
						return !HasBigger( biggestCard, player.Cards, wantedColor );
					}
				}
				else if( !HasPlayingColor( wantedColor, player.Cards ) )
				{
					// if player does not have currently wanted color he can play any card
					return true;
				}
				#endregion
			}
			else if( _currentAnnouncement.Type == AnnouncementTypeEnum.NoTrumps )
			{
				#region Game of no trumps
				if( card.CardColor == wantedColor )
				{
					//player may play any card in wanted color
					return true;
				}
				else
				{
					// player does not have currently wanted color he can play any card
					return ( !HasPlayingColor( wantedColor, player.Cards ) );
				}
				#endregion
			}
			else
			{
				#region Game of color trumps
				CardColor trumpColor = GetTrumpColor( );

				if( card.CardColor == wantedColor )
				{				
					if( trumpColor == wantedColor )
					{
						if( IsBigger( card, biggestCard ) )				
						{
							// card is biggest for now
							return true;
						}
						else 
						{
							// card is not biggest but player has no bigger one
							return !HasBigger( biggestCard, player.Cards, wantedColor );
						}
					}
					else
					{
						// card is not trump and may not be bigger
						return true;
					}			
				}
				else
				{
					if( HasPlayingColor( wantedColor, player.Cards ) )
					{
						// player has cards in wanted color but plays from different one
						return false;						
					}
					else
					{
						if( trumpColor == wantedColor )
						{
							// if player does not have currently wanted color or trump color he can play any card
							return true;	
						}
						else
						{
							biggestCard = GetBiggestCard( _currentHand, trumpColor );

							Player biggestCardOwner = _cardToPlayerMap[biggestCard] as Player;

							if( biggestCardOwner == _game.GetTeamPlayer( player ) )
							{
								//if hand is held by partner player may not give trump
								return true;
							}
							else
							{
								// player must give bigger trump
								if( !HasPlayingColor( trumpColor, player.Cards ) )				
								{
									// player does not have trumps
									return true;
								}
								else 
								{
									if ( biggestCard.CardColor != trumpColor )
									{
										// biggest card for now is not a trump 
										// so player must play a trump
										return ( card.CardColor == trumpColor );
									}
									else
									{
										if ( IsBigger( card, biggestCard ) )
										{
											// card is biggest for now
											return true;
										}
										else
										{
											// card is not biggest but player has no bigger one
											return !HasBigger( biggestCard, player.Cards, trumpColor );
										}
									}									
								}
							}							
						}
					}
				}
				#endregion
			}

			return false;
		}		

		/// <summary>
		/// Gets the current biggest card in hand. Null if hand is empty
		/// </summary>
		public Card GetBiggestCard()
		{
			Card biggest = null;

			if( !_currentHand.IsEmpty )
			{
				if( _currentAnnouncement.Type == AnnouncementTypeEnum.AllTrumps || _currentAnnouncement.Type == AnnouncementTypeEnum.NoTrumps )
				{
					biggest = GetBiggestCard( _currentHand );
				}
				else
				{
					biggest = GetBiggestCard( _currentHand, GetTrumpColor() );
				}
			}
			
			return biggest;
		}

		/// <summary>
		/// Gets the player who played a given card
		/// </summary>
		/// <param name="card">the card to check for</param>
		/// <returns>the player who played the given card or null if card has not passed</returns>
		public Player GetPlayerWhoPlayedCard( Card card )
		{
			if( card == null )
			{

			}
			return _cardToPlayerMap[card];
		}

		/// <summary>
		/// Gets the partner of the a player 
		/// </summary>
		public Player GetPartner( Player askingPlayer )
		{
			return _game.GetTeamPlayer(askingPlayer);
		}
		
		#endregion

		#region Events
		/// <summary>
		/// Handler delegate for the BelotFound event.
		/// </summary>
		internal delegate void BelotFoundHandler ( Player player, BelotCombination combination );
			
		/// <summary>
		/// Occurs when a belot combination is found.
		/// </summary>
		internal event BelotFoundHandler BelotFound;

		#endregion

		#region Private Methods
		private Player DetermineCurrentHandWinner()
		{
			Card biggest;
			if( _currentAnnouncement.Type == AnnouncementTypeEnum.AllTrumps || _currentAnnouncement.Type == AnnouncementTypeEnum.NoTrumps )
			{
				biggest = GetBiggestCard( _currentHand );
			}
			else
			{
				biggest = GetBiggestCard( _currentHand, GetTrumpColor() );
			}		
			
			return _cardToPlayerMap[ biggest ];
		}		

		private bool HasPlayingColor( CardColor color, CardsCollection cards )
		{
			foreach( Card card in cards )
			{
				if( card.CardColor == color )
					return true;
			}
			return false;
		}

		private bool IsBigger( Card card, Card biggestCard )
		{
			return _cardComparer.Compare( card, biggestCard ) > 0;
		}

		private bool HasBigger( Card biggestCard, CardsCollection cards, CardColor wantedColor )
		{
			bool hasBigger = false;
			foreach( Card card in cards )
			{
				if( card.CardColor == wantedColor )
				{
					if( _cardComparer.Compare( card, biggestCard ) > 0 )
					{
						hasBigger = true;
						break;
					}
				}
			}

			return hasBigger;
		}	

		private Card GetBiggestCard( Hand currentHand )
		{
			CardColor wantedColor = GetPlayingColor( currentHand );
			Card biggest = currentHand[0];
			foreach( Card card in currentHand )
			{
				if( card.CardColor == wantedColor )
				{
					if( _cardComparer.Compare( card, biggest ) > 0 )
					{
						biggest = card;
					}
				}
			}
			return biggest;
		}

		private Card GetBiggestCard( Hand currentHand, CardColor trumpColor )
		{
			CardColor wantedColor = GetPlayingColor( currentHand );
			Card biggest = currentHand[0];
			bool trumped = false;

			foreach( Card card in currentHand )
			{
				if( !trumped )
				{
					if( card.CardColor == trumpColor )
					{
						trumped = true;
						if( biggest.CardColor == trumpColor )
						{
							if( _cardComparer.Compare( card, biggest ) > 0 )
							{
								biggest = card;
							}
						}
						else
						{
							biggest = card;
						}
					}
					else if( card.CardColor == wantedColor )
					{
						if( _cardComparer.Compare( card, biggest ) > 0 )
						{
							biggest = card;							
						}
					}
				}
				else
				{
					if( card.CardColor == trumpColor )
					{
						if( _cardComparer.Compare( card, biggest ) > 0 )
						{
							biggest = card;
						}
					}
				}
			}
			return biggest;
		}

		private CardColor GetPlayingColor( Hand currentHand )
		{			
			return currentHand[0].CardColor;
		}

		private CardColor GetTrumpColor( )
		{
			CardColor trump = 0;

			switch( _currentAnnouncement.Type )
			{
				case AnnouncementTypeEnum.Spades:
					trump = CardColor.Spades;
					break;
				case AnnouncementTypeEnum.Hearts:
					trump = CardColor.Hearts;
					break;
				case AnnouncementTypeEnum.Diamonds:
					trump = CardColor.Diamonds;
					break;
				case AnnouncementTypeEnum.Clubs:
					trump = CardColor.Clubs;					
					break;
			}
			return trump;
		}

		private void CheckForBelotCombination( Player player, Card playedCard ) 
		{
			CardsCollection foundCards = new CardsCollection();
			CardColor trumpColor = GetTrumpColor( );

			if( _currentAnnouncement.Type == AnnouncementTypeEnum.AllTrumps )
			{
				if( _currentHand.IsEmpty )
				{
					CheckBelotForValidColor( player, playedCard, foundCards );
				}
				else
				{
					if( playedCard.CardColor == GetPlayingColor( _currentHand ) )
					{
						CheckBelotForValidColor( player, playedCard, foundCards );
					}
				}
			}
			else if( _currentAnnouncement.Type != AnnouncementTypeEnum.NoTrumps && playedCard.CardColor == trumpColor )
			{
				CheckBelotForValidColor( player, playedCard, foundCards );
			}

			if( foundCards.Count != 0 )
			{
				BelotFound( player, new BelotCombination( foundCards, 20) );
			}
		}

		private void CheckBelotForValidColor( Player player, Card playedCard, CardsCollection foundCards )
		{
			foreach( Card card in player.Cards )
			{
				if( playedCard.CardColor == card.CardColor )
				{
					if( playedCard.CardType == CardType.Queen && card.CardType == CardType.King )
					{
						foundCards.Add( playedCard );
						foundCards.Add( card );
					}

					if(playedCard.CardType == CardType.King && card.CardType == CardType.Queen)
					{
						foundCards.Add( card );
						foundCards.Add( playedCard );
					}
				}
			}

		}
		#endregion

	}
}
