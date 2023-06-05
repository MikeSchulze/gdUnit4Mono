
using System;
using System.Drawing;

namespace GdUnit4.Core
{
    public sealed class GdUnitConsole
    {

        public const int BOLD = 0x1;
        public const int ITALIC = 0x2;
        public const int UNDERLINE = 0x4;
        private const String __CSI_BOLD = "\u001b[1m";
        private const String __CSI_ITALIC = "\u001b[3m";
        private const String __CSI_UNDERLINE = "\u001b[4m";

        static object lockObj = new object();

        public GdUnitConsole Write(string message, ConsoleColor color = ConsoleColor.White)
        {
            lock (lockObj)
            {
                Console.Write(message);
            }
            return this;
        }

        public GdUnitConsole WriteLine(string message, ConsoleColor color = ConsoleColor.White)
        {
            lock (lockObj)
            {
                Color c = ToColor(color);
                Console.WriteLine("\u001b[38;2;{0};{1};{2}m  {3} \u001b[0m", c.R, c.G, c.B, message);
            }
            return this;
        }

        public GdUnitConsole NewLine()
        {
            Console.WriteLine("");
            return this;
        }

        public GdUnitConsole PrintError(String message)
        {
            return SetColor(ConsoleColor.DarkRed)
                .WriteLine(message)
                .EndColor()
                .NewLine();
        }

        public GdUnitConsole PrintColored(String message, ConsoleColor color, int flags = 0)
        {
            return SetColor(color)
                .Bold((flags & BOLD) == BOLD)
                .Italic((flags & ITALIC) == ITALIC)
                .Underline((flags & UNDERLINE) == UNDERLINE)
                .Write(message)
                .EndColor();
        }

        private GdUnitConsole SetColor(ConsoleColor color)
        {
            Color c = ToColor(color);
            Console.Write("\u001b[38;2;{0};{1};{2}m", c.R, c.G, c.B);
            return this;
        }

        private GdUnitConsole EndColor()
        {
            Console.Write("\u001b[0m");
            return this;
        }


        private GdUnitConsole Underline(bool enable)
        {
            if (enable)
                Console.Write(__CSI_UNDERLINE);
            return this;
        }

        private GdUnitConsole Bold(bool enable)
        {
            if (enable)
                Console.Write(__CSI_BOLD);
            return this;
        }

        private GdUnitConsole Italic(bool enable)
        {
            if (enable)
                Console.Write(__CSI_ITALIC);
            return this;
        }

        private static Color ToColor(ConsoleColor color)
        {
            string? colorName = Enum.GetName(typeof(ConsoleColor), color);
            return Color.FromName(colorName!);
        }
    }
}
