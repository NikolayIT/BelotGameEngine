namespace Belot.UI.Console
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public static class Program
    {
        public static void Main()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Console.OutputEncoding = Encoding.UTF8;
            //// Console.BufferHeight = Console.WindowHeight = 17;
            //// Console.BufferWidth = Console.WindowWidth = 50;
            Console.WriteLine("Belot Console 1.0");

            //// RandomCards();

            var stopwatch = Stopwatch.StartNew();
            var logGame = new BelotGame(new LoggingPlayerDecorator(new SmartPlayer()), new RandomPlayer(), new SmartPlayer(), new RandomPlayer());
            var game = new BelotGame(new SmartPlayer(), new RandomPlayer(), new SmartPlayer(), new RandomPlayer());

            var southNorthWins = 0;
            var eastWestWins = 0;
            for (var i = 1; i <= 100_000; i++)
            {
                var firstToPlay = ((i - 1) % 4) switch
                    {
                        0 => PlayerPosition.South,
                        1 => PlayerPosition.East,
                        2 => PlayerPosition.North,
                        3 => PlayerPosition.West,
                        _ => PlayerPosition.South,
                    };
                var result = game.PlayGame(firstToPlay);
                if (result.Winner == PlayerPosition.SouthNorthTeam)
                {
                    southNorthWins++;
                }
                else if (result.Winner == PlayerPosition.EastWestTeam)
                {
                    eastWestWins++;
                }

                //// Console.WriteLine(
                ////     $"Game #{i}: Winner: {result.Winner}; Result(SN-EW): {result.SouthNorthPoints} - {result.EastWestPoints} (Rounds: {result.RoundsPlayed})");
            }

            Console.WriteLine(stopwatch.Elapsed);
            Console.WriteLine($"Result: (SN-EW): {southNorthWins}-{eastWestWins}");
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
