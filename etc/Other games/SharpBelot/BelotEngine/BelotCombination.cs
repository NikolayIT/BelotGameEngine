/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

namespace Belot
{
	/// <summary>
	/// Summary description for BelotCombination.
	/// </summary>
	public class BelotCombination  : CardCombination
	{
		/// <summary>
		/// Creates an belot combination of cards
		/// </summary>
		/// <param name="cards">cards that the combination consists of</param>
		/// <param name="points">point evaluation of the combination</param>
		public BelotCombination( CardsCollection cards, int points ) : base( cards, points )
		{
			this.IsCounted = true;
		}

		/// <summary>
		/// Deprecated
		/// </summary>
		public override int CompareTo( object combination )
		{
			return 0;
		}
	}
}
