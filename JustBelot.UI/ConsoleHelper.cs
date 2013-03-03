namespace JustBelot.UI
{
    using System;

    public static class ConsoleHelper
    {
        public static void WriteOnPosition(
            string text,
            int left = 0,
            int top = 0,
            ConsoleColor foregroundColor = ConsoleColor.White,
            ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.SetCursorPosition(left, top);
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.Write(text);
        }

        public static void WriteOnPosition(
            char character,
            int left = 0,
            int top = 0,
            ConsoleColor foregroundColor = ConsoleColor.White,
            ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.SetCursorPosition(left, top);
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.Write(character);
        }

        public static void ResizeConsole(int width, int height)
        {
            Console.WindowWidth = width;
            Console.WindowHeight = height;
            Console.BufferWidth = Console.WindowWidth;
            Console.BufferHeight = Console.WindowHeight;
        }

        public static void ClearAndResetConsole()
        {
            Console.ResetColor();
            Console.Clear();
        }

        public static void DrawTextBoxTopLeft(
            string text,
            int left = 0,
            int top = 0,
            ConsoleColor foregroundColor = ConsoleColor.White,
            ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            // ╔═════════╗
            // ║Some Text║
            // ╚═════════╝
            WriteOnPosition('╔', left, top, foregroundColor, backgroundColor);
            WriteOnPosition('╗', left + text.Length + 1, top, foregroundColor, backgroundColor);

            WriteOnPosition('╚', left, top + 2, foregroundColor, backgroundColor);
            WriteOnPosition('╝', left + text.Length + 1, top + 2, foregroundColor, backgroundColor);

            WriteOnPosition('║', left, top + 1, foregroundColor, backgroundColor);
            WriteOnPosition('║', left + text.Length + 1, top + 1, foregroundColor, backgroundColor);

            WriteOnPosition(new string('═', text.Length), left + 1, top, foregroundColor, backgroundColor);
            WriteOnPosition(new string('═', text.Length), left + 1, top + 2, foregroundColor, backgroundColor);

            WriteOnPosition(text, left + 1, top + 1, foregroundColor, backgroundColor);
        }

        public static void DrawTextBoxTopRight(
            string text,
            int right = 0,
            int top = 0,
            ConsoleColor foregroundColor = ConsoleColor.White,
            ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            // ╔═════════╗
            // ║Some Text║
            // ╚═════════╝
            WriteOnPosition('╔', right - text.Length - 1, top, foregroundColor, backgroundColor);
            WriteOnPosition('╗', right, top, foregroundColor, backgroundColor);

            WriteOnPosition('╚', right - text.Length - 1, top + 2, foregroundColor, backgroundColor);
            WriteOnPosition('╝', right, top + 2, foregroundColor, backgroundColor);

            WriteOnPosition('║', right - text.Length - 1, top + 1, foregroundColor, backgroundColor);
            WriteOnPosition('║', right, top + 1, foregroundColor, backgroundColor);

            WriteOnPosition(new string('═', text.Length), right - text.Length, top, foregroundColor, backgroundColor);
            WriteOnPosition(new string('═', text.Length), right - text.Length, top + 2, foregroundColor, backgroundColor);

            WriteOnPosition(text, right - text.Length, top + 1, foregroundColor, backgroundColor);
        }
    }
}
