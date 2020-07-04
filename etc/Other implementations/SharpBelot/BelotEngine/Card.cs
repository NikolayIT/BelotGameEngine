/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

namespace Belot
{
	/// <summary>
	/// Summary description for Card.
	/// </summary>
	public class Card
	{
		#region Fields
		private CardType _cardType;
		private CardColor _cardColor;
		private bool _isSelectable = false;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the type of the current card
		/// </summary>
		public CardType CardType
		{
			get { return _cardType; } 
		}

		/// <summary>
		/// Gets the color of the current card
		/// </summary>
		public CardColor CardColor
		{
			get { return _cardColor; } 
		}

		/// <summary>
		/// Gets the if card is valid to be selected by player
		/// </summary>
		public bool IsSelectable
		{
			get
			{
				return _isSelectable;
			}
			internal set
			{
				_isSelectable = value;
			}
		}

		#endregion

		#region Constructors
		private Card()
		{			
		}

		/// <summary>
		/// Constructor for the class. Instantiates a new card object
		/// </summary>
		/// <param name="type">The type of the card</param>
		/// <param name="color">The color of the card</param>
		internal Card( CardType type, CardColor color)
		{
			this._cardType = type;
			this._cardColor = color;
		}
		#endregion

		#region Overriden Methods
		/// <summary>
		/// Gets a hash code for a card
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int hash = 0;
			switch( this.CardType )
			{
				case CardType.Ace:
					hash = 8;
					break;
				case CardType.King:
					hash = 7;
					break;
				case CardType.Queen:
					hash = 6;
					break;
				case CardType.Jack:
					hash = 5;
					break;
				case CardType.Ten:
					hash = 4;
					break;
				case CardType.Nine:
					hash = 3;
					break;
				case CardType.Eight:
					hash = 2;
					break;
				case CardType.Seven:
					hash = 1;
					break;
			}

			switch( this.CardColor )
			{
				case CardColor.Spades:
					hash += 4*8;
					break;
				case CardColor.Hearts:
					hash += 3*8;
					break;
				case CardColor.Diamonds:
					hash += 2*8;
					break;
				case CardColor.Clubs:
					hash += 8;
					break;				
			}
			return hash;
		}

		/// <summary>
		/// Gets a string presentation of a card object
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string type = "";
			char color = '\n';

			switch( this.CardType )
			{
				case CardType.Ace:
					type = "A";
					break;
				case CardType.King:
					type = "K";
					break;
				case CardType.Queen:
					type = "Q";
					break;
				case CardType.Jack:
					type = "J";
					break;
				case CardType.Ten:
					type = "10";
					break;
				case CardType.Nine:
					type = "9";
					break;
				case CardType.Eight:
					type = "8";
					break;
				case CardType.Seven:
					type = "7";
					break;
			}

			switch( this.CardColor )
			{
				case CardColor.Spades:
					color = '\u2660';
					break;
				case CardColor.Hearts:
					color = '\u2665';
					break;
				case CardColor.Diamonds:
					color = '\u2666';
					break;
				case CardColor.Clubs:
					color = '\u2663';
					break;				
			}
		
			return type + " " + color; 
		}
		#endregion		
	}
}
