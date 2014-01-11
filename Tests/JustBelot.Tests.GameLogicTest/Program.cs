namespace JustBelot.Tests.GameLogicTest
{
    using JustBelot.AI.DummyPlayer;
    using JustBelot.Common;

    internal class Program
    {
        private static void Main()
        {
            IPlayer southPlayer = new DummyPlayer("South dummy");
            //IPlayer southPlayer = new DebugDummyPlayer("South debug dummy");
            IPlayer eastPlayer = new DummyPlayer("East dummy");
            IPlayer northPlayer = new DummyPlayer("North dummy", alwaysPass: false);
            IPlayer westPlayer = new DummyPlayer("West dummy", alwaysPass: false);
            var game = new GameManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            game.GameInfo.PlayerBid += GameInfoOnPlayerBid;
            game.GameInfo.CardPlayed += GameInfoOnCardPlayed;

            for (int i = 0; i < 10000; i++)
            {
                game.StartNewGame();
                //// Console.WriteLine("{0} - {1}", game.SouthNorthScore, game.EastWestScore);
            }
        }

        private static void GameInfoOnPlayerBid(BidEventArgs eventArgs)
        {
            // Console.WriteLine("{1} from {0}", eventArgs.Position, eventArgs.Bid);
            //// Console.ReadKey();
        }

        private static void GameInfoOnCardPlayed(CardPlayedEventArgs eventArgs)
        {
            // Console.WriteLine("{0} played {1}", eventArgs.Position, eventArgs.PlayAction.Card);
            //// Console.ReadKey();
        }
    }
}
