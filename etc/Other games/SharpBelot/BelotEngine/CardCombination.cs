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
	/// Summary description for CardCombination.
	/// </summary>
	public abstract class CardCombination : IComparable
	{
		private int _points;
		private CardsCollection _cards;
		private bool _isCounted = false;

		/// <summary>
		/// Whether the points of this combination will be counted
		/// </summary>
		public bool IsCounted
		{
			get
			{
				return _isCounted;
			}
			set
			{
				_isCounted = value;
			}
		}

		/// <summary>
		/// Constructor of the class
		/// </summary>
		protected CardCombination( CardsCollection cards, int points )
		{
			_cards = cards;
			_points = points;
		}

		/// <summary>
		/// Gets the points evaluation of current combination 
		/// </summary>
		public int Points
		{
			get
			{
				return _points;
			}
		}

		/// <summary>
		/// Gets the cards that make the current combination
		/// </summary>
		public CardsCollection Cards
		{
			get { return _cards; }
		}

		/// <summary>
		/// Compares current combination to another one
		/// </summary>
		/// <param name="combination">combination to compare to</param>
		/// <returns>1 if current combination is bigger, -1 if second combination is bigger, 0 if both combination are equal</returns>
		public abstract int CompareTo( object combination );
	}
}
