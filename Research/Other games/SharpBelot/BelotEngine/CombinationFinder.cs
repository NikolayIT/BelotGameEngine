/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System.Collections.Generic;

namespace Belot
{
	/// <summary>
	/// Summary description for CombinationFinder.
	/// </summary>
	public class CombinationFinder
	{
		private IList< CardCombination > _combinations;
		private CardsCollection _cards;

		/// <summary>
		/// Constructor of the class
		/// </summary>
		public CombinationFinder( CardsCollection cards )
		{
			_combinations = new List< CardCombination >();
			_cards = cards;
			FindEquals();

			if( _combinations.Count == 0 )
			{
				FindSequential();
			}
		}

		/// <summary>
		/// Gets the combinations which are in players cards
		/// </summary>
		public IList< CardCombination > Combinations
		{
			get
			{
				return _combinations;
			}
		}

		private void FindEquals()
		{

			#region Jacks

			CardsCollection foundJacks = new CardsCollection();
			foreach( Card card in _cards )
			{
				if( card.CardType == CardType.Jack )
				{
					foundJacks.Add( card );
				}
			}

			if( foundJacks.Count == 4 )
			{
				_combinations.Add( new FourEqualsCombination( foundJacks, 200 ) );
			}

			#endregion

			#region Nines

			CardsCollection foundNines = new CardsCollection();
			foreach( Card card in _cards )
			{
				if( card.CardType == CardType.Nine )
				{
					foundNines.Add( card );
				}
			}

			if( foundNines.Count == 4 )
			{
				_combinations.Add( new FourEqualsCombination( foundNines, 150 ) );
			}

			#endregion

			#region Aces

			CardsCollection foundAces = new CardsCollection();
			foreach( Card card in _cards )
			{
				if( card.CardType == CardType.Ace )
				{
					foundAces.Add( card );
				}
			}

			if( foundAces.Count == 4 )
			{
				_combinations.Add( new FourEqualsCombination( foundAces, 100 ) );
			}

			#endregion

			#region Tens

			CardsCollection foundTens = new CardsCollection();
			foreach( Card card in _cards )
			{
				if( card.CardType == CardType.Ten )
				{
					foundTens.Add( card );
				}
			}

			if( foundTens.Count == 4 )
			{
				_combinations.Add( new FourEqualsCombination( foundTens, 100 ) );
			}

			#endregion

			#region Kings

			CardsCollection foundKings = new CardsCollection();
			foreach( Card card in _cards )
			{
				if( card.CardType == CardType.King )
				{
					foundKings.Add( card );
				}
			}

			if( foundKings.Count == 4 )
			{
				_combinations.Add( new FourEqualsCombination( foundKings, 100 ) );
			}

			#endregion

			#region Queens

			CardsCollection foundQueens = new CardsCollection();
			foreach( Card card in _cards )
			{
				if( card.CardType == CardType.Queen )
				{
					foundQueens.Add( card );
				}
			}

			if( foundQueens.Count == 4 )
			{
				_combinations.Add( new FourEqualsCombination( foundQueens, 100 ) );
			}

			#endregion
		}

		private void FindSequential()
		{
			CardsCollection foundCards = new CardsCollection();

			#region Spades

			foreach( Card card in _cards )
			{
				if( card.CardColor == CardColor.Spades )
				{
					foundCards.Add( card );
				}
			}

			if( foundCards.Count > 2 )
			{
				FindSequentialForColor( foundCards );
			}

			#endregion

			#region Hearts

			foundCards.Clear( );
			foreach( Card card in _cards )
			{
				if( card.CardColor == CardColor.Hearts )
				{
					foundCards.Add( card );
				}
			}

			if( foundCards.Count > 2 )
			{
				FindSequentialForColor( foundCards );
			}

			#endregion

			#region Diamonds

			foundCards.Clear( );
			foreach( Card card in _cards )
			{
				if( card.CardColor == CardColor.Diamonds )
				{
					foundCards.Add( card );
				}
			}

			if( foundCards.Count > 2 )
			{
				FindSequentialForColor( foundCards );
			}

			#endregion

			#region Clubs

			foundCards.Clear( );
			foreach( Card card in _cards )
			{
				if( card.CardColor == CardColor.Clubs )
				{
					foundCards.Add( card );
				}
			}

			if( foundCards.Count > 2 )
			{
				FindSequentialForColor( foundCards );
			}

			#endregion
		}

		private void FindSequentialForColor( CardsCollection cards )
		{
			CardComparer comparer = new CardComparer( );
			cards.Sort( comparer ); // we have cards sorted like A,K,Q,J,10,9,8,7

			CardsCollection foundCards = new CardsCollection();

			for( int i = 0; i < cards.Count-1; i++ )
			{
				if( IsConsequent( cards[i], cards[i+1] ) )
				{
					if( foundCards.IsEmpty )
					{
						foundCards.Add ( cards[i] );
					}	
					
					foundCards.Add ( cards[i+1] );
				}
				else
				{
					if( !foundCards.IsEmpty )
					{
						
						AddSequentialCombination( foundCards );
						foundCards = new CardsCollection();
					}
				}
			}

			if( !foundCards.IsEmpty )
			{
				AddSequentialCombination( foundCards );		
			}

		}
		private void AddSequentialCombination( CardsCollection cards )
		{
			if( cards.Count == 3 )
			{
				this._combinations.Add( new SequentialCombination ( cards, 20 ) );
			}
			if( cards.Count == 4 )
			{
				this._combinations.Add( new SequentialCombination ( cards, 50 ) );
			}
			if( cards.Count >= 5 )
			{
				this._combinations.Add( new SequentialCombination ( cards, 100 ) );
			}
		}

		private bool IsConsequent( Card x, Card y )
		{
			if( x.CardType == CardType.Eight && y.CardType == CardType.Seven )
				return true;

			if( x.CardType == CardType.Nine && y.CardType == CardType.Eight )
				return true;

			if( x.CardType == CardType.Ten && y.CardType == CardType.Nine )
				return true;

			if( x.CardType == CardType.Jack && y.CardType == CardType.Ten )
				return true;

			if( x.CardType == CardType.Queen && y.CardType == CardType.Jack )
				return true;

			if( x.CardType == CardType.King && y.CardType == CardType.Queen )
				return true;

			if( x.CardType == CardType.Ace && y.CardType == CardType.King )
				return true;

			else return false;
		}
	}
}
