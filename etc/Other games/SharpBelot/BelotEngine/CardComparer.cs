/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections;

namespace Belot
{
	/// <summary>
	/// Compares two cards according to a given announcement.
	/// </summary>
	/// <remarks>
	/// This is needed for sorting purposes. For example Jack is bigger than Ace in game of All Trumps but in game of No Trumps is smaller.
	/// </remarks>
	public class CardComparer : IComparer 
	{
		AnnouncementTypeEnum _announcementType;
		bool _isComparingCombinations = false;

		/// <summary>
		/// Constructor for the class. Creates a new CardComparer object.
		/// Compares in such order 7,8,9,10,J,Q,K,A
		/// </summary>
		internal CardComparer(  )
		{
			this._announcementType = AnnouncementTypeEnum.Pass;
			_isComparingCombinations = true;
		}

		/// <summary>
		/// Constructor for the class. Creates a new CardComparer object.
		/// </summary>
		/// <param name="announcementType"></param>
		public CardComparer( AnnouncementTypeEnum announcementType )
		{
			this._announcementType = announcementType;
			_isComparingCombinations = false;
		}

		/// <summary>
		/// Compares two cards
		/// </summary>
		/// <param name="x">First card</param>
		/// <param name="y">Second card</param>
		/// <returns>1 if first card is bigger, -1 if second card is bigger, 0 if both cards are equal</returns>
		public int Compare( object x, object y )
		{
			if( (x is Card) && (y is Card) ) 
			{
				Card cardX = (Card)x;
				Card cardY = (Card)y;

				if( _isComparingCombinations )
				{
					return CompareCombinations( cardX, cardY );
				}
				else if( (int)(cardX.CardColor) < (int)(cardY.CardColor) )
					return 1;
				else if( (int)(cardX.CardColor) > (int)(cardY.CardColor) )
					return -1;
				else 
				{
					switch( this._announcementType )
					{
						case AnnouncementTypeEnum.AllTrumps :
							return CompareTrumps( cardX, cardY );
						case AnnouncementTypeEnum.NoTrumps :
							return CompareNoTrumps( cardX, cardY );
						case AnnouncementTypeEnum.Spades :
							if( cardX.CardColor == CardColor.Spades )
								return CompareTrumps( cardX, cardY );
							else
								return CompareNoTrumps( cardX, cardY );
						case AnnouncementTypeEnum.Hearts :
							if( cardX.CardColor == CardColor.Hearts )
								return CompareTrumps( cardX, cardY );
							else
								return CompareNoTrumps( cardX, cardY );
						case AnnouncementTypeEnum.Diamonds :
							if( cardX.CardColor == CardColor.Diamonds )
								return CompareTrumps( cardX, cardY );
							else
								return CompareNoTrumps( cardX, cardY );
						case AnnouncementTypeEnum.Clubs :
							if( cardX.CardColor == CardColor.Clubs )
								return CompareTrumps( cardX, cardY );
							else
								return CompareNoTrumps( cardX, cardY );
						case AnnouncementTypeEnum.Pass :
							return CompareTrumps( cardX, cardY );
					}
				}					
			}
			else
			{
				throw new ArgumentException("One of the objects is not a Card");
			}

			return 0;
		}

		/// <summary>
		/// Compares in such order 7,8,Q,K,10,A,9,J
		/// </summary>
		private int CompareTrumps( Card cardX, Card cardY )
		{
			int x = 0, y = 0;
			
			switch( cardX.CardType )
			{
				case CardType.Jack: 
					x = 8;
					break;
				case CardType.Nine: 
					x = 7;
					break;
				case CardType.Ace: 
					x = 6;
					break;
				case CardType.Ten: 
					x = 5;
					break;
				case CardType.King: 
					x = 4;
					break;
				case CardType.Queen: 
					x = 3;
					break;
				case CardType.Eight: 
					x = 2;
					break;
				case CardType.Seven: 
					x = 1;
					break;			
			}

			switch( cardY.CardType )
			{
				case CardType.Jack: 
					y = 8;
					break;
				case CardType.Nine: 
					y = 7;
					break;
				case CardType.Ace: 
					y = 6;
					break;
				case CardType.Ten: 
					y = 5;
					break;
				case CardType.King: 
					y = 4;
					break;
				case CardType.Queen: 
					y = 3;
					break;
				case CardType.Eight: 
					y = 2;
					break;
				case CardType.Seven: 
					y = 1;
					break;			
			}
			
			if( x > y )
				return 1;
			else if( x < y )
				return -1;
			else 
				return 0;
		}

		/// <summary>
		/// Compares in such order 7,8,9,J,Q,K,10,A
		/// </summary>
		private int CompareNoTrumps( Card cardX, Card cardY )
		{
			int x = 0, y = 0;
			
			switch( cardX.CardType )
			{
				case CardType.Ace: 
					x = 8;
					break;
				case CardType.Ten: 
					x = 7;
					break;
				case CardType.King: 
					x = 6;
					break;
				case CardType.Queen: 
					x = 5;
					break;
				case CardType.Jack: 
					x = 4;
					break;
				case CardType.Nine: 
					x = 3;
					break;
				case CardType.Eight: 
					x = 2;
					break;
				case CardType.Seven: 
					x = 1;
					break;			
			}

			switch( cardY.CardType )
			{
				case CardType.Ace: 
					y = 8;
					break;
				case CardType.Ten: 
					y = 7;
					break;
				case CardType.King: 
					y = 6;
					break;
				case CardType.Queen: 
					y = 5;
					break;
				case CardType.Jack: 
					y = 4;
					break;
				case CardType.Nine: 
					y = 3;
					break;
				case CardType.Eight: 
					y = 2;
					break;
				case CardType.Seven: 
					y = 1;
					break;			
			}

			if( x > y )
				return 1;
			else if( x < y )
				return -1;
			else 
				return 0;
		}

		/// <summary>
		/// Compares in such order 7,8,9,10,J,Q,K,A
		/// </summary>
		private int CompareCombinations( Card cardX, Card cardY )
		{
			int x = 0, y = 0;
			
			switch( cardX.CardType )
			{
				case CardType.Ace: 
					x = 8;
					break;
				case CardType.King: 
					x = 7;
					break;
				case CardType.Queen: 
					x = 6;
					break;
				case CardType.Jack: 
					x = 5;
					break;
				case CardType.Ten: 
					x = 4;
					break;
				case CardType.Nine: 
					x = 3;
					break;
				case CardType.Eight: 
					x = 2;
					break;
				case CardType.Seven: 
					x = 1;
					break;			
			}

			switch( cardY.CardType )
			{
				case CardType.Ace: 
					y = 8;
					break;
				case CardType.King: 
					y = 7;
					break;
				case CardType.Queen: 
					y = 6;
					break;
				case CardType.Jack: 
					y = 5;
					break;
				case CardType.Ten: 
					y = 4;
					break;
				case CardType.Nine: 
					y = 3;
					break;
				case CardType.Eight: 
					y = 2;
					break;
				case CardType.Seven: 
					y = 1;
					break;			
			}

			if( x > y )
				return 1;
			else if( x < y )
				return -1;
			else 
				return 0;
		}
	}
}
