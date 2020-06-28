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
	/// Summary description for FourEqualsCombination.
	/// </summary>
	public class FourEqualsCombination : CardCombination
	{
		/// <summary>
		/// Creates an equal combination of cards
		/// </summary>
		/// <param name="cards">cards that the combination consists of</param>
		/// <param name="points">point evaluation of the combination</param>
		public FourEqualsCombination( CardsCollection cards, int points ) : base( cards, points )
		{
		}

		/// <summary>
		/// Compares current combination to another one
		/// </summary>
		/// <param name="combination">combination to compare to</param>
		/// <returns>1 if current combination is bigger, -1 if second combination is bigger, 0 if both combination are equal</returns>
		public override int CompareTo( object combination )
		{
			if( !(combination is FourEqualsCombination) )
			{
				throw new InvalidOperationException( "Cannot compare FourEqualsCombination to an object of different type" );
			}

			int result = 0;

			FourEqualsCombination comb = combination as FourEqualsCombination;
			if( this.Points > comb.Points )
			{
				result = 1;
			}
			else if( this.Points < comb.Points )
			{
				result = -1;
			}
			else
			{
				// non-sequential combinations with equal points can be Q, K, 10, A
				int x = 0, y = 0;
		
				switch( this.Cards[0].CardType )
				{
					case CardType.Ace: 
						x = 4;
						break;
					case CardType.Ten: 
						x = 3;
						break;
					case CardType.King: 
						x = 2;
						break;
					case CardType.Queen: 
						x = 1;
						break;		
				}

				switch( comb.Cards[0].CardType )
				{
					case CardType.Ace: 
						y = 4;
						break;
					case CardType.Ten: 
						y = 3;
						break;
					case CardType.King: 
						y = 2;
						break;
					case CardType.Queen: 
						y = 1;
						break;	
				}
		
				if( x > y )
					result = 1;
				else if( x < y )
					result = -1;
			}
			return result;
		}
	}
}
