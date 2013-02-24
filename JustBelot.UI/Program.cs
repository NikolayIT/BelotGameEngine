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
            ConsoleHelper.ResizeConsole(80, 20);
            Console.OutputEncoding = Encoding.Unicode;

            Console.WriteLine(Settings.ProgramName);
            Console.Write("Please enter player name: ");
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
