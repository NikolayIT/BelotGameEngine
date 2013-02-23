namespace JustBelot.UI
{
    using System;

    using JustBelot.AI.DummyPlayer;
    using JustBelot.Common;

    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("JustBelot 1.0.20130224");
            Console.Write("Please enter player name: ");
            var playerName = Console.ReadLine();

            IPlayer southPlayer = new ConsoleHumanPlayer(playerName);
            IPlayer eastPlayer = new DummyPlayer();
            IPlayer northPlayer = new DummyPlayer();
            IPlayer westPlayer = new DummyPlayer();

            var game = new GameManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            game.StartNewGame();
        }
    }
}
