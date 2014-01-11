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
	/// Summary description for SequentialCombination.
	/// </summary>
	public class SequentialCombination : CardCombination
	{
		/// <summary>
		/// Creates a sequential combination of cards
		/// </summary>
		/// <param name="cards">cards that the combination consists of</param>
		/// <param name="points">point evaluation of the combination</param>
		public SequentialCombination( CardsCollection cards, int points ) : base( cards, points )
		{
		}

		/// <summary>
		/// Compares current combination to another one
		/// </summary>
		/// <param name="combination">combination to compare to</param>
		/// <returns>1 if current combination is bigger, -1 if second combination is bigger, 0 if both combination are equal</returns>
		public override int CompareTo( object combination )
		{
			if( !(combination is SequentialCombination) )
			{
				throw new InvalidOperationException( "Cannot compare SequentialCombination to an object of different type" );
			}

			int result = 0;

			SequentialCombination comb = combination as SequentialCombination;
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
				// both combinations are sequential. See biggest card
				CardComparer comparer = new CardComparer( );
				this.Cards.Sort( comparer );
				comb.Cards.Sort( comparer );

				result = comparer.Compare( this.Cards[0], comb.Cards[0] );
			}
			return result;
		}
	}
}
