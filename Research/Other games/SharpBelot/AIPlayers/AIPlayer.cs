/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Belot;

namespace AIPlayers
{

	/* Примерен алгоритъм за игра
	 * 
	 * 1. Правила за оценяване на карта
	 * 
	 * 1.1. Разстоянието ( броя на картите по сила - 10 до К = 1, 10 до Q = 2, К до 7 = 3 ) от текущо 
	 * най-силната карта до оценяваната карта трябва да е >= на броя на оставащите карти на играча
	 * в боята. Повече оставащи карти водят до по-голяма положителна оценка. Ако броя на оставащите
	 * карти е < от разстоянието, картата получава оценка 0.
	 * 
	 * 1.2. Точки се прибавят/махат ако оценяваната карта е в цвят обявен от партньора/противника.
	 * 
	 * 1.3. Правило '1.1' е с по-голяма тежест от правило '1.2'.
	 * 
	 * 
	 * 
	 * 
	 * 2. Избор на карта. 
	 * 
	 * 2.1. Ако е първа карта от взятка:
	 * 
	 * 2.1.1 Ако има карта с оценка по правила '1.1' и '1.2' по-голяма от 0
	 * 
	 * 2.1.1.1 Ако картата с най-висока оценка по правила '1.1' и '1.2' (>0) е най-силната карта за момента играем нея.
	 * 
	 * 2.1.1.2 Ако не е най-силната играем най-слабата от нейната боя
	 * 
	 * 2.1.2 Ако всички оценки са <= 0, играем коя да е карта.
	 * 
	 * 2.2. Ако не е първа карта от взятка:
	 * 
	 * 2.2.1. Ако имаме текущо най-силната карта в исканата боя, играем нея. ( ако боята е цакана 
	 * от противника най-силната карта вече се търси от козовата боя )
	 * 
	 * 2.2.2. Ако нямаме текущо най-силната карта в исканата боя:
	 * 
	 * 2.2.2.1. Ако имаме боя, в която всички карти са с 0 точки по правило '1.1':
	 * 
	 * 2.2.2.1.1. Ако партньорът е играл най-силната карта даваме карта, която има 0 точки по правило '1.1'
	 * и има най-много игрови точки.
	 * 
	 * 2.2.2.1.2. Ако партньорът не е играл най-силната карта даваме карта, която има <= 0 точки 
	 * по правила '1.1' и '1.2' и има най-малко игрови точки.
	 * 
	 * 2.2.2.2. Ако нямаме боя, в която всички карти са с 0 точки по правило '1.1':
	 * 
	 * 2.2.2.2.1. Ако партньорът е играл най-силната карта, даваме карта, която има най-малко ( >0 ) точки
	 * по правило '1.1'.
	 * 
	 * 2.2.2.2.2. = '2.2.2.1.2.'
	 * 
	 **/


	public class AIPlayer : ComputerPlayer
	{
		private IDictionary< Announcement, Player > _allAnnounces;
		PlayingManager _playingManager = null;

		private AIPlayer()
		{
		}

		/// <summary>
		/// Constructor for the class. Creates a new computer player with the specified name.
		/// </summary>
		/// <param name="name">The name of the player</param>
		public AIPlayer( string name )
			: base( name )
		{
		}

		public override Announcement MakeAnnouncement( AnnouncementManager manager )
		{
			_allAnnounces = manager.GetAllAnnouncements();

			Announcement announce = null;
			bool isValidFound = false;
			int sum = 0;
			int count = 0;

			const int MIN_FOR_TRUMP = 34;
			const int MIN_FOR_NOTRUMPS = 34;
			const int MIN_FOR_ALLTRUMPS = 60;
			//const int POINTS_TO_ADD = 5; // add 5 or 10 points if player has 4 or 5 cards of same color

			#region Check Clubs
			if ( !isValidFound )
			{
				foreach ( Card card in this.Cards )
				{
					sum += CardPointEvaluator.EvaluateCard( AnnouncementTypeEnum.Clubs, card );

					if ( card.CardColor == CardColor.Clubs )
					{
						count++;
					}
				}

				if ( sum > MIN_FOR_TRUMP && count > 2 )
				{
					if ( AnnouncementTypeEnum.Clubs == manager.GetLastValidAnnouncement().Type )
					{
						announce = new Announcement( AnnouncementTypeEnum.Clubs, true, false );
					}
					else
					{
						announce = new Announcement( AnnouncementTypeEnum.Clubs, false, false );
					}
					isValidFound = manager.IsValid( this, announce );
				}
			}
			#endregion

			#region Check Diamonds
			sum = 0;
			count = 0;
			if ( !isValidFound )
			{
				foreach ( Card card in this.Cards )
				{
					sum += CardPointEvaluator.EvaluateCard( AnnouncementTypeEnum.Diamonds, card );

					if ( card.CardColor == CardColor.Diamonds )
					{
						count++;
					}
				}

				if ( sum > MIN_FOR_TRUMP && count > 2 )
				{
					if ( AnnouncementTypeEnum.Diamonds == manager.GetLastValidAnnouncement().Type )
					{
						announce = new Announcement( AnnouncementTypeEnum.Diamonds, true, false );
					}
					else
					{
						announce = new Announcement( AnnouncementTypeEnum.Diamonds, false, false );
					}
					isValidFound = manager.IsValid( this, announce );
				}
			}
			#endregion

			#region Check Hearts
			sum = 0;
			count = 0;
			if ( !isValidFound )
			{
				foreach ( Card card in this.Cards )
				{
					sum += CardPointEvaluator.EvaluateCard( AnnouncementTypeEnum.Hearts, card );

					if ( card.CardColor == CardColor.Hearts )
					{
						count++;
					}
				}

				if ( sum > MIN_FOR_TRUMP && count > 2 )
				{
					if ( AnnouncementTypeEnum.Hearts == manager.GetLastValidAnnouncement().Type )
					{
						announce = new Announcement( AnnouncementTypeEnum.Hearts, true, false );
					}
					else
					{
						announce = new Announcement( AnnouncementTypeEnum.Hearts, false, false );
					}
					isValidFound = manager.IsValid( this, announce );
				}
			}
			#endregion

			#region Check Spades
			sum = 0;
			count = 0;
			if ( !isValidFound )
			{
				foreach ( Card card in this.Cards )
				{
					sum += CardPointEvaluator.EvaluateCard( AnnouncementTypeEnum.Spades, card );

					if ( card.CardColor == CardColor.Spades )
					{
						count++;
					}
				}

				if ( sum > MIN_FOR_TRUMP && count > 2 )
				{
					if ( AnnouncementTypeEnum.Spades == manager.GetLastValidAnnouncement().Type )
					{
						announce = new Announcement( AnnouncementTypeEnum.Spades, true, false );
					}
					else
					{
						announce = new Announcement( AnnouncementTypeEnum.Spades, false, false );
					}
					isValidFound = manager.IsValid( this, announce );
				}
			}
			#endregion

			#region Check No Trumps
			sum = 0;
			if ( !isValidFound )
			{
				foreach ( Card card in this.Cards )
				{
					sum += CardPointEvaluator.EvaluateCard( AnnouncementTypeEnum.NoTrumps, card );
				}

				if ( sum > MIN_FOR_NOTRUMPS )
				{
					if ( AnnouncementTypeEnum.NoTrumps  == manager.GetLastValidAnnouncement().Type )
					{
						announce = new Announcement( AnnouncementTypeEnum.NoTrumps, true, false );
					}
					else
					{
						announce = new Announcement( AnnouncementTypeEnum.NoTrumps, false, false );
					}
					isValidFound = manager.IsValid( this, announce );
				}
			}
			#endregion

			#region Check All Trumps
			sum = 0;
			count = 0;
			if ( !isValidFound )
			{
				foreach ( Card card in this.Cards )
				{
					sum += CardPointEvaluator.EvaluateCard( AnnouncementTypeEnum.AllTrumps, card );
				}

				if ( sum > MIN_FOR_ALLTRUMPS )
				{
					if ( AnnouncementTypeEnum.AllTrumps == manager.GetLastValidAnnouncement().Type )
					{
						announce = new Announcement( AnnouncementTypeEnum.AllTrumps, true, false );
					}
					else
					{
						announce = new Announcement( AnnouncementTypeEnum.AllTrumps, false, false );
					}
					isValidFound = manager.IsValid( this, announce );
				}
			}
			#endregion


			if ( !isValidFound )
			{
				announce = new Announcement( AnnouncementTypeEnum.Pass, false, false );
			}

			RaiseAnnounceMade( announce );
			return announce;
		}

		public override bool AnnounceCombination( CardCombination combination )
		{
			//TODO implement whether player wants to announce this combination


			RaiseCardCombinationAnnounced( combination );
			return true;
		}

		protected override void PlayCard( PlayingManager manager )
		{
			Card selectedCard = null;
			_playingManager = manager;

			IList<Card> validCards = GetValidCards();

			if ( manager.CurrentHand.IsEmpty ) // 2.1
			{
				#region 2.1

				int maxEvaluated = 0;
				Card maxEvaluatedCard = null;

				foreach ( Card card in validCards )
				{
					int points = EvalutePointsOnRemaining( card );
					points += EvalutePointsOnPartnerAnnounce( card );
					points += EvalutePointsOnTrumpColor( card );

					if ( points > maxEvaluated )
					{
						maxEvaluated = points;
						maxEvaluatedCard = card;
					}
				}

				if ( maxEvaluatedCard != null )
				{
					#region 2.1.1

					if ( IsCurrentMaxCardInPlayingColor( maxEvaluatedCard ) )
					{
						#region 2.1.1.1

						selectedCard = maxEvaluatedCard;

						#endregion
					}
					else
					{
						#region 2.1.1.2

						int minPlayingPoints = 1000; // incorrect but big enough value;
						Card minPlayingPointsCard = null;

						foreach ( Card card in validCards )
						{
							if ( card.CardColor == maxEvaluatedCard.CardColor )
							{
								int points = CardPointEvaluator.EvaluateCard( _playingManager.CurrentAnnouncement.Type, card );
								if ( points < minPlayingPoints )
								{
									minPlayingPoints = points;
									minPlayingPointsCard = card;
								}
							}
						}

						selectedCard = minPlayingPointsCard;

						#endregion
					}

					#endregion
				}
				else
				{
					#region 2.1.2

					Random rand = new Random();
					selectedCard = validCards[ rand.Next( validCards.Count ) ] as Card;

					#endregion
				}

				#endregion
			}
			else //2.2
			{
				#region 2.2

				Card currentMaxCardInPlayingColor = null;
				foreach ( Card card in validCards )
				{
					if ( card.CardColor == GetPlayingColor() && IsCurrentMaxCardInPlayingColor( card ) )
					{
						currentMaxCardInPlayingColor = card;
						break;
					}
				}

				if ( currentMaxCardInPlayingColor != null ) // 2.2.1
				{
					selectedCard = currentMaxCardInPlayingColor;
				}
				else // 2.2.2
				{
					#region 2.2.2

					IList< CardColor > zeroEvaluatedColors;

					if ( FindZeroEvaluatedColor( validCards, out zeroEvaluatedColors ) ) // 2.2.2.1
					{
						#region 2.2.2.1

						Card biggestCardInHand = _playingManager.GetBiggestCard();
						Player partner = _playingManager.GetPartner( this );
						if ( IsCurrentMaxCardInPlayingColor( biggestCardInHand ) && 
							_playingManager.GetPlayerWhoPlayedCard( biggestCardInHand ) == partner )  //2.2.2.1.1
						{
							#region 2.2.2.1.1

							int maxPlayingPoints = -100;
							Card maxPlayingPointsCard = null;

							foreach ( Card card in validCards )
							{
								foreach ( CardColor color in zeroEvaluatedColors )
								{
									// we are in case 2.2.2.1 so we know card evaluated to 0 points exists
									if ( card.CardColor == color )
									{
										int points = CardPointEvaluator.EvaluateCard( _playingManager.CurrentAnnouncement.Type, card );
										if ( points > maxPlayingPoints )
										{
											maxPlayingPoints = points;
											maxPlayingPointsCard = card;
										}
									}
								}
							}

							selectedCard = maxPlayingPointsCard;

							#endregion
						}
						else // 2.2.2.1.2
						{
							#region 2.2.2.1.2

							int minPlayingPoints = 1000; // incorrect but big enough value;
							Card minPlayingPointsCard = null;

							foreach ( Card card in validCards )
							{
								foreach ( CardColor color in zeroEvaluatedColors )
								{
									// we are in case 2.2.2.1 so we know card evaluated to 0 points exists
									if ( card.CardColor == color )
									{
										int points = CardPointEvaluator.EvaluateCard( _playingManager.CurrentAnnouncement.Type, card );
										if ( points < minPlayingPoints )
										{
											minPlayingPoints = points;
											minPlayingPointsCard = card;
										}
									}
								}
							}

							selectedCard = minPlayingPointsCard;

							#endregion
						}


						#endregion
					}
					else // 2.2.2.2
					{
						#region 2.2.2.2

						Card biggestCardInHand = _playingManager.GetBiggestCard();
						Player partner = _playingManager.GetPartner( this );
						if ( IsCurrentMaxCardInPlayingColor( biggestCardInHand ) && 
							_playingManager.GetPlayerWhoPlayedCard( biggestCardInHand ) == partner )  //2.2.2.2.1
						{
							#region 2.2.2.2.1

							int minEvaluated = 1000;
							Card minEvaluatedCard = null;

							foreach ( Card card in validCards )
							{
								int pointsOnRemaining = EvalutePointsOnRemaining( card );
								// we are in case 2.2.2.2 so we know card evaluated >0 exists
								if ( pointsOnRemaining < minEvaluated )
								{
									minEvaluated = pointsOnRemaining;
									minEvaluatedCard = card;
								}
							}

							selectedCard = minEvaluatedCard;

							#endregion
						}
						else // 2.2.2.2.2
						{
							#region 2.2.2.2.2

							int minPlayingPoints = 1000; // incorrect but big enough value;
							int minOnRemainingPoints = 1000;
							Card minPlayingPointsCard = null;
							Card minOnRemainingCard = null;

							foreach ( Card card in validCards )
							{
								int pointsOnRemaining = EvalutePointsOnRemaining( card );
								pointsOnRemaining += EvalutePointsOnPartnerAnnounce( card );

								int points = CardPointEvaluator.EvaluateCard( _playingManager.CurrentAnnouncement.Type, card );
								if ( points < minPlayingPoints )
								{
									minPlayingPoints = points;
									minPlayingPointsCard = card;
								}

								if ( pointsOnRemaining <= 0 && points < minOnRemainingPoints )
								{
									minOnRemainingPoints = points;
									minOnRemainingCard = card;
								}
							}

							if ( minOnRemainingCard == null )
							{
								selectedCard = minPlayingPointsCard;
							}
							else
							{
								selectedCard = minOnRemainingCard;
							}

							#endregion
						}


						#endregion
					}

					#endregion
				}

				#endregion
			}
			
			Debug.Assert( selectedCard != null );

			RaiseCardPlayed( selectedCard );
		}

		private IList<Card> GetValidCards()
		{
			IList<Card> validCards = new List<Card>();
			foreach ( Card card in this.Cards )
			{
				if ( _playingManager.IsValid( this, card ) )
				{
					validCards.Add( card );
				}
			}

			return validCards;
		}

		private int EvalutePointsOnRemaining( Card targetCard )
		{
			// 1.1
			const int POINTS_TO_ADD = 20;
			int points = POINTS_TO_ADD;

			Card maxCard = GetCurrentMaxCardInColor( targetCard.CardColor );
			int distance = GetDistance( targetCard, maxCard, _playingManager.CurrentAnnouncement );
			int remainingCards = 0;

			foreach ( Card card in this.Cards )
			{
				if ( card.CardColor == targetCard.CardColor && card != targetCard )
				{
					remainingCards++;
				}
			}

			int passedCards = 0;
			foreach ( Card card in _playingManager.PlayedCards )
			{
				if ( card.CardColor == targetCard.CardColor )
				{
					passedCards++;
				}
			}

			if ( remainingCards >= distance )
			{
				points += ( remainingCards - distance ) * POINTS_TO_ADD - distance - passedCards;
			}
			else
			{
				points = 0;
			}

			return points;
		}

		private int EvalutePointsOnPartnerAnnounce( Card targetCard )
		{
			// 1.2
			int points = 0;
			const int POINTS_TO_ADD = 10;

			foreach ( KeyValuePair< Announcement, Player> kv in _allAnnounces )
			{
				Announcement announce = kv.Key;
				if ( announce.Type == AnnouncementTypeEnum.Spades || 
					announce.Type == AnnouncementTypeEnum.Hearts || 
					announce.Type == AnnouncementTypeEnum.Diamonds || 
					announce.Type == AnnouncementTypeEnum.Clubs )
				{
					if ( targetCard.CardColor == GetCardColor( announce.Type ) )
					{
						Player player = kv.Value;

						if ( player == _playingManager.GetPartner( this ) )
						{
							points += POINTS_TO_ADD;
						}

						if ( player != this && player != _playingManager.GetPartner( this ) )
						{
							points -= POINTS_TO_ADD;
						}
					}
				}
			}

			return points;
		}

		private int EvalutePointsOnTrumpColor( Card targetCard )
		{
			int POINTS_TO_ADD = 10;
			if ( targetCard.CardColor == GetTrumpColor() )
			{
				return POINTS_TO_ADD;
			}
			else
			{
				return 0;
			}
		}

		private bool IsCurrentMaxCardInPlayingColor( Card targetCard )
		{
			CardComparer comparer = new CardComparer( _playingManager.CurrentAnnouncement.Type );
			bool foundBigger = false;

			foreach ( Card remCard in _playingManager.RemainingCards )
			{
				if ( targetCard.CardColor == remCard.CardColor )
				{
					if ( comparer.Compare( targetCard, remCard ) < 0 )
					{
						foundBigger = true;
						break;
					}
				}
			}

			if ( !foundBigger )
			{
				foreach ( Card remCard in _playingManager.CurrentHand )
				{
					if ( targetCard.CardColor == remCard.CardColor )
					{
						if ( comparer.Compare( targetCard, remCard ) < 0 )
						{
							foundBigger = true;
							break;
						}
					}
				}
			}

			return !foundBigger;
		}
		private bool FindZeroEvaluatedColor( IList< Card> validCards, out IList< CardColor > zeroEvaluatedColors )
		{
			zeroEvaluatedColors = new List< CardColor >();

			#region Spades

			int spadesCount = 0;
			int zeroEvaluatedSpadesCount = 0;
			foreach ( Card card in validCards )
			{
				if ( card.CardColor == CardColor.Spades )
				{
					spadesCount++;
					if ( EvalutePointsOnRemaining( card ) == 0 )
					{
						zeroEvaluatedSpadesCount++;
					}
				}
			}

			if ( zeroEvaluatedSpadesCount == spadesCount && spadesCount != 0 )
			{
				zeroEvaluatedColors.Add( CardColor.Spades );
			}

			#endregion

			#region Hearts

			int heartsCount = 0;
			int zeroEvaluatedHeartsCount = 0;
			foreach ( Card card in validCards )
			{
				if ( card.CardColor == CardColor.Hearts )
				{
					heartsCount++;
					if ( EvalutePointsOnRemaining( card ) == 0 )
					{
						zeroEvaluatedHeartsCount++;
					}
				}
			}

			if ( zeroEvaluatedHeartsCount == heartsCount  && heartsCount != 0 )
			{
				zeroEvaluatedColors.Add( CardColor.Hearts );
			}

			#endregion

			#region Diamonds

			int diamondsCount = 0;
			int zeroEvaluatedDiamondsCount = 0;
			foreach ( Card card in validCards )
			{
				if ( card.CardColor == CardColor.Diamonds )
				{
					diamondsCount++;
					if ( EvalutePointsOnRemaining( card ) == 0 )
					{
						zeroEvaluatedDiamondsCount++;
					}
				}
			}

			if ( zeroEvaluatedDiamondsCount == diamondsCount && diamondsCount != 0 )
			{
				zeroEvaluatedColors.Add( CardColor.Diamonds );
			}

			#endregion

			#region Clubs

			int clubsCount = 0;
			int zeroEvaluatedClubsCount = 0;
			foreach ( Card card in validCards )
			{
				if ( card.CardColor == CardColor.Clubs )
				{
					clubsCount++;
					if ( EvalutePointsOnRemaining( card ) == 0 )
					{
						zeroEvaluatedClubsCount++;
					}
				}
			}

			if ( zeroEvaluatedClubsCount == clubsCount && clubsCount != 0 )
			{
				zeroEvaluatedColors.Add( CardColor.Clubs );
			}

			#endregion

			return zeroEvaluatedColors.Count != 0;
		}
		private CardColor GetCardColor( AnnouncementTypeEnum type )
		{
			CardColor trump = 0;

			switch ( type )
			{
				case AnnouncementTypeEnum.Spades:
					trump = CardColor.Spades;
					break;
				case AnnouncementTypeEnum.Hearts:
					trump = CardColor.Hearts;
					break;
				case AnnouncementTypeEnum.Diamonds:
					trump = CardColor.Diamonds;
					break;
				case AnnouncementTypeEnum.Clubs:
					trump = CardColor.Clubs;
					break;
			}
			return trump;
		}
		private CardColor GetTrumpColor()
		{
			CardColor trump = 0;

			switch ( _playingManager.CurrentAnnouncement.Type )
			{
				case AnnouncementTypeEnum.Spades:
					trump = CardColor.Spades;
					break;
				case AnnouncementTypeEnum.Hearts:
					trump = CardColor.Hearts;
					break;
				case AnnouncementTypeEnum.Diamonds:
					trump = CardColor.Diamonds;
					break;
				case AnnouncementTypeEnum.Clubs:
					trump = CardColor.Clubs;
					break;
			}
			return trump;
		}
		private CardColor GetPlayingColor()
		{
			return _playingManager.CurrentHand[ 0 ].CardColor;
		}
		private Card GetCurrentMaxCardInColor( CardColor color )
		{
			Card maxCard = null;
			CardComparer comparer = new CardComparer( _playingManager.CurrentAnnouncement.Type );

			foreach ( Card card in _playingManager.RemainingCards )
			{
				if ( card.CardColor == color )
				{
					if ( maxCard == null )
					{
						maxCard = card;
					}

					if ( comparer.Compare( maxCard, card ) < 0 )
					{
						maxCard = card;
					}
				}
			}

			foreach ( Card card in _playingManager.CurrentHand )
			{
				if ( card.CardColor == color )
				{
					if ( maxCard == null )
					{
						maxCard = card;
					}

					if ( comparer.Compare( maxCard, card ) < 0 )
					{
						maxCard = card;
					}
				}
			}

			return maxCard;
		}
		private int GetDistance( Card card, Card maxCard, Announcement announce )
		{
			int distance = 0;
			if ( card.CardColor != maxCard.CardColor )
			{
				throw new ArgumentException( "When comparing distance between two cards, they must have same color" );
			}

			CardColor trumpColor = GetTrumpColor();

			switch ( announce.Type )
			{
				case AnnouncementTypeEnum.AllTrumps:
					distance = GetDistanceTrumps( card, maxCard );
					break;
				case AnnouncementTypeEnum.NoTrumps:
					distance = GetDistanceNoTrumps( card, maxCard );
					break;
				case AnnouncementTypeEnum.Spades:
				case AnnouncementTypeEnum.Hearts:
				case AnnouncementTypeEnum.Diamonds:
				case AnnouncementTypeEnum.Clubs:
					if ( card.CardColor == trumpColor )
						distance = GetDistanceTrumps( card, maxCard );
					else
						distance = GetDistanceNoTrumps( card, maxCard );
					break;
			}

			return distance;
		}

		private int GetDistanceTrumps( Card card, Card maxCard )
		{
			int x = 0, y = 0;

			switch ( card.CardType )
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

			switch ( maxCard.CardType )
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

			return y - x;
		}

		private int GetDistanceNoTrumps( Card card, Card maxCard )
		{
			int x = 0, y = 0;

			switch ( card.CardType )
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

			switch ( maxCard.CardType )
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

			return y - x;

		}
	}
}
