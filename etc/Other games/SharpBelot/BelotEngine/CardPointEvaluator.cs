/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

namespace Belot
{
	/// <summary>
	/// Evaluates points for a card in a specific game .
	/// </summary>
	public class CardPointEvaluator
	{
		/// <summary>
		/// Evaluates points for a card in a game
		/// </summary>
		/// <param name="gameType">curent game</param>
		/// <param name="card">card to be evaluated</param>
		/// <returns></returns>
		public static int EvaluateCard( AnnouncementTypeEnum gameType, Card card )
		{
			int val = 0;
			switch( gameType )
			{
				case AnnouncementTypeEnum.AllTrumps :
					val = EvaluateTrumps( card );
					break;
				case AnnouncementTypeEnum.NoTrumps :
					val =  EvaluateNoTrumps( card );
					break;
				case AnnouncementTypeEnum.Spades :
					if( card.CardColor == CardColor.Spades )
						val =  EvaluateTrumps( card );
					else
						val =  EvaluateNoTrumps( card );
					break;
				case AnnouncementTypeEnum.Hearts :
					if( card.CardColor == CardColor.Hearts )
						val =  EvaluateTrumps( card );
					else
						val =  EvaluateNoTrumps( card );
					break;
				case AnnouncementTypeEnum.Diamonds :
					if( card.CardColor == CardColor.Diamonds )
						val =  EvaluateTrumps( card );
					else
						val =  EvaluateNoTrumps( card );
					break;
				case AnnouncementTypeEnum.Clubs :
					if( card.CardColor == CardColor.Clubs )
						val =  EvaluateTrumps( card );
					else
						val =  EvaluateNoTrumps( card );
					break;
				case AnnouncementTypeEnum.Pass :
					break;
			}
			return val;
		}

		private static int EvaluateTrumps( Card card )
		{
			int val = 0;
			switch( card.CardType )
			{
				case CardType.Jack: 
					val = 20;
					break;
				case CardType.Nine: 
					val = 14;
					break;
				case CardType.Ace: 
					val = 11;
					break;
				case CardType.Ten: 
					val = 10;
					break;
				case CardType.King: 
					val = 4;
					break;
				case CardType.Queen: 
					val = 3;
					break;
				case CardType.Eight: 
					val = 0;
					break;
				case CardType.Seven: 
					val = 0;
					break;			
			}
			return val;
		}

		private static int EvaluateNoTrumps( Card card )
		{
			int val = 0;
			switch( card.CardType )
			{
				case CardType.Ace: 
					val = 11;  
					break;	  
				case CardType.Ten: 
					val = 10;  
					break;	  
				case CardType.King: 
					val = 4;  
					break;	  
				case CardType.Queen: 
					val = 3;  
					break;	  
				case CardType.Jack: 
					val = 2;  
					break;	  
				case CardType.Nine: 
					val = 0;  
					break;	  
				case CardType.Eight: 
					val = 0;  
					break;	  
				case CardType.Seven: 
					val = 0;
					break;			
			}
			return val;
		}
	}
}
