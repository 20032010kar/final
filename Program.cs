using System;
using System.Text;
using System.Threading;

namespace finalProject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Mutex mutex = new Mutex(false, "WordScanner_Instance");
            if (!mutex.WaitOne(0))
            {
                Console.WriteLine("[!] Програма вже запущена!");
                return;
            }
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                SyncState.Cts.Cancel();
                Console.WriteLine("Зупинка...");
            };

            if (args.Length > 0)
                InteractiveMenu.RunFromArgs(args);
            else
                InteractiveMenu.Run();

            mutex.ReleaseMutex();
        }
    }
}
