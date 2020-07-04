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
	/// Represents a hand of 4 cards.
	/// </summary>
	public class Hand : CardsCollection
	{
		Player _winner;

		internal Hand()
		{
			
		}

		/// <summary>
		/// The player who takes this hand.
		/// </summary>
		public Player Winner
		{
			get
			{
				return _winner;
			}
		}

		internal override int Add( Card value )  
		{
			if( InnerList.Count >= 4 )

				throw new InvalidOperationException("A hand can contain maximum of 4 cards");

			int ret =  InnerList.Add( value );
			RaiseChanged();
			return ret;
		}		

		internal void SetWinner( Player winner )
		{
			_winner = winner;
		}
	}
}
