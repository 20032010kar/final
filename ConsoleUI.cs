using System;

namespace finalProject
{
    internal static class ConsoleUI
    {
        public static void Print(string msg)
        {
            lock (SyncState.ConsoleLock)
                Console.WriteLine(msg);
        }

        public static void Error(string msg) => Print("[!] " + msg);

        public static void PrintHeader()
        {
            Console.WriteLine("WORD SCANNER v1.0");
            Console.WriteLine("Пошук заборонених слів у файлах");
            Console.WriteLine();
        }

        public static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Використання:");
            Console.WriteLine("  WordScanner.exe --words слово1,слово2 --search C:\\папка");
            Console.WriteLine("  WordScanner.exe --wordfile words.txt --search C:\\папка");
            Console.WriteLine();
            Console.WriteLine("Параметри:");
            Console.WriteLine("  --words    слова через кому");
            Console.WriteLine("  --wordfile шлях до файлу зі словами");
            Console.WriteLine("  --search   папка де шукати");
            Console.WriteLine("  --output   папка для результатів");
            Console.WriteLine("  --ext      розширення (.txt,.cs,...)");
            Console.WriteLine("  --case     чутливий до регістру");
        }
    }
}
