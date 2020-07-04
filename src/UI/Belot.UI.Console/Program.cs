namespace Belot.UI.Console
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            ConsoleHelper.ResizeConsole(80, 20);
            Console.Title = "Console Belot 1.0";

            IPlayer southPlayer = new ConsoleHumanPlayer();
            IPlayer eastPlayer = new SmartPlayer();
            IPlayer northPlayer = new SmartPlayer();
            IPlayer westPlayer = new SmartPlayer();
            var game = new BelotGame(southPlayer, eastPlayer, northPlayer, westPlayer);
            var result = game.PlayGame(PlayerPosition.South);
            Console.WriteLine("Winner: " + result.Winner);

            //// RandomCards();
        }

        private static void RandomCards()
        {
            for (var i = 0; i < 100; i++)
            {
                var deck = new Deck();
                deck.Shuffle();
                for (var j = 0; j < 32; j++)
                {
                    Console.Write(deck.GetNextCard() + " ");
                }

                Console.WriteLine();
            }
        }
    }
}
