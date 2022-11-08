using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Dmap.CLI
{
    public static class Log
    {
        public static void Error(string Message, ConsoleColor BackColor = ConsoleColor.Red, ConsoleColor TextColor = ConsoleColor.Black)
        {
            ConsoleColor currTextColor = Console.ForegroundColor;
            ConsoleColor currColor = Console.BackgroundColor;
            Console.BackgroundColor = BackColor;
            Console.ForegroundColor = TextColor;
            Console.Write("ERROR:");
            Console.BackgroundColor = currColor;
            Console.ForegroundColor = currTextColor;
            Console.WriteLine($" {Message}");
        }
        public static void Warn(string Message, ConsoleColor BackColor = ConsoleColor.Yellow, ConsoleColor TextColor = ConsoleColor.Black)
        {
            ConsoleColor currTextColor = Console.ForegroundColor;
            ConsoleColor currColor = Console.BackgroundColor;
            Console.BackgroundColor = BackColor;
            Console.ForegroundColor = TextColor;
            Console.Write("WARN: ");
            Console.BackgroundColor = currColor;
            Console.ForegroundColor = currTextColor;
            Console.WriteLine($" {Message}");
        }
        public static void Info(string Message)
        {
            Console.Write("INFO:");
            Console.WriteLine($" {Message}");
        }
    }
}
