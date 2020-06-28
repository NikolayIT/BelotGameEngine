namespace Belot.Engine.GameMechanics
{
    using System;

    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class GameRoundManager
    {
        private readonly IPlayer southPlayer;

        private readonly IPlayer eastPlayer;

        private readonly IPlayer northPlayer;

        private readonly IPlayer westPlayer;

        public GameRoundManager(IPlayer southPlayer, IPlayer eastPlayer, IPlayer northPlayer, IPlayer westPlayer)
        {
            this.southPlayer = southPlayer;
            this.eastPlayer = eastPlayer;
            this.northPlayer = northPlayer;
            this.westPlayer = westPlayer;
        }

        public RoundResult PlayRound(int southNorthTeamPoints, int eastWestTeamPoints)
        {
            var deck = new Deck();
            var southCards = new CardCollection();
            var eastCards = new CardCollection();
            var northCards = new CardCollection();
            var westCards = new CardCollection();

            // Deal 5 cards each
            for (var i = 0; i < 5; i++)
            {
                southCards.Add(deck.GetNextCard());
            }

            for (var i = 0; i < 5; i++)
            {
                eastCards.Add(deck.GetNextCard());
            }

            for (var i = 0; i < 5; i++)
            {
                northCards.Add(deck.GetNextCard());
            }

            for (var i = 0; i < 5; i++)
            {
                westCards.Add(deck.GetNextCard());
            }

            // TODO: Announces

            return new RoundResult
                       {
                           NoTricksForOneOfTheTeams = false,
                           EastWestTeamPoints = new Random().Next(0, 20),
                           SouthNorthTeamPoints = new Random().Next(0, 20),
                       };
        }
    }
}
