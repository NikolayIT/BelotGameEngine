/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;

namespace Belot
{
	/// <summary>
	/// This is a base class for all players. Custom implementations (Human or Computer players) must derive from this class.
	/// </summary>
	public abstract class Player
	{
		#region Fields
		private CardsCollection _cards;
		private string _name = "";
		private PlayerCardsChangedHandler _cardsChanged; 
		private CardPlayedHandler _cardPlayed; 
		private PlayerPosition _position;
		#endregion

		#region Properties
		/// <summary>
		/// Gets a readonly collection of players cards
		/// </summary>
		internal protected CardsCollection Cards
		{
			get
			{ 
				return _cards; 
			}
		}		

		/// <summary>
		/// The name of the player
		/// </summary>
		public string Name
		{
			get
			{ 
				return _name; 
			}
			set
			{ 
				_name = value; 
			}
		}

		/// <summary>
		/// The position of the player
		/// </summary>
		public PlayerPosition Position
		{
			get
			{ 
				return _position; 
			}
		}
		#endregion		

		#region Constructor
		/// <summary>
		/// Constructor for the class.
		/// </summary>
		protected Player( )
		{
			this._cards = new CardsCollection();
			this._cards.Changed += new Belot.CardsCollection.CardsCollectionChangedHandler( RaiseCardsChanged );
		}

		/// <summary>
		/// Constructor for the class. Creates a new player with the specified name.
		/// </summary>
		/// <param name="name">The name of the player</param>
		protected Player( string name ) : this()
		{			
			this.Name = name;
		}
		#endregion

		#region Events

			#region CardsChanged

			/// <summary>
			/// Handler delegate for the CardsChanged event.
			/// </summary>
			public delegate void PlayerCardsChangedHandler ( Player player, CardsCollection cards );
				
			/// <summary>
			/// Occurs when player cards have changed.
			/// </summary>
			public event PlayerCardsChangedHandler CardsChanged
			{
				add
				{
					if(!( value.Target is Player ) )
					{
						_cardsChanged += value;
					}
					else
					{
						throw new InvalidOperationException( "Players cannot wait for cards changed event of other players" );
					}
				}
				remove
				{
					_cardsChanged -= value;
				}
			}
			
			/// <summary>
			/// Raises the CardsChanged event
			/// </summary>
			protected void RaiseCardsChanged( )
			{
				if( _cardsChanged != null )
				{
					_cardsChanged( this, this._cards );
				}			
			}

		#endregion

			#region CardPlaying Event
			/// <summary>
			/// Handler delegate for the CardPlaying event.
			/// </summary>
			public delegate void CardPlayingHandler ( Player player, PlayingManager manager );
					
			/// <summary>
			/// Occurs when the player is to plays a card.
			/// </summary>
			public event CardPlayingHandler CardPlaying;
				
			/// <summary>
			/// Raises the CardPlaying event
			/// </summary>
			protected void RaiseCardPlaying( PlayingManager manager )
			{
				if( CardPlaying != null )
				{
					 CardPlaying( this, manager );
				}
			}
			#endregion

			#region CardPlayed

			/// <summary>
			/// Handler delegate for the CardsChanged event.
			/// </summary>
			public delegate void CardPlayedHandler ( Player player, Card card );
					
			/// <summary>
			/// Occurs when player cards have changed.
			/// </summary>
			public event CardPlayedHandler CardPlayed
			{
				add
				{	
					_cardPlayed += value;
				}
				remove
				{
					_cardPlayed -= value;
				}
			}
				
			/// <summary>
			/// Raises the CardsChanged event
			/// </summary>
			protected void RaiseCardPlayed( Card card )
			{
				if( _cardPlayed != null )
				{
					_cardPlayed( this, card );
				}			
			}

			#endregion

		#endregion

		internal void SetPosition( PlayerPosition position )
		{
			_position = position;
		}

		/// <summary>
		/// The player has to play a card. Player has to ask the manager if desired card is valid. Otherwise returning invalid card throws exception.
		/// </summary>
		/// <param name="manager">Manager that observes playing rules</param>
		internal protected abstract void PlayCard( PlayingManager manager );
		
		/// <summary>
		/// The player makes an announcement (bidding). Player has to ask the manager if desired announcement is valid. Otherwise returning invalid announcement throws exception.
		/// </summary>
		/// <param name="manager">Manager that observes bidding rules</param>
		/// <returns>Announcement made by the player</returns>
		public abstract Announcement MakeAnnouncement( AnnouncementManager manager );

		/// <summary>
		/// The player has a combination in his cards.
		/// </summary>
		/// <param name="combination">The combination of cards tha player has</param>
		/// <returns>true if player wants to announce this combination</returns>
		public abstract bool AnnounceCombination( CardCombination combination );
	
	}
}
