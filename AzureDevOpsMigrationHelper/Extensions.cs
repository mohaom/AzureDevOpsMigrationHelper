using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevOps_Migration_Helper
{
    public static class Extensions
    {
        public static void Writeline(this StreamWriter sw, string text)
        {
            sw.WriteLine(text);
            Console.WriteLine(text);
        }
        public static void Writeline(this StreamWriter sw, string text, MessageType mesType)
        {
            switch (mesType)
            {
                case MessageType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(text);
                    Console.ResetColor();
                    break;
                case MessageType.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(text);
                    Console.ResetColor();
                    break;
                case MessageType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(text);
                    Console.ResetColor();
                    break;
            }
            sw.WriteLine(text);

        }
    }

    public enum MessageType
    {
        Error, Info, Warning
    }
}
