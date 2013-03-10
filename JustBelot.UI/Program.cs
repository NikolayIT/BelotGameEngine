namespace JustBelot.UI
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using JustBelot.AI.DummyPlayer;
    using JustBelot.Common;

    public class Program
    {
        public static void Main()
        {
            // Initialize console properties
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            ConsoleHelper.ResizeConsole(80, Settings.ConsoleHeight);
            Console.OutputEncoding = Encoding.Unicode;

            Console.Title = Settings.ProgramName;
            ConsoleHelper.WriteOnPosition("Please enter player name: ", 20, 9, ConsoleColor.Black, ConsoleColor.DarkGray);
            var playerName = Console.ReadLine();
            Console.Clear();

            IPlayer southPlayer = new ConsoleHumanPlayer(playerName);
            IPlayer eastPlayer = new DummyPlayer("East dummy");
            IPlayer northPlayer = new DummyPlayer("North dummy");
            IPlayer westPlayer = new DummyPlayer("West dummy");

            var game = new GameManager(southPlayer, eastPlayer, northPlayer, westPlayer);
            game.StartNewGame();
        }
    }
}
