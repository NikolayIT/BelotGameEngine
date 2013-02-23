namespace JustBelot.UI
{
    using System;
    using System.Collections.Generic;

    using JustBelot.Common;
    using JustBelot.NikiAI;

    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("JustBelot 1.0.20130220");
            Console.Write("Please enter player name: ");
            string playerName = Console.ReadLine();

            IPlayer southPlayer = new ConsoleHumanPlayer(playerName);
            IPlayer eastPlayer = new DummyPlayer();
            IPlayer northPlayer = new DummyPlayer();
            IPlayer westPlayer = new DummyPlayer();

            var game = new GameManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            game.StartNewGame();
        }
    }
}
