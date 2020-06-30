namespace Belot.UI.Console
{
    using System;
    using System.Diagnostics;
    using System.Text;

    using Belot.AI.DummyPlayer;
    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            //// Console.BufferHeight = Console.WindowHeight = 17;
            //// Console.BufferWidth = Console.WindowWidth = 50;
            Console.WriteLine("Belot Console 1.0");

            var stopwatch = Stopwatch.StartNew();
            //// var game = new BelotGame(
            ////     new LoggingPlayerDecorator(new SmartPlayer()),
            ////     new LoggingPlayerDecorator(new RandomPlayer()),
            ////     new LoggingPlayerDecorator(new SmartPlayer()),
            ////     new LoggingPlayerDecorator(new RandomPlayer()));
            var game = new BelotGame(new SmartPlayer(), new RandomPlayer(), new SmartPlayer(), new RandomPlayer());

            int southNorthWins = 0;
            int eastWestWins = 0;

            for (var i = 1; i <= 100000; i++)
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
    }
}
